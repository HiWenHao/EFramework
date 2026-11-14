/*
 * ================================================
 * Describe:        This script is used to define the launcher view callbacks.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 15:58:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 15:58:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 启动界面契约。<b>实现方必须放在 AOT 程序集，且不能引用 EF.UI</b>（它在热更层，此时还不存在）
    /// <para>Launcher view contract. Implementations must live in an AOT assembly and must not use EF.UI.</para>
    /// </summary>
    public interface ILauncherView
    {
        /// <summary>
        /// 状态切换时回调
        /// <para>Called when the pipeline enters a new state.</para>
        /// </summary>
        /// <param name="state">当前状态</param>
        /// <param name="tip">给玩家看的文案</param>
        void OnStateChanged(LauncherState state, string tip);

        /// <summary>
        /// 下载进度回调（没有下载任务时 totalCount 为 0）
        /// <para>Download progress. totalCount is 0 when there is nothing to download.</para>
        /// </summary>
        void OnProgress(long currentCount, long totalCount, long currentBytes, long totalBytes);

        /// <summary>
        /// 启动失败回调，可调用 retry 重试
        /// <para>Boot failure. Invoke retry to run the pipeline again.</para>
        /// </summary>
        void OnError(LauncherErrorCode code, string message, Action retry);
    }
}