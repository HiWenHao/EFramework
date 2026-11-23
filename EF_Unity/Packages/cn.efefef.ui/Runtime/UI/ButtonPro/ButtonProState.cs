/*
 * ================================================
 * Describe:      Public selection state for ButtonPro state-changed event.
 * Author:        Alvin8412
 * CreationTime:  2026-11-23 15:18:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-23 15:18:00
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Ui
{
    /// <summary>
    /// ButtonPro 的可公开访问状态，映射自 Selectable 的受保护 SelectionState。
    /// <para>Publicly accessible state of ButtonPro, mapped from the protected SelectionState of Selectable.</para>
    /// </summary>
    public enum ButtonProState
    {
        /// <summary>
        /// 可交互但未选中
        /// <para>Interactable but not selected</para>
        /// </summary>
        Normal,

        /// <summary>
        /// 指针悬停
        /// <para>Pointer hovering</para>
        /// </summary>
        Highlighted,

        /// <summary>
        /// 正在按压
        /// <para>Being pressed</para>
        /// </summary>
        Pressed,

        /// <summary>
        /// 已选中
        /// <para>Selected</para>
        /// </summary>
        Selected,

        /// <summary>
        /// 不可交互
        /// <para>Not interactable</para>
        /// </summary>
        Disabled
    }
}