/*
 * ================================================
 * Describe:        框架更新接口
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2023-03-02 16:24:48
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 11:40:00
 * ScriptVersion:   0.1
 * ===============================================
 */

namespace EasyFramework
{
    /// <summary>
    /// Update interface in project.
    /// <para>项目中的更新接口</para>
    /// </summary>
    public interface IUpdate
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
        /// <para>Polls the frame update</para>
        /// </summary>
        /// <param name="elapse">逻辑流逝时间，以秒为单位<para>The interval in seconds from the last frame to the current one</para></param>
        /// <param name="realElapse">真实流逝时间，以秒为单位<para>The timeScale-independent interval in seconds from the last frame to the current one</para></param>
        void Update(float elapse, float realElapse);

        #endregion
    }
}