/*
 * ================================================
 * Describe:        Asset Checker 界面样式（与配置数据分离）
 * Author:          Alvin5100
 * CreationTime:    2026-10-31 23:05:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-31 23:05:00
 * ScriptVersion:   0.1
 * ===============================================
*/

using UnityEngine;

namespace EasyFramework.Edit.Windows.AssetChecker
{
    /// <summary>
    /// Asset Checker 的界面样式。
    /// <para>UI styles for Asset Checker.</para>
    /// <para>
    /// 与 <see cref="AssetsCheckerConfig"/>（纯配置数据）分离的原因：<c>GUI.skin</c> 属于 GUI 函数，
    /// 只能在 OnGUI 内访问；放在配置类的静态字段初始化器里会在「打开窗口但尚未进入 OnGUI」时抛
    /// <c>ArgumentException: You can only call GUI functions from inside OnGUI</c>。
    /// 因此这里改为按需（懒）创建，并在皮肤变化（明暗主题切换）时自动重建。
    /// </para>
    /// </summary>
    internal static class AssetCheckerStyles
    {
        #region Fields

        // 缓存的标签样式及其来源皮肤（用于皮肤变化后重建）
        private static GUIStyle _labelStyle;
        private static GUISkin _labelSkin;

        // 缓存的按钮样式及其来源皮肤（用于皮肤变化后重建）
        private static GUIStyle _buttonStyle;
        private static GUISkin _buttonSkin;

        #endregion

        #region Properties

        /// <summary>
        /// 居中白字标签样式（只能在 OnGUI 内访问）
        /// <para>Centered white-text label style (OnGUI only).</para>
        /// </summary>
        internal static GUIStyle Label
        {
            get
            {
                var skin = GUI.skin;
                if (_labelStyle == null || _labelSkin != skin)
                {
                    _labelSkin = skin;
                    _labelStyle = new GUIStyle(skin.label)
                    {
                        normal = new GUIStyleState
                        {
                            textColor = Color.white,
                        },
                        clipping = TextClipping.Clip,
                        padding = new RectOffset(0, 0, 4, 0),
                        alignment = TextAnchor.MiddleCenter,
                    };
                }

                return _labelStyle;
            }
        }

        /// <summary>
        /// 左对齐按钮样式（只能在 OnGUI 内访问）
        /// <para>Left-aligned button style (OnGUI only).</para>
        /// </summary>
        internal static GUIStyle Button
        {
            get
            {
                var skin = GUI.skin;
                if (_buttonStyle == null || _buttonSkin != skin)
                {
                    _buttonSkin = skin;
                    _buttonStyle = new GUIStyle(skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft
                    };
                }

                return _buttonStyle;
            }
        }

        #endregion
    }
}
