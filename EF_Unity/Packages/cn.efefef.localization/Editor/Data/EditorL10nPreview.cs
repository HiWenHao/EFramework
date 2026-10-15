/*
 * ================================================
 * Describe:        编辑器下按 key 预览译文的数据桥接（与运行时共用 L10nTableStore）
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 17:35:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-15 18:30:00
 * ScriptVersion:   0.2
 * ================================================
 */

using System.Collections.Generic;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// 编辑器下预览译文的数据桥接
    /// <para>直接复用运行时数据通道 <see cref="L10nTableStore"/>（读 <c>Resources/L10n</c>
    /// 的导表产物），保证「编辑器里看到的」等于「游戏里显示的」——同一份文件、同一个类，
    /// 不存在第二套读取逻辑。</para>
    ///
    /// <para><b>预览语言与运行时语言刻意独立：</b>编辑器下 Runtime 服务尚未初始化，
    /// 且预览语言是「美术要看哪一版」的主观选择，不该被运行时的语言切换影响。</para>
    ///
    /// <para>Editor-side preview bridge sharing the exact same data channel
    /// (<see cref="L10nTableStore"/>) as the running game, so previews are WYSIWYG.</para>
    /// </summary>
    public static class EditorL10nPreview
    {
        /// <summary>
        /// 未发现语言表时的兜底语言集合
        /// <para>与清单默认语言一致，保证全新工程（还没导过表）下拉框也有可选项。</para>
        /// </summary>
        private static readonly string[] FallbackLanguages = { LanguageCode.Zn, LanguageCode.En };

        /// <summary>
        /// 编辑器预览当前使用的语言
        /// <para>默认 zn：主要使用场景为中文工程，美术最先核对中文文案。</para>
        /// </summary>
        public static string PreviewLanguage = LanguageCode.Zn;

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

            var value = L10nTableStore.TryGet(key, PreviewLanguage);

            // 预览语言没有该词时回落到兜底语言，与运行时行为保持一致
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
