/*
 * ================================================
 * Describe:      内部流程实例数据
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 18:49:57
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-08
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.Procedure
{
    /// <summary>
    /// 单流程实例
    /// </summary>
    internal sealed class ProcedureInstance
    {
        public uint Uid;                                    // 流程实例唯一标识符
        public uint ParentUid;                              // 父流程实例 Uid
        public uint RuntimeVersion;                         // 运行时版本，用于检测陈旧引用
        public int Depth;                                   // 嵌套深度
        public int ExitQueued;                              // 退出排队标记（0未排队，1已排队）
        public int ExitState;                               // 是否已开始退出（0未开始，1已开始）
        public Type ProcedureType;                          // 流程的具体类型
        public ProcedureState State;                        // 当前状态
        public IProcedure Procedure;                        // 流程实例对象
        public Exception ExitException;                     // 退出异常
        public ProcedureExitType ExitReason;                // 退出原因
        public ProcedureContext Context;                    // 关联的上下文
        public Dictionary<string, object> Params;           // 启动参数（可变字典）
        public CancellationTokenSource LifecycleCts;        // 生命周期取消令牌源
        public CancellationTokenSource EnterTimeoutCts;     // 进入超时取消令牌源
        public UniTaskCompletionSource CompletionSource;    // 完成通知源，供外部等待流程退出
        public UniTaskCompletionSource<ProcedureResult> ResultSource; // 结果源（可选，用于返回详细结果）

        /// <summary>
        /// 是否已退出
        /// </summary>
        public bool IsExited => ExitState != 0;
        
        /// <summary>
        /// 是否处于活动状态
        /// </summary>
        public bool IsActive => State == ProcedureState.Active;

        public void Reset()
        {
            Uid = 0;
            ExitQueued = 0;
            ParentUid = 0;
            Depth = 0;
            ProcedureType = null;
            Procedure = null;
            Context = null;
            ExitState = 0;
            State = ProcedureState.None;

            Params?.Clear();
            Params = null;

            try
            {
                LifecycleCts?.Cancel();
                LifecycleCts?.Dispose();
            }
            catch
            {
                // ignored
            }

            LifecycleCts = null;

            try
            {
                EnterTimeoutCts?.Cancel();
                EnterTimeoutCts?.Dispose();
            }
            catch
            {
                // ignored
            }

            ExitReason = ProcedureExitType.Completed;
            ResultSource = null;
            ExitException = null;
            EnterTimeoutCts = null;
            CompletionSource = null;
        }
    }
}