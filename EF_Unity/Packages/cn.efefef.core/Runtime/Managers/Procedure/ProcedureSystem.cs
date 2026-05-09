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
    /// <summary>
    /// 流程系统
    /// </summary>
    public sealed class ProcedureSystem : MonoSingleton<ProcedureSystem>, IManager, IUpdate
    {

        [SerializeField] 
        private int maxDepth = 100;                    // 最大嵌套深度限制
        [SerializeField]
        private int maxChainRepeat = 5;                // 链式同类型重复最大次数
        [SerializeField] 
        private float defaultTimeoutSeconds = 300f;    // 进入流程默认超时时间（秒）
        
        private uint _nextUid = 1;          // 下一个可用的流程 Uid
        private bool _openDebug;            // 是否开启调试日志
        private bool _isClearing;           // 是否正在清空流程栈（Switch 期间）
        private bool _processingExit;       // 是否正在处理退出队列（防止重入）
        private Queue<ProcedureInstance> _pendingExits;             // 待退出的流程队列
        private Stack<ProcedureInstance> _instanceStack;            // 流程实例栈（栈顶为当前活动流程）
        private Dictionary<Type, Func<IProcedure>> _factories;      // 类型 -> 工厂方法
        private Dictionary<uint, ProcedureInstance> _uidToInstance; // Uid -> 流程实例 映射

        /// <summary>
        /// 是否有正在运行的流程
        /// <para>Whether there is a running procedure</para>
        /// </summary>
        public bool HasRunningProcedure => _instanceStack.Count > 0;

        void ISingleton.Init()
        {
            _pendingExits =  new Queue<ProcedureInstance>();
            _instanceStack =  new Stack<ProcedureInstance>();
            _factories = new Dictionary<Type, Func<IProcedure>>();
            _uidToInstance = new Dictionary<uint, ProcedureInstance>();
            
            var pool = PoolManager.Instance;
            pool.CreateObjectPool(4096,() => new ProcedureContext(),x => x.Reset());
            pool.CreateObjectPool(4096,() => new ProcedureInstance(),x => x.Reset());
            pool.CreateObjectPool(4096,() => new Dictionary<string, object>(8),x => x.Clear());
        }

        //  驱动当前活动流程并处理待退出队列
        void IUpdate.Update(float elapse, float realElapse)
        {
            var inst = GetActiveInstance();
            if (inst is { IsActive: true })
            {
                try
                {
                    inst.Procedure.OnUpdate(elapse, realElapse);
                }
                catch (Exception e)
                {
                    Error(e);
                    RequestExit(inst);
                }
            }

            ProcessPendingExits().Forget();
        }
        
        void ISingleton.Quit()
        {
            // 清空待退出队列并重置处理标志
            _pendingExits.Clear();
            _processingExit = false;
            _isClearing = false;
            
            // 清理流程栈
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
                catch (Exception e) { Error(e); }
                inst.CompletionSource?.TrySetResult();
                if (inst.State == ProcedureState.None) 
                    continue;
                
                inst.Reset();
                PoolManager.Instance.ReturnToPool(inst);
            }
            _instanceStack.Clear();
            _uidToInstance.Clear();
            _factories.Clear();
            
            _openDebug = false;
            _nextUid = 1;
        }

        /// <summary>
        /// 注册流程类型
        /// <para>Register procedure type (using parameterless constructor)</para>
        /// </summary>
        /// <typeparam name="T">流程类型，必须有无参构造函数</typeparam>
        public void Register<T>() where T : IProcedure, new()
        {
            _factories[typeof(T)] = () => new T();
        }

        /// <summary>
        /// 注册流程类型
        /// <para>Register procedure type (using custom factory delegate)</para>
        /// </summary>
        /// <param name="factory">创建流程实例的工厂方法</param>
        /// <typeparam name="T">流程类型</typeparam>
        public void Register<T>(Func<T> factory) where T : IProcedure
        {
            _factories[typeof(T)] = () => factory();
        }

        /// <summary>
        /// 启动一个新的根流程（先同步清空当前流程栈）
        /// <para>Start a new root procedure (clear current stack synchronously first)</para>
        /// </summary>
        /// <param name="parameters">启动参数</param>
        /// <typeparam name="T">根流程类型</typeparam>
        /// <returns>异步操作，等待根流程完全结束后返回</returns>
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
        /// <para>Called by ProcedureContext to start a sub-procedure and wait for its full exit</para>
        /// </summary>
        /// <param name="parentCtx">父流程上下文</param>
        /// <param name="parameters">启动参数</param>
        /// <typeparam name="T">子流程类型</typeparam>
        /// <returns>异步操作，等待子流程完全退出</returns>
        internal async UniTask StartSubProcedureAndWait<T>(ProcedureContext parentCtx, Dictionary<string, object> parameters) where T : IProcedure
        {
            var parentInst = GetInstanceByUid(parentCtx.Uid);
            if (parentInst == null || !_uidToInstance.ContainsKey(parentCtx.Uid) || parentInst.ExitState != 0)
            {
                Error("Parent procedure not found or already exited.");
                return;
            }

            if (parentInst.State != ProcedureState.Active && parentInst.State != ProcedureState.Entering)
            {
                Error("Parent procedure is not active.");
                return;
            }

            // 记录启动前的活动实例（应为父流程）
            var previousTop = GetActiveInstance();
            
            // 挂起父流程
            SuspendInstance(parentInst);
            
            try
            {
                // 尝试启动子流程
                await StartProcedureInternal(typeof(T), parameters, parentCtx.Uid, true);
            }
            finally
            {
                // 检查子流程是否成功启动并成为新的栈顶
                var currentTop = GetActiveInstance();
                // 如果当前栈顶还是原来的父流程，说明子流程未能启动（或同步失败且未入栈），需要恢复父流程
                if (currentTop == previousTop && parentInst.State == ProcedureState.Suspended)
                {
                    // 恢复父流程
                    parentInst.State = ProcedureState.Active;
                    EF.Events.Publish(new ProcedureResumeEvent
                    {
                        Uid = parentInst.Uid,
                        ProcedureType = parentInst.ProcedureType,
                        Depth = parentInst.Depth
                    });
                    Warning($"Sub-procedure {typeof(T).Name} failed to start, parent procedure resumed.");
                }
            }
        }

        /// <summary>
        /// 结束当前流程（主动退出），由上下文调用
        /// <para>End current procedure (voluntary exit), called by context</para>
        /// </summary>
        /// <param name="ctx">当前流程上下文</param>
        /// <returns>异步操作，等待流程完全退出</returns>
        internal async UniTask EndProcedureInternal(ProcedureContext ctx)
        {
            var inst = GetActiveInstance();
            if (inst == null || inst.Uid != ctx.Uid)
            {
                Error("EndProcedure mismatch: not the active instance.");
                return;
            }

            RequestExit(inst);
            var completion = inst.CompletionSource;
            if (completion != null)
                await completion.Task;
        }

        /// <summary>
        /// 根据 Uid 获取内部实例（供上下文检查释放状态）
        /// <para>Get internal instance by Uid (for context disposal check)</para>
        /// </summary>
        /// <param name="uid">实例 Uid</param>
        /// <returns>流程实例，可能为 null</returns>
        internal ProcedureInstance GetInstanceByUidInternal(uint uid)
        {
            return GetInstanceByUid(uid);
        }

        // 启动流程内部逻辑
        private async UniTask StartProcedureInternal(Type targetType,
            Dictionary<string, object> parameters, uint parentUid, bool waitForExit)
        {
            int newDepth = parentUid == 0 ? 1 : (GetInstanceByUid(parentUid)?.Depth + 1 ?? 1);
            if (newDepth > maxDepth)
            {
                Error($"MaxDepth reached: {targetType.Name}");
                return;
            }

            if (parentUid != 0 && WouldExceedChainLimit(targetType, parentUid))
            {
                Error("Chain limit exceeded");
                return;
            }

            if (!_factories.TryGetValue(targetType, out var factory))
            {
                Error($"Unregistered Procedure: {targetType}");
                return;
            }

            var inst = PoolManager.Instance.GetFromPool<ProcedureInstance>();
            inst.Uid = _nextUid++;
            inst.ParentUid = parentUid;
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
            ctx.Uid = inst.Uid;
            ctx.ParentUid = parentUid;
            ctx.Depth = newDepth;
            ctx.RuntimeVersion = inst.RuntimeVersion;
            ctx.Params = paramDict;
            inst.Context = ctx;

            inst.LifecycleCts = new CancellationTokenSource();
            inst.EnterTimeoutCts = new CancellationTokenSource();

            _instanceStack.Push(inst);
            _uidToInstance[inst.Uid] = inst;

            EF.Events.Publish(new ProcedureEnterEvent
            {
                Uid = inst.Uid,
                ParentUid = parentUid,
                ProcedureType = targetType,
                Depth = newDepth
            });

            TimeoutAsync(inst.Uid, inst.RuntimeVersion, inst.EnterTimeoutCts.Token).Forget();

            try
            {
                await inst.Procedure.OnEnter(ctx, inst.LifecycleCts.Token);

                if (inst.ExitState == 0 && inst.State == ProcedureState.Entering)
                {
                    inst.State = ProcedureState.Active;
                    CancelTimeoutSafe(inst);
                    EF.Events.Publish(new ProcedureActivateEvent
                    {
                        Uid = inst.Uid,
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
                Error(e);
                RequestExit(inst);
                
                if (!waitForExit)
                    return;
                
                var completion = inst.CompletionSource;
                if (completion != null)
                    await completion.Task;
                return;
            }

            if (waitForExit)
            {
                var completion = inst.CompletionSource;
                if (completion != null)
                    await completion.Task;
            }
        }

        // 挂起指定实例（暂停更新）
        private void SuspendInstance(ProcedureInstance inst)
        {
            if (inst.State != ProcedureState.Active && inst.State != ProcedureState.Entering)
                return;

            inst.State = ProcedureState.Suspended;
            CancelTimeoutSafe(inst);
            EF.Events.Publish(new ProcedureSuspendEvent
            {
                Uid = inst.Uid,
                ProcedureType = inst.ProcedureType,
                Depth = inst.Depth
            });
        }

        // 请求退出指定实例（加入待退出队列）
        private void RequestExit(ProcedureInstance inst)
        {
            if (inst == null) return;
            if (inst.State == ProcedureState.Exited) return;
            if (Interlocked.Exchange(ref inst.ExitQueued, 1) == 1) return;

            _pendingExits.Enqueue(inst);
        }

        // 处理待退出队列
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

                    try
                    {
                        await PopAndResumeInternal(inst);
                    }
                    catch (Exception e)
                    {
                        Error(e);
                    }
                }
            }
            finally
            {
                _processingExit = false;
            }
        }

        // 执行退出并清理实例（幂等，可处理已退出实例的回收）
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
                    Uid = inst.Uid,
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
                    Error(e);
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
                Error(
                    $"Illegal non-top exit detected. Uid={inst.Uid}, Type={inst.ProcedureType?.Name}. " +
                    "Instance will be removed from lookup but stack structure preserved.");
                // 不破坏栈，标记为僵尸，由后续 GetActiveInstance 清理
                _uidToInstance.Remove(inst.Uid);
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
            _uidToInstance.Remove(inst.Uid);

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
                    if (parent.ExitQueued == 0 && parent.ExitState == 0 && parent.State == ProcedureState.Suspended)
                    {
                        parent.State = ProcedureState.Active;
                        EF.Events.Publish(new ProcedureResumeEvent
                        {
                            Uid = parent.Uid,
                            ProcedureType = parent.ProcedureType,
                            Depth = parent.Depth
                        });
                    }
                }
            }
        }

        // 清理栈顶已退出的僵尸实例
        private void CleanupZombiesAtTop()
        {
            while (_instanceStack.Count > 0)
            {
                var top = _instanceStack.Peek();
                if (top is not { State: ProcedureState.Exited })
                    break;

                _instanceStack.Pop();
                // 僵尸实例的资源已在前序步骤中回收，只重置实例本身
                top.Reset();
                PoolManager.Instance.ReturnToPool(top);
            }
        }

        // 超时监控
        private async UniTask TimeoutAsync(uint uid, uint version, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(defaultTimeoutSeconds), cancellationToken: token);
        
                var inst = GetInstanceByUid(uid);
                if (inst == null) return;
                if (inst.RuntimeVersion != version) return;
                if (inst.ExitState != 0) return;
                if (inst.State != ProcedureState.Entering) return;

                inst.State = ProcedureState.Timeout;
        
                // 发布超时事件，让外部可以观测到超时
                EF.Events.Publish(new ProcedureTimeoutEvent
                {
                    Uid = inst.Uid,
                    ProcedureType = inst.ProcedureType,
                    Depth = inst.Depth
                });
        
                Warning($"Procedure Timeout: {inst.Uid}");
                RequestExit(inst);
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Error(e); }
        }

        // 安全取消超时
        private void CancelTimeoutSafe(ProcedureInstance inst)
        {
            try { inst.EnterTimeoutCts?.Cancel(); }
            catch
            {
                // ignored
            }
        }

        // 检查链式重复限制
        private bool WouldExceedChainLimit(Type targetType, uint parentUid)
        {
            int count = 0;
            uint uid = parentUid;
            while (uid != 0)
            {
                var inst = GetInstanceByUid(uid);
                if (inst == null) break;
                if (inst.ProcedureType == targetType) count++;
                uid = inst.ParentUid;
            }
            return count >= maxChainRepeat;
        }

        // 获取当前活动实例（栈顶）
        private ProcedureInstance GetActiveInstance()
        {
            CleanupZombiesAtTop();
            return _instanceStack.Count > 0 ? _instanceStack.Peek() : null;
        }

        // 根据 Uid 获取实例
        private ProcedureInstance GetInstanceByUid(uint uid)
        {
            _uidToInstance.TryGetValue(uid, out var inst);
            return inst;
        }

        private void Warning(string msg)
        {
            if (_openDebug)
                D.Warning(msg);
        }
        private void Error(object msg)
        {
            if (_openDebug)
                D.Error(msg);
        }
    }
}