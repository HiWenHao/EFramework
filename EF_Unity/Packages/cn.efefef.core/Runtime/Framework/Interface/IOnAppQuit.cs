/*
 * ================================================
 * Describe:        应用退出前通知接口
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
    /// 应用退出前通知：在管理器销毁流程开始前同步调用，是保存数据的最后一拍
    /// <para>Application quit notification: invoked synchronously before manager teardown begins, the last chance to persist data</para>
    /// </summary>
    public interface IOnAppQuit
    {
        #region Methods

        /// <summary>
        /// 应用退出前调用，回调必须同步完成不可 await
        /// <para>Called before application quit; the callback must finish synchronously without await</para>
        /// </summary>
        void OnAppQuit();

        #endregion
    }
}