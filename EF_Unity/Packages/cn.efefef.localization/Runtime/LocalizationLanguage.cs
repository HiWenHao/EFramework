/*
 * ================================================
 * Describe:        运行时语言代码常量与切换事件
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 16:40:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-14 16:40:00
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Localization
{
    /// <summary>
    /// 语言代码常量
    /// <para>与 Luban 生成的表名一一对应（TbText_{语言}），新增语言时两边都要加。
    /// 这里刻意用 const string 而非 enum：包经 UPM 分发后目录只读，
    /// 且语言列表由配置驱动，字符串更灵活。</para>
    /// <para>Language codes matching the generated Luban tables (TbText_{lang}).</para>
    public static class LanguageCode
    {
        /// <summary>英语（兜底语言） </summary>
        public const string En = "en";

        /// <summary>中文（zn 为工程内既定的语言代码，非 ISO 标准的 zh） </summary>
        public const string Zn = "zn";
    }

    /// <summary>
    /// 语言切换事件
    /// <para>由 I18NManager 在语言真正切换成功后发布，界面层据此刷新所有 LocalizedText。
    /// 只在「实际语言发生变化」时发布，避免重复设置同一语言引发无谓刷新。</para>
    /// <para>Published by I18NManager after a successful language switch.</para>
    /// </summary>
    public readonly struct LanguageChangedEvent
    {
        /// <summary>切换后的语言代码 </summary>
        public string Language { get; }

        /// <summary>
        /// 构造语言切换事件
        /// </summary>
        /// <param name="language">切换后的语言代码</param>
        public LanguageChangedEvent(string language)
        {
            Language = language;
        }
    }
}