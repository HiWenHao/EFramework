/*
 * ================================================
 * Describe:      全局流程管理器 - 串行退出、安全并发
 * Author:        Alvin5100
 * Modification:  支持返回 ProcedureResult，精简状态
 * ScriptVersion: 1.0
 * ===============================================
 */

using System;
using System.Collections.Concurrent;
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
        [SerializeField] 
        private float leaveTimeoutSeconds = 30f; // OnLeave 超时时间（秒）
        
        private uint _nextUid = 1;          // 下一个可用的流程 Uid
        private uint _runtimeVersionSeed;   // 递增版本号
        private int _processingExit;        // 是否正在处理退出队列（防止重入） 0 = 空闲，1 = 正在处理
        private bool _openDebug;            // 是否开启调试日志
        private bool _isClearing;           // 是否正在清空流程栈（Switch 期间）
        private Stack<ProcedureInstance> _instanceStack;            // 流程实例栈（栈顶为当前活动流程）
        private Dictionary<Type, Func<IProcedure>> _factories;      // 类型 -> 工厂方法
        private ConcurrentQueue<ProcedureInstance> _pendingExits;   // 待退出的流程队列
        private Dictionary<uint, ProcedureInstance> _uidToInstance; // Uid -> 流程实例 映射

        /// <summary>
        /// 是否有正在运行的流程
        /// </summary>
        public bool HasRunningProcedure => _instanceStack.Count > 0;

        void ISingleton.Init()
        {
            _instanceStack = new Stack<ProcedureInstance>();
            _factories = new Dictionary<Type, Func<IProcedure>>();
            _pendingExits = new ConcurrentQueue<ProcedureInstance>();
            _uidToInstance = new Dictionary<uint, ProcedureInstance>();
            
            var pool = PoolManager.Instance;
            pool.CreateObjectPool(4096, () => new ProcedureContext(), x => x.Reset());
            pool.CreateObjectPool(4096, () => new ProcedureInstance(), x => x.Reset());
            pool.CreateObjectPool(4096, () => new Dictionary<string, object>(8), x => x.Clear());
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
                    D.Error(e);
                    RequestExit(inst, ProcedureExitType.Exception, e);
                }
            }

            ProcessPendingExits().Forget();
        }
        
        void ISingleton.Quit()
        {
            _pendingExits.Clear();
            _processingExit = 0;
            _isClearing = false;
            
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
                catch (Exception e) { D.Error(e); }
                inst.CompletionSource?.TrySetResult();
                if (inst.State != ProcedureState.None)
                {
                    inst.Reset();
                    PoolManager.Instance.ReturnToPool(inst);
                }
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
                    await PopAndResumeInternal(top, ProcedureExitType.Cancelled);
                }
            }
            finally
            {
                _isClearing = false;
            }

            await StartProcedureInternal(typeof(T), parameters, 0, false, null);
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
            await StartSubProcedureAndWaitWithResult<T>(parentCtx, parameters);
        }

        // 带返回值版本
        internal async UniTask<ProcedureResult> StartSubProcedureAndWaitWithResult<T>(ProcedureContext parentCtx, Dictionary<string, object> parameters) where T : IProcedure
        {
            var parentInst = GetInstanceByUid(parentCtx.Uid);
            if (parentInst == null || !_uidToInstance.ContainsKey(parentCtx.Uid) || parentInst.ExitState != 0)
            {
                D.Error("Parent procedure not found or already exited.");
                return new ProcedureResult { IsSuccess = false, Reason = ProcedureExitType.Cancelled };
            }

            if (parentInst.State != ProcedureState.Active && parentInst.State != ProcedureState.Entering)
            {
                D.Error("Parent procedure is not active.");
                return new ProcedureResult { IsSuccess = false, Reason = ProcedureExitType.Cancelled };
            }

            SuspendInstance(parentInst);

            var resultSource = new UniTaskCompletionSource<ProcedureResult>();
            bool started = await StartProcedureInternal(typeof(T), parameters, parentCtx.Uid, true, resultSource);

            // 等待子流程退出结果
            if (started) 
                return await resultSource.Task;
            
            // 未能入栈，立即恢复父流程并返回失败结果
            if (parentInst.State != ProcedureState.Suspended)
                return new ProcedureResult { IsSuccess = false, Reason = ProcedureExitType.NotRegistered };
                
            parentInst.State = ProcedureState.Active;
            EF.Events.Publish(new ProcedureResumeEvent(parentInst.Uid, parentInst.ProcedureType, parentInst.Depth));
            return new ProcedureResult { IsSuccess = false, Reason = ProcedureExitType.NotRegistered }; // 具体原因会在内部设置，这里简化
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
                D.Error("EndProcedure mismatch: not the active instance.");
                return;
            }

            RequestExit(inst, ProcedureExitType.Completed);
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
        private async UniTask<bool> StartProcedureInternal(
            Type targetType,
            Dictionary<string, object> parameters,
            uint parentUid,
            bool waitForExit,
            UniTaskCompletionSource<ProcedureResult> resultSource)
        {
            // 深度检查
            int newDepth = parentUid == 0 ? 1 : (GetInstanceByUid(parentUid)?.Depth + 1 ?? 1);
            if (newDepth > maxDepth)
            {
                D.Error($"MaxDepth reached: {targetType.Name}");
                resultSource?.TrySetResult(new ProcedureResult { IsSuccess = false, Reason = ProcedureExitType.DepthExceeded });
                return false;
            }

            // 链式重复检查
            if (parentUid != 0 && WouldExceedChainLimit(targetType, parentUid))
            {
                D.Error("Chain limit exceeded");
                resultSource?.TrySetResult(new ProcedureResult { IsSuccess = false, Reason = ProcedureExitType.ChainRepeated });
                return false;
            }

            // 注册检查
            if (!_factories.TryGetValue(targetType, out var factory))
            {
                D.Error($"Unregistered Procedure: {targetType}");
                resultSource?.TrySetResult(new ProcedureResult { IsSuccess = false, Reason = ProcedureExitType.NotRegistered });
                return false;
            }

            // 创建实例
            var inst = PoolManager.Instance.GetFromPool<ProcedureInstance>();
            inst.Uid = _nextUid++;
            inst.ParentUid = parentUid;
            inst.Depth = newDepth;
            inst.ProcedureType = targetType;
            inst.Procedure = factory();
            inst.State = ProcedureState.Entering;
            inst.CompletionSource = new UniTaskCompletionSource();
            inst.ResultSource = resultSource; // 可能为 null

            // 参数处理
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
            inst.RuntimeVersion = ++_runtimeVersionSeed;
            ctx.RuntimeVersion = inst.RuntimeVersion;
            ctx.Params = paramDict;
            inst.Context = ctx;

            inst.LifecycleCts = new CancellationTokenSource();
            inst.EnterTimeoutCts = new CancellationTokenSource();

            _instanceStack.Push(inst);
            _uidToInstance[inst.Uid] = inst;

            EF.Events.Publish(new ProcedureEnterEvent(inst.Uid, parentUid, targetType, newDepth));

            TimeoutAsync(inst.Uid, inst.RuntimeVersion, inst.EnterTimeoutCts.Token).Forget();

            try
            {
                await inst.Procedure.OnEnter(ctx, inst.LifecycleCts.Token);

                if (inst.ExitState == 0 && inst.State == ProcedureState.Entering)
                {
                    inst.State = ProcedureState.Active;
                    CancelTimeoutSafe(inst);
                    EF.Events.Publish(new ProcedureActivateEvent(inst.Uid, targetType, newDepth));
                }
            }
            catch (OperationCanceledException e)
            {
                var reason =
                    inst.EnterTimeoutCts.IsCancellationRequested
                        ? ProcedureExitType.Timeout
                        : ProcedureExitType.Cancelled;

                RequestExit(inst, reason, e);

                await ProcessPendingExits();
            }
            catch (Exception e)
            {
                D.Error(e);
                RequestExit(inst, ProcedureExitType.Exception, e);
                // 立即处理退出队列，使子流程尽快退出
                await ProcessPendingExits();
                // 注意：子流程已入栈，返回 true，等待退出流程自然恢复父流程
                return true;
            }

            if (waitForExit)
            {
                var completion = inst.CompletionSource;
                if (completion != null)
                    await completion.Task;
            }
            return true;
        }

        // 挂起指定实例（暂停更新）
        private void SuspendInstance(ProcedureInstance inst)
        {
            if (inst.State != ProcedureState.Active && inst.State != ProcedureState.Entering)
                return;

            inst.State = ProcedureState.Suspended;
            CancelTimeoutSafe(inst);
            EF.Events.Publish(new ProcedureSuspendEvent(inst.Uid, inst.ProcedureType, inst.Depth));
        }

        // 请求退出指定实例（加入待退出队列）
        private void RequestExit(ProcedureInstance inst, ProcedureExitType reason = ProcedureExitType.Completed, Exception exception = null)
        {
            if (inst == null) return;
            if (inst.State == ProcedureState.Exited) return;
            if (Interlocked.Exchange(ref inst.ExitQueued, 1) == 1) return;

            inst.ExitReason = reason;
            inst.ExitException = exception;
            
            _pendingExits.Enqueue(inst);
        }

        // 处理待退出队列
        private async UniTask ProcessPendingExits()
        {
            if (Interlocked.CompareExchange(ref _processingExit, 1, 0) != 0)
                return;

            try
            {
                while (_pendingExits.TryDequeue(out var inst))
                {
                    if (inst == null) continue;
                    try
                    {
                        await PopAndResumeInternal(inst);
                    }
                    catch (Exception e)
                    {
                        D.Error(e);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _processingExit, 0);
            }
        }

        // 执行退出并清理实例（幂等，可处理已退出实例的回收）
        private async UniTask PopAndResumeInternal(ProcedureInstance inst, ProcedureExitType defaultReason = ProcedureExitType.Completed)
        {
            if (inst == null) return;

            // 首次退出标记，负责执行 OnLeave 及发布事件
            if (Interlocked.Exchange(ref inst.ExitState, 1) == 0)
            {
                inst.State = ProcedureState.Exiting;
                CancelTimeoutSafe(inst);

                EF.Events.Publish(new ProcedureExitEvent(inst.Uid, inst.ProcedureType, inst.Depth));

                try
                {
                    inst.LifecycleCts?.Cancel();

                    using var leaveCts = new CancellationTokenSource();
                    var leaveTimeout = TimeSpan.FromSeconds(leaveTimeoutSeconds);
                    leaveCts.CancelAfter(leaveTimeout);

                    var leaveTask = inst.Procedure.OnLeave(leaveCts.Token);
                    var timeoutTask = UniTask.Delay(leaveTimeout, cancellationToken: leaveCts.Token);

                    var winIndex = await UniTask.WhenAny(leaveTask, timeoutTask);
                    if (winIndex == 1) // 超时任务先完成
                    {
                        D.Error($"OnLeave timeout for procedure UID={inst.Uid}, Type={inst.ProcedureType?.Name}. Force exiting.");
                    }

                    inst.LifecycleCts?.Dispose();
                    inst.EnterTimeoutCts?.Dispose();
                }
                catch (Exception e)
                {
                    D.Error(e);
                }
                finally
                {
                    inst.LifecycleCts = null;
                    inst.EnterTimeoutCts = null;
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
                D.Error(
                    $"Illegal non-top exit detected. Uid={inst.Uid}, Type={inst.ProcedureType?.Name}. " +
                    "Instance will be removed from lookup but stack structure preserved.");
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
                // 设置结果（如果有 ResultSource）
                if (inst.ResultSource != null)
                {
                    // 根据实际情况构造结果，这里简化
                    var result = new ProcedureResult { IsSuccess = false, Reason = ProcedureExitType.Cancelled };
                    inst.ResultSource.TrySetResult(result);
                }
                inst.State = ProcedureState.Exited;
                return;
            }

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
            
            if (inst.ResultSource != null)
            {
                var result = new ProcedureResult
                {
                    Reason = inst.ExitReason,
                    Exception = inst.ExitException,
                    IsSuccess = inst.ExitReason == ProcedureExitType.Completed,
                };
                inst.ResultSource.TrySetResult(result);
            }

            if (inst.State != ProcedureState.None)
            {
                inst.Reset();
                PoolManager.Instance.ReturnToPool(inst);
            }

            if (!_isClearing && _instanceStack.Count > 0)
            {
                CleanupZombiesAtTop();

                if (_instanceStack.Count > 0)
                {
                    var parent = _instanceStack.Peek();
                    if (parent.ExitQueued == 0 && parent.ExitState == 0 && parent.State == ProcedureState.Suspended)
                    {
                        parent.State = ProcedureState.Active;
                        EF.Events.Publish(new ProcedureResumeEvent(parent.Uid, parent.ProcedureType, parent.Depth));
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

                // 超时：不再设置 Timeout 状态（已移除），而是直接发布事件并请求退出
                EF.Events.Publish(new ProcedureTimeoutEvent(inst.Uid, inst.ProcedureType, inst.Depth));
        
                Warning($"Procedure Timeout: {inst.Uid}");
                RequestExit(inst, ProcedureExitType.Timeout);
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { D.Error(e); }
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
    }
}