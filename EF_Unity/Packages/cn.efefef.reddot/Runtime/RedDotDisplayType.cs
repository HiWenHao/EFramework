/*
 * ================================================
 * Describe:      用来定义红点展示类型. Used to define red dot display type
 * Author:        Alvin5100
 * CreationTime:  2026-05-12 17:51:21
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-13 15:09:01
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Managers.RedDot
{
    /// <summary>
    /// 红点展示类型
    /// <para>Red dot display type</para>
    /// </summary>
    public enum RedDotDisplayType
    {
        /// <summary>
        ///	仅显示红点（无数字）
        /// <para>Show only the red dots (without numbers)</para>
        /// </summary>
        Dot,

        /// <summary>
        /// 显示数字（如“3”）
        /// <para>Display numbers (such as "8")</para>
        /// </summary>
        Number,

        /// <summary>
        /// 显示自定义图片
        /// <para>Display custom image</para>
        /// </summary>
        Image,

        /// <summary>
        /// 图片 + 数字
        /// <para>Picture + Number</para>
        /// </summary>
        ImageNumber
    }
}