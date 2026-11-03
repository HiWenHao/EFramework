/*
 * ================================================
 * Describe:        应用焦点变化通知接口
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
    /// 应用焦点变化通知：Android 前后台切换与桌面失焦/复焦时触发
    /// <para>Application focus notification: raised on Android background switches and desktop focus changes</para>
    /// </summary>
    public interface IOnAppFocus
    {
        #region Methods

        /// <summary>
        /// 应用焦点变化时调用
        /// <para>Called when the application focus changes</para>
        /// </summary>
        /// <param name="focused">是否获得焦点<para>Whether the app gained focus</para></param>
        void OnAppFocus(bool focused);

        #endregion
    }
}