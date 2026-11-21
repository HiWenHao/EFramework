/*
 * ================================================
 * Describe:      存档系统运行时日志出口：统一受 ArchiveConfig.enableLog 开关控制。
 * Author:        Alvin8412
 * CreationTime:  2026-11-21
 * ScriptVersion: 1.0
 * ===============================================
 */

using UnityEngine;

namespace EasyFramework.Archive
{
    /// <summary>
    /// 存档系统运行时日志出口。受 ArchiveConfig.enableLog 控制（错误始终输出）。
    /// <para>Runtime archive logging, controlled by ArchiveConfig.enableLog (errors always output).</para>
    /// </summary>
    internal static class ArchiveLog
    {
        // config 为 null（管理器尚未初始化）时兜底：允许输出
        private static bool Enabled(ArchiveConfig config) => config == null || config.enableLog;

        /// <summary>信息日志（受 enableLog 控制）</summary>
        internal static void Log(string msg, ArchiveConfig config = null)
        {
            if (!Enabled(config)) return;
            try { D.Log(msg); } catch { Debug.Log(msg); }
        }

        /// <summary>警告日志（受 enableLog 控制）</summary>
        internal static void Warning(string msg, ArchiveConfig config = null)
        {
            if (!Enabled(config)) return;
            try { D.Warning(msg); } catch { Debug.LogWarning(msg); }
        }

        /// <summary>错误日志（始终输出）</summary>
        internal static void Error(string msg, ArchiveConfig config = null)
        {
            try { D.Error(msg); } catch { Debug.LogError(msg); }
        }
    }
}