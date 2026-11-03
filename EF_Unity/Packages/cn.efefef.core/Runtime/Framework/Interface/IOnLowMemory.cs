/*
 * ================================================
 * Describe:        系统低内存告警通知接口
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
    /// 系统低内存告警通知：Android onTrimMemory / iOS didReceiveMemoryWarning 时触发
    /// <para>Low memory notification: raised on Android onTrimMemory and iOS didReceiveMemoryWarning</para>
    /// </summary>
    public interface IOnLowMemory
    {
        #region Methods

        /// <summary>
        /// 收到系统低内存告警时调用，订阅者应释放可再生的缓存资源
        /// <para>Called on system low memory warning; subscribers should release regenerable caches</para>
        /// </summary>
        void OnLowMemory();

        #endregion
    }
}