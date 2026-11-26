/*
 * ================================================
 * Describe:        无缝滚动列表的绘制起点方向枚举
 * Author:          Alvin8412
 * CreationTime:    2026-11-25 18:20:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-25 18:20:00
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Ui
{
    /// <summary>
    /// 无缝滚动列表的绘制起点方向。一个值即一个用户可选项，无非法组合。
    /// <para>Drawing origin direction of the seamless scroll list. One value maps to exactly one
    /// user option, with no illegal combinations.</para>
    /// <br/> 与既有 <see cref="Direction"/> 无损映射：Vertical→TopToBottom、Horizontal→LeftToRight。
    /// <para>Lossless mapping to <see cref="Direction"/>: Vertical→TopToBottom, Horizontal→LeftToRight.</para>
    /// </summary>
    public enum ScrollFlow
    {
        /// <summary>
        /// 从上往下绘制，索引 0 在视口顶部
        /// <para>Draw from top to bottom; index 0 sits at the viewport top edge.</para>
        /// </summary>
        TopToBottom,

        /// <summary>
        /// 从下往上绘制，索引 0 在视口底部
        /// <para>Draw from bottom to top; index 0 sits at the viewport bottom edge.</para>
        /// </summary>
        BottomToTop,

        /// <summary>
        /// 从左往右绘制，索引 0 在视口左边
        /// <para>Draw from left to right; index 0 sits at the viewport left edge.</para>
        /// </summary>
        LeftToRight,

        /// <summary>
        /// 从右往左绘制，索引 0 在视口右边
        /// <para>Draw from right to left; index 0 sits at the viewport right edge.</para>
        /// </summary>
        RightToLeft,
    }
}
