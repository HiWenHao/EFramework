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

namespace EasyFramework.Managers.Procedure
{
    /// <summary>
    /// 流程执行结果（用于 StartSubProcedureAndWait 返回）
    /// </summary>
    public readonly struct ProcedureResult
    {
        /// <summary>
        /// 是否正常完成（未异常、未超时、未取消）
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// 退出原因
        /// </summary>
        public ProcedureExitType Reason { get; }

        /// <summary>
        /// 如果因异常退出，携带异常对象（可能为 null）
        /// </summary>
        public Exception Exception { get; }

        public ProcedureResult(bool isSuccess, ProcedureExitType reason, Exception exception = null)
        {
            IsSuccess = isSuccess;
            Reason = reason;
            Exception = exception;
        }
    }
}
