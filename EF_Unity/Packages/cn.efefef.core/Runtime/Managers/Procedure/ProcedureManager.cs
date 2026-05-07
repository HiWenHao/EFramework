/*
 * ================================================
 * Describe:      全局流程管理器 - 支持嵌套、UID、风控
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 15:19:27
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 15:19:27
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Edit;
using UnityEngine;

namespace EasyFramework.Managers.Procedure
{
    /// <summary>
    /// 流程管理器
    /// </summary>
    public class ProcedureManager : MonoSingleton<ProcedureManager>, IManager, IUpdate
    {
        private uint _nextUid = 1;   // 自增UID 
        private uint _activeUid;
        private bool _isSwitching;  // 切换锁，防止并发切换
        private Stack<ProcedureInstance> _instanceStack;        // 流程实例栈（栈顶为当前活动流程）
        private Dictionary<Type, IProcedure> _procedureTypes;   // 流程类型注册表
        
        [HeaderPro("风控配置,最大嵌套层级", "Risk control configuration, maximum nesting level")] [SerializeField]
        private int maxDepth = 100;
        [SerializeField] private int maxChainRepeat = 5;
        [SerializeField] private float defaultTimeoutSeconds = 300f;

        void ISingleton.Init()
        {
            _nextUid = 1;
            _activeUid = 0;
            _isSwitching = false;
            _instanceStack = new Stack<ProcedureInstance>();
            _procedureTypes = new Dictionary<Type, IProcedure>();
        }

        void IUpdate.Update(float elapse, float realElapse)
        {
            var active = GetActiveInstance();
            if (active is not { IsActive: true } || _isSwitching)
                return;

            active.Procedure.OnUpdate(elapse, realElapse);
        }

        void ISingleton.Quit()
        {
            _activeUid = 0;
            while (_instanceStack.Count > 0)
            {
                var inst = _instanceStack.Pop();
                inst.Procedure?.OnLeave().Forget();
                inst.Dispose();
            }
        }

        /// <summary>
        /// 从流程内部调用：启动子流程
        /// </summary>
        internal async UniTask StartSubProcedureInternal<T>(ProcedureContext parentCtx, object parameters)
            where T : IProcedure
        {
            await SwitchProcedure(typeof(T), parameters, parentUid: parentCtx.UID);
        }

        /// <summary>
        /// 从流程内部调用：结束当前流程
        /// </summary>
        internal void EndProcedureInternal(ProcedureContext ctx)
        {
            var inst = GetActiveInstance();
            if (inst == null || inst.UID != ctx.UID)
            {
                Debug.LogError($"[ProcedureManager] EndProcedure 调用不匹配：当前活动实例 UID={inst?.UID}, 上下文 UID={ctx.UID}");
                return;
            }

            // 异步触发退出（会在帧末处理）
            PopAndResumeAsync(inst).Forget();
        }

        private async UniTask SwitchProcedure(Type targetType, object parameters, uint parentUid)
        {
            if (_isSwitching && _activeUid == 0)
            {
                Debug.LogWarning("[ProcedureManager] 正在切换中，新的切换请求被忽略");
                return;
            }

            // 深度检查
            int newDepth = parentUid == 0 ? 1 : GetInstanceByUid(parentUid)?.Depth + 1 ?? 1;
            if (newDepth > maxDepth)
            {
                Debug.LogError($"[ProcedureManager] 达到最大嵌套深度 {maxDepth}，无法启动 {targetType.Name}");
                return;
            }

            // 循环检测
            if (parentUid != 0 && WouldExceedChainLimit(targetType, parentUid))
            {
                Debug.LogError($"[ProcedureManager] 检测到流程链上 {targetType.Name} 重复次数超过限制 {maxChainRepeat}，拒绝启动");
                return;
            }

            _isSwitching = true;

            try
            {
                // 挂起当前活动流程
                var currentInst = GetActiveInstance();
                if (currentInst != null)
                {
                    currentInst.IsActive = false;
                    // 发布离开事件（注意：不是退出，只是挂起）
                    EF.Events.Publish(new ProcedureSuspendEvent
                    {
                        Uid = currentInst.UID,
                        ProcedureType = currentInst.ProcedureType,
                        Depth = currentInst.Depth
                    });
                }

                // 创建新实例
                if (!_procedureTypes.TryGetValue(targetType, out var procTemplate))
                {
                    Debug.LogError($"[ProcedureManager] 未注册的流程类型: {targetType.Name}");
                    _isSwitching = false;
                    return;
                }

                var newInst = new ProcedureInstance
                {
                    UID = _nextUid++,
                    ParentUID = parentUid,
                    Depth = newDepth,
                    ProcedureType = targetType,
                    Procedure = procTemplate, // 直接使用注册时的实例（无状态，可复用）
                    Params = parameters as Dictionary<string, object> ?? new Dictionary<string, object>(),
                    IsActive = true
                };

                var ctx = new ProcedureContext
                {
                    UID = newInst.UID,
                    ParentUID = parentUid,
                    Depth = newDepth,
                    Params = newInst.Params,
                };
                newInst.Context = ctx;

                // 超时设置
                newInst.TimeoutCts = new CancellationTokenSource();
                TimeoutAsync(newInst).Forget();

                // 入栈
                _instanceStack.Push(newInst);

                // 发布进入事件
                EF.Events.Publish(new ProcedureEnterEvent
                {
                    Uid = newInst.UID,
                    ParentUid = parentUid,
                    ProcedureType = targetType,
                    Depth = newDepth
                });

                // 执行 OnEnter
                await newInst.Procedure.OnEnter(ctx);

                // 如果没有在 OnEnter 里被异常退出或切换，则正常激活
                if (newInst.IsActive && !newInst.IsExiting)
                {
                    EF.Events.Publish(new ProcedureActivateEvent
                    {
                        Uid = newInst.UID,
                        ProcedureType = targetType,
                        Depth = newDepth
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProcedureManager] 流程切换异常: {e}");
            }
            finally
            {
                _isSwitching = false;
            }
        }

        /// <summary>弹出栈顶实例并恢复父流程</summary>
        private async UniTask PopAndResumeAsync(ProcedureInstance inst)
        {
            if (inst.IsExiting) return;
            inst.IsExiting = true;
            inst.IsActive = false;

            // 取消超时
            inst.TimeoutCts?.Cancel();

            // 发布离开事件
            EF.Events.Publish(new ProcedureLeaveEvent
            {
                Uid = inst.UID,
                ProcedureType = inst.ProcedureType,
                Depth = inst.Depth
            });

            // 调用 OnLeave
            try
            {
                await inst.Procedure.OnLeave();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProcedureManager] OnLeave 异常: UID={inst.UID}, {e}");
            }

            // 从栈中移除
            if (_instanceStack.Count > 0 && _instanceStack.Peek() == inst)
            {
                _instanceStack.Pop();
                inst.Dispose();
            }
            else
            {
                Debug.LogError("[ProcedureManager] 栈顶不一致，跳过弹出");
            }

            // 恢复父流程（如果存在）
            if (_instanceStack.Count > 0)
            {
                var parentInst = _instanceStack.Peek();
                parentInst.IsActive = true;
                EF.Events.Publish(new ProcedureResumeEvent
                {
                    Uid = parentInst.UID,
                    ProcedureType = parentInst.ProcedureType,
                    Depth = parentInst.Depth
                });
            }
        }

        private async UniTask TimeoutAsync(ProcedureInstance inst)
        {
            try
            {
                float timeout = defaultTimeoutSeconds;
                // 可以后续在流程元数据中自定义超时，这里先用默认值
                await UniTask.Delay(TimeSpan.FromSeconds(timeout), cancellationToken: inst.TimeoutCts.Token);
                Debug.LogWarning($"[ProcedureManager] 流程 {inst.ProcedureType.Name} (UID={inst.UID}) 超时，强制退出");
                PopAndResumeAsync(inst).Forget();
            }
            catch (OperationCanceledException)
            {
            }
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
            return _instanceStack.Count > 0 ? _instanceStack.Peek() : null;
        }

        private ProcedureInstance GetInstanceByUid(uint uid)
        {
            foreach (var inst in _instanceStack)
            {
                if (inst.UID == uid) return inst;
            }

            return null;
        }
        
        #region 外部调用函数
        
        /// <summary>
        /// 注册流程类型（只需注册一次，实例会自动创建）
        /// </summary>
        public void Register<T>(T procedure) where T : IProcedure
        {
            _procedureTypes[typeof(T)] = procedure;
        }

        /// <summary>
        /// 外部启动第一个流程（只能在栈空时调用）
        /// </summary>
        public async UniTask Switch<T>(object parameters = null) where T : IProcedure
        {
            await SwitchProcedure(typeof(T), parameters, parentUid: 0);
        }

        #endregion
    }
}