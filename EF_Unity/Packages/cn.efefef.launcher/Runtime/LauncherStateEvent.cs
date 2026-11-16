/*
 * ================================================
 * Describe:        ScriptableObject channel for launcher state changes.
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
    /// 启动状态事件负载。
    /// <para>Payload for a launcher state change.</para>
    /// </summary>
    public struct LauncherStatePayload
    {
        /// <summary>当前状态<para>Current state.</para></summary>
        public LauncherState State { get; }

        /// <summary>给玩家看的文案<para>Player-facing tip.</para></summary>
        public string Tip { get; }

        /// <summary>构造一次状态变更<para>Create a state change payload.</para></summary>
        /// <param name="state">当前状态<para>Current state.</para></param>
        /// <param name="tip">给玩家看的文案<para>Player-facing tip.</para></param>
        public LauncherStatePayload(LauncherState state, string tip)
        {
            State = state;
            Tip = tip;
        }
    }

    /// <summary>
    /// 启动状态事件通道：把状态变化广播给界面与埋点，Launcher 不需要持有它们的引用。
    /// <para>State change channel. Broadcasts to presenters and telemetry without the launcher holding their references.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "LauncherStateEvent", menuName = "EF/Launcher/State Event", order = 210)]
    public sealed class LauncherStateEvent : LauncherEventChannel<LauncherStatePayload>
    {
    }
}
