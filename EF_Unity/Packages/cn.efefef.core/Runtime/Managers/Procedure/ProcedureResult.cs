/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin5100
 * CreationTime:    2026-05-09 14:34:57
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:07:04
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Procedure
{
    /// <summary>
    /// 流程执行结果（用于 StartSubProcedureAndWait 返回）
    /// <para>Procedure execution result (returned by StartSubProcedureAndWait).</para>
    /// </summary>
    public readonly struct ProcedureResult
    {
        /// <summary>
        /// 是否正常完成（未异常、未超时、未取消）
        /// <para>Whether it completed normally (no exception, timeout or cancellation).</para>
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// 退出原因
        /// <para>The reason it exited.</para>
        /// </summary>
        public ProcedureExitType Reason { get; }

        /// <summary>
        /// 如果因异常退出，携带异常对象（可能为 null）
        /// <para>The exception object if it exited with an error (may be null).</para>
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 构造流程执行结果
        /// <para>Constructs a procedure execution result.</para>
        /// </summary>
        /// <param name="isSuccess">是否正常完成 <para>Whether it completed normally</para></param>
        /// <param name="reason">退出原因 <para>The exit reason</para></param>
        /// <param name="exception">异常对象（可选） <para>The exception object (optional)</para></param>
        public ProcedureResult(bool isSuccess, ProcedureExitType reason, Exception exception = null)
        {
            IsSuccess = isSuccess;
            Reason = reason;
            Exception = exception;
        }
    }
}
