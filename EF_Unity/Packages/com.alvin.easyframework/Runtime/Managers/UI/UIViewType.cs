/*
 * ================================================
 * Describe:      用来区分UI面板展示类型，方便管理层级
 * Author:        Alvin5100
 * CreationTime:  2026-04-03 18:36:45
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-03 18:36:45
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.UI
{
    /// <summary>
    /// UI视窗类型
    /// </summary>
    public enum UIViewType
    {
        /// <summary>
        /// 缓存
        /// </summary>
        Cache,

        /// <summary>
        /// 底层常驻
        /// </summary>
        BottomPermanent,

        /// <summary>
        /// 正常显示
        /// </summary>
        Page,

        /// <summary>
        /// 弹窗
        /// </summary>
        Popup,

        /// <summary>
        /// 提示窗
        /// </summary>
        Tips,

        /// <summary>
        /// 顶层常驻
        /// </summary>
        TopPermanent,
    }
}