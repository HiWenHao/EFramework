/*
 * ================================================
 * Describe:        框架延迟更新接口
 * Author:          Alvin5100
 * CreationTime:    2026-05-15 16:48:42
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-03 11:40:00
 * ScriptVersion:   0.1
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
        #region Properties

        /// <summary>
        /// 暂停, 自身不再轮询迭代
        /// <para>Is paused, and it is not being updated itself.</para>
        /// </summary>
        bool IsPaused { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 延迟更新,每帧中最后更新的内容
        /// <para>Delayed update, the last updated content in each frame</para>
        /// </summary>
        /// <param name="elapse">逻辑流逝时间，以秒为单位<para>The interval in seconds from the last frame to the current one</para></param>
        /// <param name="realElapse">真实流逝时间，以秒为单位<para>The timeScale-independent interval in seconds from the last frame to the current one</para></param>
        void LateUpdate(float elapse, float realElapse);

        #endregion
    }
}