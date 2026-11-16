/*
 * ================================================
 * Describe:        ScriptableObject channel for launcher download progress.
 * Author:          Alvin8412
 * CreationTime:    2026-11-16 11:20:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-16 11:20:00
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 下载进度事件负载。
    /// <para>Payload for a download progress update.</para>
    /// </summary>
    public struct LauncherProgressPayload
    {
        /// <summary>已下载文件数<para>Number of downloaded files.</para></summary>
        public long CurrentCount { get; }

        /// <summary>总文件数<para>Total file count.</para></summary>
        public long TotalCount { get; }

        /// <summary>已下载字节数<para>Downloaded bytes.</para></summary>
        public long CurrentBytes { get; }

        /// <summary>总字节数<para>Total bytes.</para></summary>
        public long TotalBytes { get; }

        /// <summary>构造一次进度更新<para>Create a progress payload.</para></summary>
        /// <param name="currentCount">已下载文件数<para>Number of downloaded files.</para></param>
        /// <param name="totalCount">总文件数<para>Total file count.</para></param>
        /// <param name="currentBytes">已下载字节数<para>Downloaded bytes.</para></param>
        /// <param name="totalBytes">总字节数<para>Total bytes.</para></param>
        public LauncherProgressPayload(long currentCount, long totalCount, long currentBytes, long totalBytes)
        {
            CurrentCount = currentCount;
            TotalCount = totalCount;
            CurrentBytes = currentBytes;
            TotalBytes = totalBytes;
        }
    }

    /// <summary>
    /// 下载进度事件通道：进度条、埋点等各自订阅，互不感知。
    /// <para>Progress channel. Progress bars and telemetry subscribe independently.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "LauncherProgressEvent", menuName = "EF/Launcher/Progress Event", order = 211)]
    public sealed class LauncherProgressEvent : LauncherEventChannel<LauncherProgressPayload>
    {
    }
}
