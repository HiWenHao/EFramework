/*
 * ================================================
 * Describe:        UI视窗开关时的动画类型
 * Author:          Alvin8412
 * CreationTime:    2026-06-22 17:41:03
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-22 17:41:03
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 动画类型
    /// <para>Animation type</para>
    /// </summary>
    public enum UiViewAnimationType
    {
        /// <summary>
        /// 无动画
        /// <para>No animation</para>
        /// </summary>
        None,

        /// <summary>
        /// 从左滑入
        /// <para>Slide from left</para></summary>
        SlideFromLeft,

        /// <summary>
        /// 从右滑入
        /// <para>Slide from right</para></summary>
        SlideFromRight,

        /// <summary>
        /// 从上滑入
        /// <para>Slide from top</para></summary>
        SlideFromTop,

        /// <summary>
        /// 从下滑入
        /// <para>Slide from bottom</para></summary>
        SlideFromBottom,

        /// <summary>
        /// 缩放
        /// <para>Do scale</para></summary>
        Scale,
    }
}