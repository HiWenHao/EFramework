/*
 * ================================================
 * Describe:      GM 命令元数据与风险枚举。
 * Author:        Alvin8412
 * CreationTime:  2026-11-24
 * ModifyTime:    2026-11-24
 * Version:       0.2
 * ===============================================
 */

using System;
using System.Collections.Generic;

namespace EasyFramework.GameMaster
{
    /// <summary>
    /// GM 命令风险等级；Dangerous 及以上在面板中执行前需二次确认。
    /// <para>Risk level of a GM command; Dangerous and above require confirmation in the panel.</para>
    /// </summary>
    public enum GmRisk
    {
        /// <summary> 只读，无副作用。<para>Read-only, no side effect.</para> </summary>
        ReadOnly,

        /// <summary> 常规，副作用可恢复。<para>Normal, side effect recoverable.</para> </summary>
        Normal,

        /// <summary> 危险，副作用显著，需确认。<para>Dangerous, significant side effect, needs confirmation.</para> </summary>
        Dangerous,

        /// <summary> 破坏性，可能导致数据丢失。<para>Destructive, may cause data loss.</para> </summary>
        Destructive,
    }

    /// <summary>
    /// GM 命令元数据（只读展示信息，不含委托，供 Editor 与运行时共用）。
    /// <para>Read-only display metadata of a GM command, shared by Editor and runtime, without the delegate.</para>
    /// </summary>
    public sealed class GmCommandInfo
    {
        /// <summary>
        /// 默认面板分区名。
        /// <para>Default panel section name.</para>
        /// </summary>
        public const string DefaultSection = "Common";

        /// <summary> 稳定命令 ID。<para>Stable command identifier.</para> </summary>
        public string Id { get; }

        /// <summary> 显示名：注册时传入的原文，直接展示不做多语言解析。<para>Display name: the literal text passed at registration, shown as-is without localization.</para> </summary>
        public string DisplayKey { get; }

        /// <summary> 面板分区名，用于编辑器折叠；未指定时为 <see cref="DefaultSection"/>。
        /// <para>Panel section name used for folding in the Editor; defaults to <see cref="DefaultSection"/>.</para>
        /// </summary>
        public string Section { get; }

        /// <summary> 风险等级。<para>Risk level.</para> </summary>
        public GmRisk Risk { get; }

        // 参数键列表（只读副本）
        private readonly string[] _paramKeys;

        /// <summary>
        /// 创建命令元数据。
        /// <para>Creates command metadata.</para>
        /// </summary>
        /// <param name="id">稳定 ID。<para>Stable ID.</para></param>
        /// <param name="displayKey">显示名原文。<para>Literal display name.</para></param>
        /// <param name="risk">风险等级。<para>Risk level.</para></param>
        /// <param name="paramKeys">参数键。<para>Argument keys.</para></param>
        /// <param name="section">面板分区名，null 视为 Common。<para>Panel section name; null means Common.</para></param>
        public GmCommandInfo(string id, string displayKey, GmRisk risk,
            IReadOnlyList<string> paramKeys, string section)
        {
            Id = id;
            DisplayKey = displayKey;
            Section = string.IsNullOrEmpty(section) ? DefaultSection : section;
            Risk = risk;
            _paramKeys = paramKeys == null ? Array.Empty<string>() : CopyKeys(paramKeys);
        }

        // 复制参数键，避免外部集合可变导致元数据漂移
        private static string[] CopyKeys(IReadOnlyList<string> source)
        {
            var copy = new string[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            return copy;
        }

        /// <summary>
        /// 获取参数键的只读快照。
        /// <para>Gets a read-only snapshot of argument keys.</para>
        /// </summary>
        /// <returns>参数键数组。<para>Argument key array.</para></returns>
        public string[] GetParamKeys()
        {
            var copy = new string[_paramKeys.Length];
            for (int i = 0; i < _paramKeys.Length; i++)
                copy[i] = _paramKeys[i];
            return copy;
        }
    }
}