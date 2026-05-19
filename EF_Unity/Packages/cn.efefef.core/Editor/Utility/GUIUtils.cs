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
        
        
        private static GUIStyle _text;
        /// <summary> 通用文本 </summary>
        public static GUIStyle Text(int fontSize = 12, FontStyle fontStyle = FontStyle.Normal)
        {
            _text ??= new GUIStyle(GUI.skin.label);
            _text.fontSize = fontSize;
            _text.fontStyle = fontStyle;
            return _text;
        }
        
        
        private static GUIStyle _colorText;
        /// <summary> 彩色文本 </summary>
        public static GUIStyle ColorText(int fontSize = 12, FontStyle fontStyle = FontStyle.Normal, Color textColor = default)
        {
            _colorText ??= new GUIStyle(GUI.skin.label);
            _colorText.fontSize = fontSize;
            _colorText.fontStyle = fontStyle;
            _colorText.normal.textColor = textColor;
            return _colorText;
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
    }
}