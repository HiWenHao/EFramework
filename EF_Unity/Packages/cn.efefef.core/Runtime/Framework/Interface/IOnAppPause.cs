/*
 * ================================================
 * Describe:        应用前后台切换通知接口
 * Author:          Alvin8412
 * CreationTime:    2026-11-03 11:32:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 11:32:00
 * ScriptVersion:   0.1
 * ===============================================
 */

namespace EasyFramework
{
    /// <summary>
    /// 应用前后台切换通知：移动端退后台与回前台时触发
    /// <para>Application pause notification: raised when the app enters or leaves the background on mobile</para>
    /// </summary>
    public interface IOnAppPause
    {
        #region Methods

        /// <summary>
        /// 应用暂停状态变化时调用，回调内不可依赖下一帧
        /// <para>Called when the application pause state changes; do not rely on the next frame inside the callback</para>
        /// </summary>
        /// <param name="paused">是否进入后台<para>Whether the app is entering the background</para></param>
        void OnAppPause(bool paused);

        #endregion
    }
}