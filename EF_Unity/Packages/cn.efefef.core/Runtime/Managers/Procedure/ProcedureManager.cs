/*
 * ================================================
 * Describe:      全局流程管理器 - 串行退出、安全并发
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 15:19:27
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-08
 * ScriptVersion: 0.8
 * ===============================================
 */
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Pool;
using UnityEngine;

namespace EasyFramework.Managers.Procedure
{
    public sealed class ProcedureManager : MonoSingleton<ProcedureManager>, IManager, IUpdate
    {
        private uint _nextUid = 1;
        private bool _isClearing;
        private bool _processingExit;
        private readonly Stack<ProcedureInstance> _instanceStack = new();
        private readonly Dictionary<uint, ProcedureInstance> _uidToInstance = new();
        private readonly Dictionary<Type, Func<IProcedure>> _factories = new();
        private readonly Queue<ProcedureInstance> _pendingExits = new();

        [SerializeField] private int maxDepth = 100;
        [SerializeField] private int maxChainRepeat = 5;
        [SerializeField] private float defaultTimeoutSeconds = 300f;

        public bool HasRunningProcedure => _instanceStack.Count > 0;

        void ISingleton.Init() => InitPools();
        void ISingleton.Quit() => ForceClear();

        private void InitPools()
        {
            var pool = PoolManager.Instance;
            pool.CreateObjectPool<ProcedureInstance>(4096,
                () => new ProcedureInstance(),
                x => x.Reset());
            pool.CreateObjectPool<ProcedureContext>(4096,
                () => new ProcedureContext(),
                x => x.Reset());
            pool.CreateObjectPool<Dictionary<string, object>>(4096,
                () => new Dictionary<string, object>(8),
                x => x.Clear());
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            var inst = GetActiveInstance();
            if (inst != null && inst.IsActive)
            {
                try
                {
                    inst.Procedure.OnUpdate(elapse, realElapse);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    RequestExit(inst);
                }
            }

            ProcessPendingExits().Forget();
        }

        public void Register<T>() where T : IProcedure, new()
        {
            _factories[typeof(T)] = () => new T();
        }

        public void Register<T>(Func<T> factory) where T : IProcedure
        {
            _factories[typeof(T)] = () => factory();
        }

        /// <summary>
        /// 启动一个新的根流程（先同步清空当前流程栈）
        /// </summary>
        public async UniTask Switch<T>(Dictionary<string, object> parameters = null) where T : IProcedure
        {
            _isClearing = true;
            try
            {
                while (_instanceStack.Count > 0)
                {
                    var top = _instanceStack.Peek();
                    // 统一通过退出方法清理（即使已退出，也会回收资源并从栈移除）
                    await PopAndResumeInternal(top);
                }
            }
            finally
            {
                _isClearing = false;
            }

            await StartProcedureInternal(typeof(T), parameters, 0, false);
        }

        /// <summary>
        /// 供 ProcedureContext 调用，启动子流程并等待其完全退出
        /// </summary>
        internal async UniTask StartSubProcedureAndWait<T>(ProcedureContext parentCtx,
            Dictionary<string, object> parameters) where T : IProcedure
        {
            var parentInst = GetInstanceByUid(parentCtx.UID);
            if (parentInst == null || !_uidToInstance.ContainsKey(parentCtx.UID) || parentInst.ExitState != 0)
            {
                Debug.LogError("Parent procedure not found or already exited.");
                return;
            }

            if (parentInst.State != ProcedureState.Active && parentInst.State != ProcedureState.Entering)
            {
                Debug.LogError("Parent procedure is not active.");
                return;
            }

            SuspendInstance(parentInst);
            await StartProcedureInternal(typeof(T), parameters, parentCtx.UID, true);
        }

        internal async UniTask EndProcedureInternal(ProcedureContext ctx)
        {
            var inst = GetActiveInstance();
            if (inst == null || inst.UID != ctx.UID)
            {
                Debug.LogError("EndProcedure mismatch: not the active instance.");
                return;
            }

            RequestExit(inst);
            var completion = inst.CompletionSource;
            if (completion != null)
                await completion.Task;
        }

        internal ProcedureInstance GetInstanceByUidInternal(uint uid)
        {
            return GetInstanceByUid(uid);
        }

        private async UniTask StartProcedureInternal(Type targetType,
            Dictionary<string, object> parameters, uint parentUid, bool waitForExit)
        {
            int newDepth = parentUid == 0 ? 1 : (GetInstanceByUid(parentUid)?.Depth + 1 ?? 1);
            if (newDepth > maxDepth)
            {
                Debug.LogError($"MaxDepth reached: {targetType.Name}");
                return;
            }

            if (parentUid != 0 && WouldExceedChainLimit(targetType, parentUid))
            {
                Debug.LogError("Chain limit exceeded");
                return;
            }

            if (!_factories.TryGetValue(targetType, out var factory))
            {
                Debug.LogError($"Unregistered Procedure: {targetType}");
                return;
            }

            var inst = PoolManager.Instance.GetFromPool<ProcedureInstance>();
            inst.UID = _nextUid++;
            inst.ParentUID = parentUid;
            inst.Depth = newDepth;
            inst.ProcedureType = targetType;
            inst.Procedure = factory();
            inst.State = ProcedureState.Entering;
            inst.CompletionSource = new UniTaskCompletionSource();

            var paramDict = PoolManager.Instance.GetFromPool<Dictionary<string, object>>();
            paramDict.Clear();
            if (parameters != null)
            {
                foreach (var kv in parameters)
                    paramDict[kv.Key] = kv.Value;
            }
            inst.Params = paramDict;

            var ctx = PoolManager.Instance.GetFromPool<ProcedureContext>();
            ctx.UID = inst.UID;
            ctx.ParentUID = parentUid;
            ctx.Depth = newDepth;
            ctx.RuntimeVersion = inst.RuntimeVersion;
            ctx.Params = paramDict;
            inst.Context = ctx;

            inst.LifecycleCts = new CancellationTokenSource();
            inst.EnterTimeoutCts = new CancellationTokenSource();

            _instanceStack.Push(inst);
            _uidToInstance[inst.UID] = inst;

            EF.Events.Publish(new ProcedureEnterEvent
            {
                Uid = inst.UID,
                ParentUid = parentUid,
                ProcedureType = targetType,
                Depth = newDepth
            });

            TimeoutAsync(inst).Forget();

            try
            {
                await inst.Procedure.OnEnter(ctx, inst.LifecycleCts.Token);

                if (inst.ExitState == 0 && inst.State == ProcedureState.Entering)
                {
                    inst.State = ProcedureState.Active;
                    CancelTimeoutSafe(inst);
                    EF.Events.Publish(new ProcedureActivateEvent
                    {
                        Uid = inst.UID,
                        ProcedureType = targetType,
                        Depth = newDepth
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // 超时或外部取消
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                RequestExit(inst);
                return;
            }

            if (waitForExit)
            {
                var completion = inst.CompletionSource;
                if (completion != null)
                    await completion.Task;
            }
        }

        private void SuspendInstance(ProcedureInstance inst)
        {
            if (inst.State != ProcedureState.Active && inst.State != ProcedureState.Entering)
                return;

            inst.State = ProcedureState.Suspended;
            CancelTimeoutSafe(inst);
            EF.Events.Publish(new ProcedureSuspendEvent
            {
                Uid = inst.UID,
                ProcedureType = inst.ProcedureType,
                Depth = inst.Depth
            });
        }

        private void RequestExit(ProcedureInstance inst)
        {
            if (inst == null) return;
            if (inst.State == ProcedureState.Exited) return;
            if (Interlocked.Exchange(ref inst.ExitQueued, 1) == 1) return;

            _pendingExits.Enqueue(inst);
        }

        private async UniTaskVoid ProcessPendingExits()
        {
            if (_processingExit) return;
            _processingExit = true;

            try
            {
                while (_pendingExits.Count > 0)
                {
                    var inst = _pendingExits.Dequeue();
                    if (inst == null) continue;

                    // 由 PopAndResumeInternal 统一处理幂等与退出逻辑
                    try
                    {
                        await PopAndResumeInternal(inst);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
            finally
            {
                _processingExit = false;
            }
        }

        /// <summary>
        /// 执行退出并清理实例（幂等，可处理已退出实例的回收）
        /// </summary>
        private async UniTask PopAndResumeInternal(ProcedureInstance inst)
        {
            if (inst == null) return;

            // 首次退出标记，负责执行 OnLeave 及发布事件
            if (Interlocked.Exchange(ref inst.ExitState, 1) == 0)
            {
                inst.State = ProcedureState.Exiting;
                CancelTimeoutSafe(inst);

                EF.Events.Publish(new ProcedureExitEvent
                {
                    Uid = inst.UID,
                    ProcedureType = inst.ProcedureType,
                    Depth = inst.Depth
                });

                try
                {
                    inst.LifecycleCts?.Cancel();
                    await inst.Procedure.OnLeave(CancellationToken.None);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            // 以下为清理和栈移除（无论首次还是重复调用均执行）
            bool isTop = _instanceStack.Count > 0 && _instanceStack.Peek() == inst;

            if (isTop)
            {
                _instanceStack.Pop();
            }
            else
            {
                Debug.LogError(
                    $"Illegal non-top exit detected. UID={inst.UID}, Type={inst.ProcedureType?.Name}. " +
                    "Instance will be removed from lookup but stack structure preserved.");
                // 不破坏栈，标记为僵尸，由后续 GetActiveInstance 清理
                _uidToInstance.Remove(inst.UID);
                if (inst.Context != null)
                {
                    inst.Context.IsDisposed = true;
                    PoolManager.Instance.ReturnToPool(inst.Context);
                    inst.Context = null;
                }

                if (inst.Params != null)
                {
                    PoolManager.Instance.ReturnToPool(inst.Params);
                    inst.Params = null;
                }

                inst.CompletionSource?.TrySetResult();
                inst.State = ProcedureState.Exited; // 标记等待后续弹出回收
                return;
            }

            // 正常从栈顶移除后的回收
            _uidToInstance.Remove(inst.UID);

            if (inst.Context != null)
            {
                inst.Context.IsDisposed = true;
                PoolManager.Instance.ReturnToPool(inst.Context);
                inst.Context = null;
            }

            if (inst.Params != null)
            {
                PoolManager.Instance.ReturnToPool(inst.Params);
                inst.Params = null;
            }

            inst.CompletionSource?.TrySetResult();
            // 确保只回收未重置过的实例
            if (inst.State != ProcedureState.None)
            {
                inst.Reset();
                PoolManager.Instance.ReturnToPool(inst);
            }

            // 恢复父流程（非清理阶段）
            if (!_isClearing && _instanceStack.Count > 0)
            {
                // 清理栈顶僵尸实例
                CleanupZombiesAtTop();

                if (_instanceStack.Count > 0)
                {
                    var parent = _instanceStack.Peek();
                    if (parent.ExitState == 0 && parent.State == ProcedureState.Suspended)
                    {
                        parent.State = ProcedureState.Active;
                        EF.Events.Publish(new ProcedureResumeEvent
                        {
                            Uid = parent.UID,
                            ProcedureType = parent.ProcedureType,
                            Depth = parent.Depth
                        });
                    }
                }
            }
        }

        private void CleanupZombiesAtTop()
        {
            while (_instanceStack.Count > 0)
            {
                var top = _instanceStack.Peek();
                if (top == null || top.State != ProcedureState.Exited)
                    break;

                _instanceStack.Pop();
                // 僵尸实例的资源已在前序步骤中回收，只重置实例本身
                top.Reset();
                PoolManager.Instance.ReturnToPool(top);
            }
        }

        private UniTask PopAndResumeAsync(ProcedureInstance inst)
        {
            return PopAndResumeInternal(inst);
        }

        private async UniTask TimeoutAsync(ProcedureInstance inst)
        {
            uint version = inst.RuntimeVersion;
            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(defaultTimeoutSeconds),
                    cancellationToken: inst.EnterTimeoutCts.Token);

                if (inst.RuntimeVersion != version) return;
                if (inst.ExitState != 0) return;
                if (inst.State != ProcedureState.Entering) return;

                inst.State = ProcedureState.Timeout;
                Debug.LogWarning($"Procedure Timeout: {inst.UID}");
                RequestExit(inst);
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogError(e); }
        }

        private void CancelTimeoutSafe(ProcedureInstance inst)
        {
            try { inst.EnterTimeoutCts?.Cancel(); } catch { }
        }

        private bool WouldExceedChainLimit(Type targetType, uint parentUid)
        {
            int count = 0;
            uint uid = parentUid;
            while (uid != 0)
            {
                var inst = GetInstanceByUid(uid);
                if (inst == null) break;
                if (inst.ProcedureType == targetType) count++;
                uid = inst.ParentUID;
            }
            return count >= maxChainRepeat;
        }

        private ProcedureInstance GetActiveInstance()
        {
            CleanupZombiesAtTop();
            return _instanceStack.Count > 0 ? _instanceStack.Peek() : null;
        }

        private ProcedureInstance GetInstanceByUid(uint uid)
        {
            _uidToInstance.TryGetValue(uid, out var inst);
            return inst;
        }

        private void ForceClear()
        {
            _pendingExits.Clear();
            _processingExit = false;
            while (_instanceStack.Count > 0)
            {
                var inst = _instanceStack.Pop();
                CancelTimeoutSafe(inst);
                try
                {
                    if (inst.Context != null)
                    {
                        inst.Context.IsDisposed = true;
                        PoolManager.Instance.ReturnToPool(inst.Context);
                        inst.Context = null;
                    }

                    if (inst.Params != null)
                    {
                        PoolManager.Instance.ReturnToPool(inst.Params);
                        inst.Params = null;
                    }
                }
                catch (Exception e) { Debug.LogError(e); }
                inst.CompletionSource?.TrySetResult();
                if (inst.State != ProcedureState.None)
                {
                    inst.Reset();
                    PoolManager.Instance.ReturnToPool(inst);
                }
            }
            _uidToInstance.Clear();
        }
    }
}