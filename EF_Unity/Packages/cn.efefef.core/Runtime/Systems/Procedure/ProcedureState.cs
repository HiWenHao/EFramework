/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin5100
 * CreationTime:    2026-05-08 18:06:08
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-09 23:07:04
 * ScriptVersion:   0.1
 * ===============================================
 */

namespace EasyFramework.Systems.Procedure
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
    }
}
