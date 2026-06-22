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

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// UI视窗类型
    /// </summary>
    public enum UIViewType
    {
        /// <summary>
        /// 缓存 - 自动销毁倒计时结束后销毁
        /// <para>Cache - Destroyed after the automatic destruction countdown ends</para>
        /// </summary>
        Cache,

        /// <summary>
        /// 底层常驻 - 同类型视窗只存在一个
        /// <para>Only one of the same type of ui view.</para>
        /// </summary>
        BottomPermanent,

        /// <summary>
        /// 正常显示 - 同类型单一显示
        /// <para>Only one of the same type view will be displayed.</para>
        /// </summary>
        Page,

        /// <summary>
        /// 顶层常驻 - 同类型视窗只存在一个
        /// <para>Only one of the same type of ui view.</para>
        /// </summary>
        TopPermanent,

        /// <summary>
        /// 提示窗 - 同类型叠加显示
        /// <para>Same-type view overlay display</para>
        /// </summary>
        Tips,

        /// <summary>
        /// 弹窗 - 同类型叠加显示
        /// <para>Same-type view overlay display</para>
        /// </summary>
        Popup,
    }
}