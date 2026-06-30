/*
 * ================================================
 * Describe:      UI参数传递方向，控制参数是发给Enable还是Disable
 * Author:        Alvin8412
 * CreationTime:  2026-06-30
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-06-30
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// UI参数传递方向
    /// <para>UI argument passing direction</para>
    /// </summary>
    public enum UiArgsDirection
    {
        /// <summary>
        /// 仅传递给即将被打开页面的OnEnable函数
        /// <para>Only passed to the OnEnable function of the page that is about to be opened</para>
        /// </summary>
        ToEnable,

        /// <summary>
        /// 仅传递给即将被关闭页面的OnDisable函数
        /// <para>Only passed to the OnDisable function of the page that is about to be closed.</para>
        /// </summary>
        ToDisable,

        /// <summary>
        /// 同时传递给 OnEnable 和 OnDisable
        /// <para>Pass to both OnEnable and OnDisable</para>
        /// </summary>
        Both,
    }
}