/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-08 18:06:08
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-08 18:06:08
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Managers.Procedure
{
    /// <summary>
    /// 流程状态
    /// </summary>
    public enum ProcedureState
    {
        /// <summary>
        /// 无状态
        /// </summary>
        None,
        
        /// <summary>
        /// 进入中
        /// </summary>
        Entering,
        
        /// <summary>
        /// 活跃
        /// </summary>
        Active,
        
        /// <summary>
        /// 挂起
        /// </summary>
        Suspended,
        
        /// <summary>
        /// 退出中
        /// </summary>
        Exiting,
        
        /// <summary>
        /// 退出后
        /// </summary>
        Exited,
        
        /// <summary>
        /// 超时
        /// </summary>
        Timeout
    }
}