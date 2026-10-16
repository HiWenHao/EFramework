/*
 * ================================================
 * Describe:        编辑器下按 key 预览译文的数据桥接（与运行时共用 L10nTableStore）
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 17:35:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 18:45:00
 * ScriptVersion:   0.5
 * ================================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// 编辑器下预览译文的数据桥接（编辑态走 Resources 通道，Play 模式透传运行时服务）
    /// <para>Editor-side preview bridge: Resources channel in edit mode,
    /// runtime service pass-through in play mode.</para>
    /// </summary>
    public static class EditorL10nPreview
    {
        #region Fields

        // 未发现语言表时的兜底语言集合（与清单默认语言一致，保证全新工程下拉框也有可选项）
        private static readonly string[] FallbackLanguages = { LanguageCode.Zh, LanguageCode.En };

        #endregion

        #region Properties

        /// <summary>
        /// 编辑器预览当前使用的语言（仅编辑态；经 EditorPrefs 持久化，跨编译域重载保持上次选择）
        /// <para>Editor preview language; edit mode only, persisted via EditorPrefs.</para>
        /// <para>Play 模式下预览跟随运行时语言（见 <see cref="FollowsRuntimeLanguage"/>），本值不参与取词。</para>
        /// </summary>
        public static string PreviewLanguage
        {
            get => LocalizationData.PreviewLanguage;
            set => LocalizationData.PreviewLanguage = value;
        }

        /// <summary>
        /// 预览是否正跟随运行时语言
        /// <para>Whether the preview follows the runtime language.</para>
        /// <para>Play 模式且 I18NManager 已确定当前语言时为 true；编辑态恒为 false。
        /// 短路顺序保证编辑态绝不触碰 I18NManager（避免其单例在编辑态被创建）。</para>
        /// </summary>
        public static bool FollowsRuntimeLanguage
            => Application.isPlaying
               && I18NManager.Instance != null
               && !string.IsNullOrEmpty(I18NManager.Instance.CurrentLanguage);

        /// <summary>
        /// 预览数据是否可用（Resources/L10n 下存在语言表；供 Inspector 区分「没有语言表」与「表里缺词」）
        /// <para>Whether preview data is available (language tables exist under Resources/L10n).</para>
        /// </summary>
        public static bool IsDataAvailable => L10nTableStore.HasTables;

        /// <summary>
        /// 可选的预览语言集合（来自语言表文件名自动发现；没有语言表时回落兜底集合）
        /// <para>Available preview languages discovered from table file names.</para>
        /// </summary>
        public static IReadOnlyList<string> AvailableLanguages
            => L10nTableStore.HasTables ? L10nTableStore.Languages : FallbackLanguages;

        #endregion

        #region Methods

        /// <summary>
        /// 切换运行时语言（Play 模式下由预览下拉调用；装载为异步过程，此处不等待，完成后自动广播刷新）
        /// <para>Switches the runtime language from the preview dropdown (play mode only).</para>
        /// </summary>
        public static void TrySwitchRuntimeLanguage(string language)
        {
            if (Application.isPlaying && I18NManager.Instance != null)
                _ = I18NManager.Instance.SwitchLanguage(language);
        }

        /// <summary>
        /// 取译文预览
        /// <para>Looks up the preview translation.</para>
        /// </summary>
        /// <param name="key">本地化 key<para>Localization key.</para></param>
        /// <returns>译文；未命中或语言表不可用时返回 null（由调用方决定显示占位还是提示）<para>The translation, or null on a miss.</para></returns>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            // 运行态：直接问运行时服务（与场景文本同源）；[Missing:xxx] 占位在预览中等同于缺词，返回 null 交调用方提示
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
        /// 使数据缓存失效，下次取值重新发现语言并重新读表（导表同步后调用，使预览立即反映最新译文）
        /// <para>Invalidates the data cache so the next access re-scans and re-reads.</para>
        /// </summary>
        public static void Invalidate() => L10nTableStore.Invalidate();

        #endregion
    }
}