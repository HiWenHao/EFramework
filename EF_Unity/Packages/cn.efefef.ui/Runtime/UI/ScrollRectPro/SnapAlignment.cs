/*
 * ================================================
 * Describe:        EF框架的UI通用吸附枚举
 * Author:          Alvin8412
 * CreationTime:    2026-06-05 21:31:38
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-05 21:31:38
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// 吸附对齐
    /// <para>Snap alignment</para>
    /// </summary>
    public enum SnapAlignment
    {
        /// <summary>
        /// 不吸附
        ///<para>No adsorption</para>
        /// </summary>
        None,

        /// <summary>
        /// 吸附到视口顶部
        ///<para>Snap to top of viewport</para>
        /// </summary>
        Top,

        /// <summary>
        /// 吸附到视口中央
        /// <para>Snap to center of viewport</para></summary>
        Center,

        /// <summary>
        /// 吸附到视口底部
        /// <para>Snap to bottom of viewport</para>
        /// </summary>
        Bottom
    }
}