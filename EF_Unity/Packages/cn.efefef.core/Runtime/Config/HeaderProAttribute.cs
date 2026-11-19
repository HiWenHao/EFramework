/*
 * ================================================
 * Describe:      用来拓展HeaderAttribute，能随意切换中英文，后面还可以拓展
 * Author:        Alvin8412
 * CreationTime:  2026-04-26 00:11:48
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-19 11:33:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using UnityEngine;

namespace EasyFramework
{
    /// <summary>
    /// HeaderPro 的显示类型：控制加粗、颜色与是否带分隔线，形成视觉层级
    /// <para>Display type of HeaderPro: controls bold, color and divider, forming a visual hierarchy.</para>
    /// </summary>
    public enum HeaderProType
    {
        /// <summary>
        /// 标题：加粗 + 暖黄
        /// <para>Title: bold + warm yellow</para>
        /// </summary>
        Title,

        /// <summary>
        /// 副标题：不加粗 + 暖黄 + 无分隔线
        /// <para>Subtitle: normal + warm yellow, no divider</para>
        /// </summary>
        Subtitle,

        /// <summary>加粗注释
        /// <para>Bold note</para>
        /// </summary>
        BoldNote,

        /// <summary>
        /// 普通文本
        /// <para>Note: plain text</para>
        /// </summary>
        Note,

        /// <summary>
        /// 分隔线：不显示文字，只画一条随 Inspector 面板宽度铺满的横线
        /// <para>Separator: no text, only a horizontal line spanning the panel width</para>
        /// </summary>
        Separator
    }

    /// <summary>
    ///	使用此“属性属性”来在“检查器”中的某些字段上方添加标题。
    /// <para>Use this PropertyAttribute to add a header above some fields in the Inspector.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public class HeaderProAttribute : PropertyAttribute
    {
        public readonly string Chinese;
        public readonly string English;

        /// <summary>显示类型（默认标题，兼容旧的两参写法）<para>Display type (defaults to Title, keeps old two-arg calls working)</para></summary>
        public readonly HeaderProType Type;

        public HeaderProAttribute(string chinese, string english, HeaderProType type = HeaderProType.Note)
        {
            Chinese = chinese;
            English = english;
            Type = type;
        }
    }
}