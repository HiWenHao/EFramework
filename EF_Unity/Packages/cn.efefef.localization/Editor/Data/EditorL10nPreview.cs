/*
 * ================================================
 * Describe:        编辑器下按 key 预览译文的数据桥接（与运行时共用 L10nTableStore）
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 17:35:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 16:05:00
 * ScriptVersion:   0.5
 * ================================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// 编辑器下预览译文的数据桥接（Resources 通道）
    /// <para>编辑态复用 <see cref="L10nTableStore"/>（读 <c>Resources/L10n</c> 的导表产物）；
    /// 运行态直接透传 <see cref="I18NManager"/>（其语言表经 AssetsManager 按需加载），
    /// 保证「编辑器/预览里看到的」等于「游戏里显示的」。</para>
    /// <para>Editor-side preview bridge: Resources channel in edit mode, runtime
    /// service pass-through in play mode.</para>
    /// </summary>
    public static class EditorL10nPreview
    {
        /// <summary>
        /// 未发现语言表时的兜底语言集合
        /// <para>与清单默认语言一致，保证全新工程（还没导过表）下拉框也有可选项。</para>
        /// </summary>
        private static readonly string[] FallbackLanguages = { LanguageCode.Zh, LanguageCode.En };

        /// <summary>
        /// 编辑器预览当前使用的语言（<strong>仅编辑态</strong>）
        /// <para>经 EditorPrefs 持久化（见 <see cref="LocalizationData.PreviewLanguage"/>）：
        /// 静态字段会在每次代码编译（域重载）后清零，导致预览语言总被重置回 zn；
        /// 改为持久化后保持使用者上一次的选择。默认 zn：主要使用场景为中文工程，
        /// 美术最先核对中文文案。</para>
        /// <para><b>仅编辑态生效</b>：Play 模式下预览跟随运行时语言
        /// （见 <see cref="FollowsRuntimeLanguage"/>），本值不参与取词。</para>
        /// </summary>
        public static string PreviewLanguage
        {
            get => LocalizationData.PreviewLanguage;
            set => LocalizationData.PreviewLanguage = value;
        }

        /// <summary>
        /// 预览是否正跟随运行时语言
        /// <para>Play 模式且 <see cref="I18NManager"/> 已确定当前语言时为 true：
        /// 组件此刻显示的就是运行时译文，预览必须与之同源，否则两处显示对不上。
        /// 编辑态恒为 false——预览语言是「美术要看哪一版」的主观选择，独立于运行时。
        /// 短路顺序保证编辑态绝不触碰 <see cref="I18NManager"/>（避免其单例在编辑态被创建）。</para>
        /// </summary>
        public static bool FollowsRuntimeLanguage
            => Application.isPlaying
               && I18NManager.Instance != null
               && !string.IsNullOrEmpty(I18NManager.Instance.CurrentLanguage);

        /// <summary>
        /// 切换运行时语言（Play 模式下由预览下拉调用）
        /// <para>经 <see cref="I18NManager.SwitchLanguage"/> 异步装载新语言表，
        /// 完成后广播事件，全场景 LocalizedText 即时刷新；非 Play 模式调用无效果。
        /// 装载为异步过程，此处不等待（完成后自动广播刷新）。</para>
        /// </summary>
        public static void TrySwitchRuntimeLanguage(string language)
        {
            if (Application.isPlaying && I18NManager.Instance != null)
                _ = I18NManager.Instance.SwitchLanguage(language);
        }

        /// <summary>
        /// 预览数据是否可用（Resources/L10n 下存在语言表）
        /// <para>供 Inspector 区分「没有语言表」与「表里缺这个词」两种情况——
        /// 前者是导表流程没走完，后者是翻译缺失，提示文案完全不同。</para>
        /// </summary>
        public static bool IsDataAvailable => L10nTableStore.HasTables;

        /// <summary>
        /// 可选的预览语言集合（来自语言表文件名自动发现）
        /// <para>没有语言表时回落到兜底集合，保证下拉框永远有项可选。</para>
        /// </summary>
        public static IReadOnlyList<string> AvailableLanguages
            => L10nTableStore.HasTables ? L10nTableStore.Languages : FallbackLanguages;

        /// <summary>
        /// 取译文预览
        /// </summary>
        /// <param name="key">本地化 key</param>
        /// <returns>
        /// 译文；未命中或语言表不可用时返回 <c>null</c>（由调用方决定显示占位还是提示）
        /// </returns>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            // 运行态：直接问运行时服务（其语言表经 AssetsManager 按需加载，与场景文本同源）；
            // [Missing:xxx] 占位在预览中等同于缺词，交由调用方显示缺词提示
            if (Application.isPlaying && I18NManager.Instance != null)
            {
                var runtime = I18NManager.Instance.Get(key);
                return runtime != null && !runtime.StartsWith(I18NManager.MissingPrefix, StringComparison.Ordinal)
                    ? runtime
                    : null;
            }

            // 编辑态：Resources 通道（预览语言），当前语言缺词回落 en
            var value = L10nTableStore.TryGet(key, PreviewLanguage);

            if (string.IsNullOrEmpty(value) && PreviewLanguage != LanguageCode.En)
                value = L10nTableStore.TryGet(key, LanguageCode.En);

            return string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// 使数据缓存失效，下次取值重新发现语言并重新读表
        /// <para>导表产物同步进 Resources 后由 L10nTablePublisher 自动调用；
        /// 手工改动语言表文件后也可手动触发。</para>
        /// </summary>
        public static void Invalidate() => L10nTableStore.Invalidate();
    }
}
