/*
 * ================================================
 * Describe:        ScriptableObject channel for launcher failures.
 * Author:          Alvin8412
 * CreationTime:    2026-11-16 11:20:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-16 11:20:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using UnityEngine;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 启动失败事件负载。
    /// <para>Payload for a launcher failure.</para>
    /// </summary>
    public struct LauncherErrorPayload
    {
        /// <summary>失败原因<para>Failure reason.</para></summary>
        public LauncherErrorCode Code { get; }

        /// <summary>诊断信息<para>Diagnostic message.</para></summary>
        public string Message { get; }

        /// <summary>重试回调，可为空<para>Retry callback, may be null.</para></summary>
        public Action Retry { get; }

        /// <summary>构造一次失败通知<para>Create a failure payload.</para></summary>
        /// <param name="code">失败原因<para>Failure reason.</para></param>
        /// <param name="message">诊断信息<para>Diagnostic message.</para></param>
        /// <param name="retry">重试回调<para>Retry callback.</para></param>
        public LauncherErrorPayload(LauncherErrorCode code, string message, Action retry)
        {
            Code = code;
            Message = message;
            Retry = retry;
        }
    }

    /// <summary>
    /// 启动失败事件通道：错误面板、上报 SDK 各自订阅，Launcher 不需要持有它们的引用。
    /// <para>Failure channel. Error panels and crash reporters subscribe independently.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "LauncherErrorEvent", menuName = "EF/Launcher/Error Event", order = 212)]
    public sealed class LauncherErrorEvent : LauncherEventChannel<LauncherErrorPayload>
    {
    }
}
