/*
 * ================================================
 * Describe:        ISnapStrategy 的内置实现（顶部/居中/底部）与工厂
 * Author:          Alvin8412
 * CreationTime:    2026-11-26 15:45:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-26 15:45:00
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Ui
{
    /// <summary>顶部对齐策略：item 起点对齐视窗起点。<para>Start alignment: item start meets viewport start.</para></summary>
    public readonly struct TopSnapStrategy : ISnapStrategy
    {
        /// <inheritdoc/>
        public float GetItemAnchor(float itemStart, float itemSize) => itemStart;

        /// <inheritdoc/>
        public float GetViewAnchor(float scrollOffset, float viewportSize) => scrollOffset;
    }

    /// <summary>居中对齐策略：item 中心对齐视窗中心。<para>Center alignment: item center meets viewport center.</para></summary>
    public readonly struct CenterSnapStrategy : ISnapStrategy
    {
        /// <inheritdoc/>
        public float GetItemAnchor(float itemStart, float itemSize) => itemStart + itemSize * 0.5f;

        /// <inheritdoc/>
        public float GetViewAnchor(float scrollOffset, float viewportSize) => scrollOffset + viewportSize * 0.5f;
    }

    /// <summary>底部对齐策略：item 末尾对齐视窗末尾。<para>End alignment: item end meets viewport end.</para></summary>
    public readonly struct BottomSnapStrategy : ISnapStrategy
    {
        /// <inheritdoc/>
        public float GetItemAnchor(float itemStart, float itemSize) => itemStart + itemSize;

        /// <inheritdoc/>
        public float GetViewAnchor(float scrollOffset, float viewportSize) => scrollOffset + viewportSize;
    }

    /// <summary>
    /// 吸附策略工厂：把 <see cref="SnapAlignment"/> 枚举映射到策略实例。
    /// <para>Factory mapping SnapAlignment to strategy instances.</para>
    /// </summary>
    public static class SnapStrategyFactory
    {
        /// <summary>
        /// 按对齐枚举取策略；None 返回 null。
        /// <para>Gets a strategy by alignment; returns null for None.</para>
        /// </summary>
        /// <param name="alignment">对齐枚举<para>Alignment enum</para></param>
        /// <returns>策略实例或 null<para>Strategy instance or null</para></returns>
        public static ISnapStrategy Create(SnapAlignment alignment)
        {
            return alignment switch
            {
                SnapAlignment.Top => new TopSnapStrategy(),
                SnapAlignment.Center => new CenterSnapStrategy(),
                SnapAlignment.Bottom => new BottomSnapStrategy(),
                _ => null
            };
        }
    }
}