/*
 * ================================================
 * Describe:        全局流程管理器 - 串行退出、安全并发
 * Author:          Alvin5100
 * CreationTime:    2026-05-07 18:49:57
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:07:04
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Edit;
using EasyFramework.Managers;
using EasyFramework.Systems.Event;
using EasyFramework.Systems.Pool;
using UnityEngine;

namespace EasyFramework.Systems.Procedure
{
    /// <summary>
    /// 流程系统
    /// </summary>
    [Manager]
    [Dependency(typeof(PoolSystem))]
    [Dependency(typeof(EventSystem))]
    public sealed class ProcedureSystem : MonoSingleton<ProcedureSystem>, ISingleton, IUpdate
    {
        [HeaderPro("最大嵌套深度限制", "Maximum nesting depth limit")] [SerializeField]
        private int maxDepth = 100;

        [HeaderPro("链式同类型重复最大次数", "Maximum number of consecutive same type repetitions")] [SerializeField]
        private int maxChainRepeat = 5;

        [HeaderPro("进入流程默认超时时间（秒）", "Default timeout time for entering the process (seconds)")] [SerializeField]
        private float defaultTimeoutSeconds = 300f;

        [HeaderPro("OnLeave 超时时间（秒）", "OnLeave timeout duration (seconds)")] [SerializeField]
        private float leaveTimeoutSeconds = 30f; // 
        
        private long _nextUid = 1;          // 下一个可用的流程 Uid
        private uint _runtimeVersionSeed;   // 递增版本号
        private int _processingExit;        // 是否正在处理退出队列（防止重入） 0 = 空闲，1 = 正在处理
        private bool _isClearing;           // 是否正在清空流程栈（Switch 期间）
        private Stack<ProcedureInstance> _instanceStack;            // 流程实例栈（栈顶为当前活动流程）
        private Dictionary<Type, Func<IProcedure>> _factories;      // 类型 -> 工厂方法
        private ConcurrentQueue<ProcedureInstance> _pendingExits;   // 待退出的流程队列

        /// <summary>
        /// 是否有正在运行的流程
        /// </summary>
        public bool HasRunningProcedure => _instanceStack.Count > 0;

        void ISingleton.Init()
        {
            _instanceStack = new Stack<ProcedureInstance>();
            _factories = new Dictionary<Type, Func<IProcedure>>();
            _pendingExits = new ConcurrentQueue<ProcedureInstance>();
            
            EF.Pool.CreateObjectPool(4096, () => new ProcedureContext(), x => x.Reset());
            EF.Pool.CreateObjectPool(4096, () => new ProcedureInstance(), x => x.Reset());
            EF.Pool.CreateObjectPool(4096, () => new Dictionary<string, object>(8), x => x.Clear());
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
                    RequestExit(inst, ProcedureExitType.Exception, e);
                }
            }

            if (Interlocked.CompareExchange(ref _processingExit, 1, 0) != 0) 
                return;
            
            if (!_pendingExits.IsEmpty)
                ProcessPendingExits().Forget(Exception);
            else
                Interlocked.Exchange(ref _processingExit, 0); // 无需处理，恢复标记
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
                        EF.Pool.ReturnToPool(inst.Context);
                        inst.Context = null;
                    }

                    if (inst.Params != null)
                    {
                        EF.Pool.ReturnToPool(inst.Params);
                        inst.Params = null;
                    }
                }
                catch (Exception e) { Error(e); }
                inst.CompletionSource?.TrySetResult();
                inst.ResultSource?.TrySetResult(new ProcedureResult(false, ProcedureExitType.Cancelled));
                if (inst.State != ProcedureState.None)
                {
                    inst.Reset();
                    EF.Pool.ReturnToPool(inst);
                }
            }
            _instanceStack.Clear();
            _factories.Clear();
            
            _nextUid = 1;
        }

        #region 公开函数
        
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
            // 抢占退出处理权，并清空待处理队列
            Interlocked.Exchange(ref _processingExit, 1);
            while (_pendingExits.TryDequeue(out _)) { } // 清空队列
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
                Interlocked.Exchange(ref _processingExit, 0);
                CleanupZombiesAtTop();
            }

            await StartProcedureInternal(typeof(T), parameters, 0, false, null);
        }
        
        #endregion

        #region 内部函数

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
            var parentInst = FindInstance(parentCtx.Uid);
            if (parentInst is not { ExitState: 0 })
            {
                Error("Parent procedure not found or already exited.");
                return new ProcedureResult(false, ProcedureExitType.Cancelled);
            }

            if (parentInst.State != ProcedureState.Active && parentInst.State != ProcedureState.Entering)
            {
                Error("Parent procedure is not active.");
                return new ProcedureResult(false, ProcedureExitType.Cancelled);
            }

            SuspendInstance(parentInst);

            var resultSource = new UniTaskCompletionSource<ProcedureResult>();
            bool started = await StartProcedureInternal(typeof(T), parameters, parentCtx.Uid, true, resultSource);

            if (started) 
                return await resultSource.Task;
            
            if (parentInst.State == ProcedureState.Suspended)
            {
                parentInst.State = ProcedureState.Active;
                EF.Events.Publish(new ProcedureResumeEvent(parentInst.Uid, parentInst.ProcedureType, parentInst.Depth));
            }

            return new ProcedureResult(false, ProcedureExitType.NotRegistered);
        }

        /// <summary>
        /// 结束当前流程（主动退出），由上下文调用
        /// <para>End current procedure (voluntary exit), called by context</para>
        /// </summary>
        /// <param name="ctx">当前流程上下文</param>
        /// <returns>异步操作，返回是否成功退出</returns>
        internal async UniTask<bool> EndProcedureInternal(ProcedureContext ctx)
        {
            var inst = GetActiveInstance();
            if (inst == null || inst.Uid != ctx.Uid)
            {
                Error("EndProcedure mismatch: not the active instance.");
                return false;
            }

            try { inst.LifecycleCts?.Cancel(); } catch { /* ignore */ }

            RequestExit(inst, ProcedureExitType.Completed);
            var completion = inst.CompletionSource;
            if (completion != null)
                await completion.Task;
            return true;
        }

        /// <summary>
        /// 根据 Uid 获取内部实例（供上下文检查释放状态）
        /// <para>Get internal instance by Uid (for context disposal check)</para>
        /// </summary>
        /// <param name="uid">实例 Uid</param>
        /// <returns>流程实例，可能为 null</returns>
        internal ProcedureInstance GetInstanceByUidInternal(long uid)
        {
            return FindInstance(uid);
        }
        
        #endregion

        #region 私有函数

        // 启动流程内部逻辑
        private async UniTask<bool> StartProcedureInternal(
            Type targetType,
            Dictionary<string, object> parameters,
            long parentUid,
            bool waitForExit,
            UniTaskCompletionSource<ProcedureResult> resultSource)
        {
            // 前置校验（深度、链式重复、注册检查）保持原样...
            int newDepth = parentUid == 0 ? 1 : (FindInstance(parentUid)?.Depth + 1 ?? 1);
            if (newDepth > maxDepth)
            {
                Error($"MaxDepth reached: {targetType.Name}");
                resultSource?.TrySetResult(new ProcedureResult(false, ProcedureExitType.DepthExceeded));
                return false;
            }

            if (parentUid != 0 && WouldExceedChainLimit(targetType, parentUid))
            {
                Error("Chain limit exceeded");
                resultSource?.TrySetResult(new ProcedureResult(false, ProcedureExitType.ChainRepeated));
                return false;
            }

            if (!_factories.TryGetValue(targetType, out var factory))
            {
                Error($"Unregistered Procedure: {targetType}");
                resultSource?.TrySetResult(new ProcedureResult(false, ProcedureExitType.NotRegistered));
                return false;
            }

            // 资源与状态
            bool pushed = false;
            ProcedureInstance inst = null;
            Dictionary<string, object> paramDict = null;
            ProcedureContext ctx = null;

            try
            {
                // ------ 阶段1：创建与入栈（任何异常都视为未启动，需清理并返回 false）------
                inst = EF.Pool.GetFromPool<ProcedureInstance>();
                inst.Uid = _nextUid++;
                inst.ParentUid = parentUid;
                inst.Depth = newDepth;
                inst.ProcedureType = targetType;
                inst.Procedure = factory();                 // 可能异常
                inst.State = ProcedureState.Entering;
                inst.CompletionSource = new UniTaskCompletionSource();
                inst.ResultSource = resultSource;

                paramDict = EF.Pool.GetFromPool<Dictionary<string, object>>();
                paramDict.Clear();
                if (parameters != null)
                {
                    foreach (var kv in parameters)
                        paramDict[kv.Key] = kv.Value;
                }
                inst.Params = paramDict;

                ctx = EF.Pool.GetFromPool<ProcedureContext>();
                ctx.Uid = inst.Uid;
                ctx.ParentUid = parentUid;
                ctx.Depth = newDepth;
                inst.RuntimeVersion = ++_runtimeVersionSeed;
                ctx.RuntimeVersion = inst.RuntimeVersion;
                ctx.Params = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(paramDict);
                inst.Context = ctx;

                inst.LifecycleCts = new CancellationTokenSource();
                inst.EnterTimeoutCts = new CancellationTokenSource();

                // 正式入栈
                _instanceStack.Push(inst);
                pushed = true;

                // ------ 阶段2：进入异步逻辑（已入栈，异常通过退出流程处理）------
                EF.Events.Publish(new ProcedureEnterEvent(inst.Uid, parentUid, targetType, newDepth));
                
                TimeoutAsync(inst.Uid, inst.RuntimeVersion, inst.EnterTimeoutCts.Token).Forget(Exception);

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
                if (pushed)
                {
                    // 已入栈的取消：可能是超时或 LifecycleCts 取消，走正常退出流程
                    var reason = inst.EnterTimeoutCts.IsCancellationRequested
                        ? ProcedureExitType.Timeout
                        : ProcedureExitType.Cancelled;
                    RequestExit(inst, reason, e);
                    if (inst.CompletionSource != null)
                        await inst.CompletionSource.Task; 
                    return true;
                }
                
                // 未入栈就取消（极端边缘情况），清理资源
                CleanupResources(inst, paramDict, ctx);
                return false;
            }
            catch (Exception e)
            {
                Error(e);
                if (pushed)
                {
                    // 已入栈后异常（包含 OnEnter 异常），触发退出流程
                    RequestExit(inst, ProcedureExitType.Exception, e);
                    if (inst.CompletionSource != null)
                        await inst.CompletionSource.Task;
                    return true;    // 已入栈，退出流程会接管后续
                }
                
                // 未入栈异常（factory() / 池分配等），安全清理并通知调用方启动失败
                CleanupResources(inst, paramDict, ctx);
                return false;
            }

            // 等待子流程完全退出（如果调用方需要）
            if (waitForExit)
            {
                var completion = inst.CompletionSource;
                if (completion != null)
                    await completion.Task;
            }
            return true;
        }

        // 辅助清理方法
        private void CleanupResources(ProcedureInstance inst, Dictionary<string, object> paramDict, ProcedureContext ctx)
        {
            if (ctx != null)
            {
                ctx.IsDisposed = true;
                EF.Pool.ReturnToPool(ctx);
            }
            if (paramDict != null)
            {
                EF.Pool.ReturnToPool(paramDict);
            }

            if (inst == null) return;

            // 【移除】对 CompletionSource 和 ResultSource 的强制结束
            // 因为此时流程未入栈，外部不会等待这些源，且错误原因未知，强制设置会掩盖真实情况。

            inst.Reset();
            EF.Pool.ReturnToPool(inst);
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
            if (inst.ExitState != 0) return;
            if (Interlocked.Exchange(ref inst.ExitQueued, 1) == 1) return;

            var top = GetActiveInstance();
            if (inst != top)
            {
                Error($"Attempt to exit non-top procedure Uid={inst.Uid}, Type={inst.ProcedureType?.Name}. Request ignored.");
                Interlocked.Exchange(ref inst.ExitQueued, 0);
                return;
            }

            inst.ExitReason = reason;
            inst.ExitException = exception;
    
            try { inst.LifecycleCts?.Cancel(); } catch { /* ignore */ }
    
            _pendingExits.Enqueue(inst);
    
            if (Interlocked.CompareExchange(ref _processingExit, 1, 0) == 0)
                ProcessPendingExits().Forget(Exception);
        }

        // 处理待退出队列
        private async UniTask ProcessPendingExits()
        {
            // _processingExit 已在调用前设置为 1
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
                        Error(e);
                    }
                    // 循环继续，无需中断，因为可能有新入队的元素
                }
            }
            finally
            {
                Interlocked.Exchange(ref _processingExit, 0);
        
                // 处理期间又有新请求入队？立即再启动一次，避免依赖下一帧 Update
                if (!_pendingExits.IsEmpty && Interlocked.CompareExchange(ref _processingExit, 1, 0) == 0)
                    ProcessPendingExits().Forget(Exception);
            }
        }

        // 执行退出并清理实例（幂等，可处理已退出实例的回收）
        private async UniTask PopAndResumeInternal(ProcedureInstance inst, ProcedureExitType defaultReason = ProcedureExitType.Completed)
        {
            if (inst == null) return;

            // 栈顶校验（仅允许栈顶退出）
            if (_instanceStack.Count == 0 || _instanceStack.Peek() != inst)
            {
                Error($"FATAL: Attempt to exit non-top instance Uid={inst.Uid}, Type={inst.ProcedureType?.Name}. Exit denied.");
                return; // 拒绝，防止破坏栈结构
            }

            // 先将实例从栈中移除，再执行异步 OnLeave，避免清理僵尸实例时发生竞态
            _instanceStack.Pop();
            if (Interlocked.Exchange(ref inst.ExitState, 1) == 0)
            {
                inst.State = ProcedureState.Exiting;
                // 如果调用者指定了非 Completed 的原因（例如 Switch 时传入 Cancelled），应覆盖默认值
                if (defaultReason != ProcedureExitType.Completed)
                    inst.ExitReason = defaultReason;
                
                CancelTimeoutSafe(inst);
                EF.Events.Publish(new ProcedureExitEvent(inst.Uid, inst.ProcedureType, inst.Depth));

                try
                {
                    inst.LifecycleCts?.Cancel();

                    using var leaveCts = new CancellationTokenSource();
                    leaveCts.CancelAfter(TimeSpan.FromSeconds(leaveTimeoutSeconds));
                    try
                    {
                        // 使用 leaveCts.Token，让 OnLeave 能响应超时取消
                        await inst.Procedure.OnLeave(leaveCts.Token);
                    }
                    catch (OperationCanceledException) when (leaveCts.IsCancellationRequested)
                    {
                        Error($"OnLeave timeout for procedure UID={inst.Uid}, Type={inst.ProcedureType?.Name}. Force exiting.");
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消（LifecycleCts 触发），无需额外处理
                    }
                }
                catch (Exception e)
                {
                    Error(e);
                }
                finally
                {
                    try { inst.LifecycleCts?.Dispose(); } catch { /* Ignore */ }
                    try { inst.EnterTimeoutCts?.Dispose(); } catch { /* Ignore */ }
                    inst.LifecycleCts = null;
                    inst.EnterTimeoutCts = null;
                }
            }

            // 实例已从栈中移除，下面进行资源回收和父流程恢复
            inst.CompletionSource?.TrySetResult();
            var result = new ProcedureResult(
                inst.ExitReason == ProcedureExitType.Completed,
                inst.ExitReason,
                inst.ExitException);
            inst.ResultSource?.TrySetResult(result);

            if (inst.Context != null)
            {
                inst.Context.IsDisposed = true;
                EF.Pool.ReturnToPool(inst.Context);
                inst.Context = null;
            }
            if (inst.Params != null)
            {
                EF.Pool.ReturnToPool(inst.Params);
                inst.Params = null;
            }

            if (inst.State != ProcedureState.None)
            {
                inst.Reset();
                EF.Pool.ReturnToPool(inst);
            }

            if (!_isClearing && _instanceStack.Count > 0)
            {
                CleanupZombiesAtTop();
                if (_instanceStack.Count > 0)
                {
                    var parent = _instanceStack.Peek();
                    if (parent.ExitState == 0 && parent.State == ProcedureState.Suspended)
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
                if (top.IsExited)
                {
                    _instanceStack.Pop();
                    top.Reset();
                    EF.Pool.ReturnToPool(top);
                }
                else
                {
                    break;
                }
            }
        }

        // 超时监控
        private async UniTask TimeoutAsync(long uid, uint version, CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(defaultTimeoutSeconds), cancellationToken: token);
        
                var inst = FindInstance(uid);
                if (inst == null) return;
                if (inst.RuntimeVersion != version) return;
                if (inst.ExitState != 0) return;
                if (inst.State != ProcedureState.Entering) return;

                // 超时：不再设置 Timeout 状态（已移除），而是直接发布事件并请求退出
                EF.Events.Publish(new ProcedureTimeoutEvent(inst.Uid, inst.ProcedureType, inst.Depth));
                RequestExit(inst, ProcedureExitType.Timeout);
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
        private bool WouldExceedChainLimit(Type targetType, long parentUid)
        {
            int count = 0;
            long uid = parentUid;
            while (uid != 0)
            {
                var inst = FindInstance(uid);
                if (inst == null || inst.ParentUid == uid) break; // 防止循环
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
        
        // 在栈中按 Uid 查找实例（O(n)，n 很小）
        private ProcedureInstance FindInstance(long uid)
        {
            foreach (var inst in _instanceStack)  // struct enumerator，零 GC
            {
                if (inst.Uid == uid)
                    return inst;
            }
            return null;
        }

        private void Error(object message)
        {
            D.Error($"[ProcedureSystem] {message}");
        }
        private void Exception(object message)
        {
            D.Exception($"[ProcedureSystem] {message}]");
        }
        
        #endregion
    }
}
