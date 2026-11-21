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

namespace EasyFramework.Procedure
{
    /// <summary>
    /// 流程状态
    /// <para>Procedure state</para>
    /// </summary>
    public enum ProcedureState
    {
        /// <summary>
        /// 无状态
        /// <para>No state</para>
        /// </summary>
        None,
        
        /// <summary>
        /// 进入中
        /// <para>Entering</para>
        /// </summary>
        Entering,
        
        /// <summary>
        /// 活跃
        /// <para>Active</para>
        /// </summary>
        Active,
        
        /// <summary>
        /// 挂起
        /// <para>Suspended</para>
        /// </summary>
        Suspended,
        
        /// <summary>
        /// 退出中
        /// <para>Exiting</para>
        /// </summary>
        Exiting,
    }
}
