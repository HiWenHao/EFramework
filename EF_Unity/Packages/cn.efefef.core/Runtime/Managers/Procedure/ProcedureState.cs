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
        None,
        Entering,
        Active,
        Suspended,
        Exiting,
        Exited,
        Faulted,
        Timeout
    }
}