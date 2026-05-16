/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-15 21:16:57
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-15 21:16:57
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework
{
    /// <summary>
    /// 物理更新接口
    /// </summary>
    public interface IFixedUpdate
    {
        /// <summary>
        /// 暂停, 自身不再轮询迭代
        /// <para>Is paused, and it is not being updated itself.</para>
        /// </summary>
        bool IsPaused { get; }

        /// <summary>
        /// 轮询更新
        /// </summary>
        /// <param name="fixedDeltaTime">The interval in seconds from the last frame to the current one.
        /// <para>逻辑流逝时间，以秒为单位</para>
        /// </param>
        void FixedUpdate(float fixedDeltaTime);
    }
}
