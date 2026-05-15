/*
 * ================================================
 * Describe:      框架延迟更新接口
 * Author:        Alvin5100
 * CreationTime:  2026-05-15 16:48:42
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-15 16:48:42
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework
{
    /// <summary>
    /// 延迟更新,每帧中最后更新的内容
    /// <para>Delayed update, the last updated content in each frame</para>
    /// </summary>
    public interface ILateUpdate
    {
        /// <summary>
        /// 延迟更新,每帧中最后更新的内容
        /// <para>Delayed update, the last updated content in each frame</para>
        /// </summary>
        void LateUpdate();
    }
}