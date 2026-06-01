/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-04-14 17:24:53
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-14 17:24:53
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows
{
    /// <summary>
    /// 编辑器界面风格
    /// </summary>
    public static class GUIUtils
    {
        #region Color

        /// <summary> 浅黄色 </summary>
        public static Color LightYellow = new Color(0.9f, 0.9f, 0.8f);
        
        /// <summary> 浅红色 </summary>
        public static Color LightRed = new Color(1f, 0.4f, 0.4f);

        /// <summary> 浅绿色 </summary>
        public static Color LightGreen = new Color(0.3f, 0.8f, 0.3f);
        #endregion

        #region Text

        private static GUIStyle _inspectorTitleStyle;
        /// <summary> Inspector通用标题 </summary>
        public static GUIStyle InspectorTitle()
        {
            _inspectorTitleStyle ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(5, 5, 5, 5),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.9f, 0.8f) }
            };
            return _inspectorTitleStyle;
        }

        
        private static GUIStyle _titleStyle;
        /// <summary> 通用标题 </summary>
        public static GUIStyle Title()
        {
            _titleStyle ??= new GUIStyle()
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = new Color(0.7f, 0.7f, 0.7f),
                },
                alignment = TextAnchor.UpperLeft
            };
            return _titleStyle;
        }


        private static GUIStyle _smallNote;
        /// <summary> 小提示 </summary>
        public static GUIStyle SmallNote()
        {
            _smallNote ??= new GUIStyle()
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.gray }
            };
            return _smallNote;
        }
        
        
        /// <summary> 通用文本（每次返回新实例，避免多调用方共享状态被覆盖） </summary>
        public static GUIStyle Text(int fontSize = 12, FontStyle fontStyle = FontStyle.Normal)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle
            };
        }

        /// <summary> 彩色文本（每次返回新实例，避免多调用方共享状态被覆盖） </summary>
        public static GUIStyle ColorText(int fontSize = 12, FontStyle fontStyle = FontStyle.Normal, Color textColor = default)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                normal = { textColor = textColor }
            };
        }


        /// <summary>
        /// 带图标的文本
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="iconName">图标名 以 d_ 开头</param>
        public static GUIContent IconText(string text, string iconName)
        {
            GUIContent iconText = EditorGUIUtility.IconContent(iconName);
            iconText.text = text;
            return iconText;
        }
        
        
        #endregion

        #region Button

        /// <summary> 通用按钮（每次返回新实例，避免多调用方共享状态被覆盖） </summary>
        public static GUIStyle Button(Color color = default, int fontSize = 12)
        {
            Color c = color == default ? Color.white : color;
            return new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                normal = { textColor = c },
                hover = { textColor = c },
                active = { textColor = c }
            };
        }

        #endregion

        #region Box background

        private static GUIStyle _backgroundStyle;
        /// <summary> 背景框 </summary>
        public static GUIStyle BackgroundStyle()
        {
            _backgroundStyle ??= new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 11, 11),
                margin = new RectOffset(5, 5, 3, 3)
            };
            return _backgroundStyle;
        }
        
        
        private static GUIStyle _scrollViewBackground;
        /// <summary> 滑动背景框 </summary>
        public static GUIStyle ScrollViewBackground()
        {
            _scrollViewBackground ??= new GUIStyle("Badge")
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };
            return _scrollViewBackground;
        }
        
        
        #endregion

        #region 覆盖层（刷新/编译/错误）

        public static readonly GUIStyle OverlayLabel = new(EditorStyles.largeLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontSize = 14,
        };

        public static readonly GUIStyle OverlaySmallLabel = new(EditorStyles.largeLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white },
            fontSize = 10,
        };

        public static readonly Color OverlayRefreshBg = new(0.15f, 0.15f, 0.15f, 0.95f);
        public static readonly Color OverlayErrorBg = new(0.3f, 0.1f, 0.1f, 0.95f);

        #endregion

        #region 重命名工具

        /// <summary> 旧名称（灰色小字） </summary>
        public static GUIStyle OldNameStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                fontSize = 10
            };
        }

        /// <summary> 预览名称（绿色加粗） </summary>
        public static GUIStyle PreviewNameStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.2f, 0.8f, 0.4f) },
                fontStyle = FontStyle.Bold
            };
        }

        /// <summary> 资产类型标签（居中白字小标签） </summary>
        public static GUIStyle TagStyle()
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                normal = { textColor = Color.white }
            };
        }

        /// <summary> 删除按钮（红色文字） </summary>
        public static GUIStyle RemoveBtnStyle()
        {
            return new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.3f, 0.3f) },
                hover = { textColor = Color.red }
            };
        }

        #endregion

        #region 脚本工具

        /// <summary> 左对齐按钮 </summary>
        public static GUIStyle LeftButtonStyle()
        {
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            };
        }

        /// <summary> 居中加粗标题 </summary>
        public static GUIStyle BoldCenterTitle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 30
            };
        }

        /// <summary> 居中结束标记 </summary>
        public static GUIStyle CenteredEndLabel()
        {
            return new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        #endregion

        #region 进度条

        public static readonly GUIStyle ProgressStatusLabel = new(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize = 12,
            fontStyle = FontStyle.Normal,
            fixedHeight = 20,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
        };

        public static readonly GUIStyle ProgressPctLabel = new(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            normal = { textColor = Color.white },
        };

        public static readonly Color ProgressBg = new(0.16f, 0.16f, 0.18f, 0.75f);
        public static readonly Color ProgressGreen = new(0.22f, 0.72f, 0.30f, 1f);
        public static readonly Color ProgressGreenGlow = new(0.55f, 0.95f, 0.55f, 0.40f);
        public static readonly Color ProgressBlue = new(0.30f, 0.58f, 0.92f, 0.80f);
        public static readonly Color ProgressBlueGlow = new(0.55f, 0.78f, 1f, 0.35f);
        public static readonly Color ProgressHighlight = new(1f, 1f, 1f, 0.15f);
        public static readonly Color ProgressBorder = new(0.35f, 0.35f, 0.35f, 0.55f);

        #endregion
    }
}