/*
 * ================================================
 * Describe:        多语言 Key 类型（静态 / 动态）
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 13:48:07
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 18:45:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// Key 类型：标记该文案是否需要运行时拼接参数
    /// <para>Key kind: marks whether the text needs runtime parameter formatting.</para>
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LocalizationKeyKind
    {
        #region Fields

        /// <summary>
        /// 静态文案：整条翻译，无参数
        /// <para>Static text: whole-string translation, no parameters.</para>
        /// </summary>
        [EnumMember(Value = "Static")] Static = 0,

        /// <summary>
        /// 动态文案：运行时拼接参数，需考虑复数规则（德语/俄语必查）
        /// <para>Dynamic text: formatted at runtime, plural rules required.</para>
        /// </summary>
        [EnumMember(Value = "Dynamic")] Dynamic = 1,

        #endregion
    }
}