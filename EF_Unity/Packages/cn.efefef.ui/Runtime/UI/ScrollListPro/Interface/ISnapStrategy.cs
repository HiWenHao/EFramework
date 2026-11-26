/*
 * ================================================
 * Describe:        滚动吸附策略契约：定义 item 与视窗的对齐锚点计算
 * Author:          Alvin8412
 * CreationTime:    2026-11-26 15:45:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-26 15:45:00
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Ui
{
    /// <summary>
    /// 滚动吸附策略：把"对齐到 item 的哪个位置"与"对齐到视窗的哪个位置"的计算抽离，
    /// 新增吸附方式（如顶部带留白、最近 item 带偏移）无需改动列表主类。
    /// <para>Snap strategy: abstracts how an item edge and the viewport edge align, so new snap
    /// behaviors (e.g. top with padding) need no change to the list class.</para>
    /// </summary>
    public interface ISnapStrategy
    {
        /// <summary>
        /// 计算 item 的对齐锚点（虚拟坐标）。
        /// <para>Computes the item's alignment anchor in virtual coordinates.</para>
        /// </summary>
        /// <param name="itemStart">item 起点偏移<para>Item start offset</para></param>
        /// <param name="itemSize">item 尺寸<para>Item size</para></param>
        /// <returns>item 锚点<para>Item anchor</para></returns>
        float GetItemAnchor(float itemStart, float itemSize);

        /// <summary>
        /// 计算视窗的对齐锚点（虚拟坐标）。
        /// <para>Computes the viewport's alignment anchor in virtual coordinates.</para>
        /// </summary>
        /// <param name="scrollOffset">当前滚动偏移<para>Current scroll offset</para></param>
        /// <param name="viewportSize">视窗尺寸<para>Viewport size</para></param>
        /// <returns>视窗锚点<para>Viewport anchor</para></returns>
        float GetViewAnchor(float scrollOffset, float viewportSize);
    }
}