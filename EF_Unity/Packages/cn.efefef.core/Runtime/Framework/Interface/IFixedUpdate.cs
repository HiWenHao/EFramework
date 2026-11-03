/*
 * ================================================
 * Describe:        框架物理更新接口
 * Author:          Alvin8412
 * CreationTime:    2026-05-15 21:16:57
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 11:40:00
 * ScriptVersion:   0.1
 * ===============================================
 */

namespace EasyFramework
{
    /// <summary>
    /// 物理更新接口
    /// <para>Physics update interface</para>
    /// </summary>
    public interface IFixedUpdate
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
        /// 轮询更新
        /// <para>Polls the fixed update</para>
        /// </summary>
        /// <param name="fixedDeltaTime">物理帧间隔，以秒为单位<para>The fixed interval in seconds from the last physics frame to the current one</para></param>
        void FixedUpdate(float fixedDeltaTime);

        #endregion
    }
}