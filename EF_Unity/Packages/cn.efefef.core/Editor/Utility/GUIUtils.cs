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

using UnityEngine;

namespace EasyFramework.Edit.Windows
{
    /// <summary>
    /// 编辑器界面风格
    /// </summary>
    public static class GUIUtils
    {
        #region Color

        /// <summary> 浅红色 </summary>
        public static Color LightRed = new Color(1f, 0.4f, 0.4f);

        /// <summary> 浅绿色 </summary>
        public static Color LightGreen = new Color(0.3f, 0.8f, 0.3f);
        #endregion

        #region Text

        private static GUIStyle _titleStyle;
        /// <summary> 通用标题 </summary>
        public static GUIStyle Title(int fontSize = 20, Color color = default,
            FontStyle fontStyle = FontStyle.Bold, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            _titleStyle ??= new GUIStyle()
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                normal =
                {
                    textColor = color == default ? Color.white : color,
                },
                alignment = anchor,
                margin = new RectOffset(5, 5, 5, 5)
            };
            return _titleStyle;
        }

        private static GUIStyle _text;
        /// <summary> 通用文本 </summary>
        public static GUIStyle Text()
        {
            _text ??= new GUIStyle(GUI.skin.label);
            return _text;
        }

        #endregion

        #region Button

        private static GUIStyle _button;
        /// <summary> 通用按钮 </summary>
        public static GUIStyle Button(Color color = default, int fontSize = 12)
        {
            _button ??= new GUIStyle(GUI.skin.button);
            _button.normal.textColor = color == default ? Color.white : color;
            _button.hover.textColor = color == default ? Color.white : color;
            _button.active.textColor = color == default ? Color.white : color;
            _button.fontSize = fontSize;
            return _button;
        }

        #endregion

        #region Box background

        private static GUIStyle _backgroundStyle;
        /// <summary> 背景框 </summary>
        public static GUIStyle BackgroundStyle()
        {
            _backgroundStyle ??= new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(15, 15, 15, 15),
                margin = new RectOffset(5, 5, 5, 5)
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
    }
}