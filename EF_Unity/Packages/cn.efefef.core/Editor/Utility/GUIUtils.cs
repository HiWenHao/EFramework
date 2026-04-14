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
        public static Color LightRed = new Color(0.9f, 0.4f, 0.4f);

        #endregion
        
        #region Text
        
        /// <summary> 通用标题 </summary>
        public static GUIStyle Title
        {
            get
            {
                _titleStyle ??= new GUIStyle()
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = Color.white
                    },
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(5, 5, 5, 5)
                };
                return _titleStyle;
            }
        }
        private static GUIStyle _titleStyle;

        /// <summary> 通用文本 </summary>
        public static GUIStyle Text
        {
            get
            {
                _text ??= new GUIStyle(GUI.skin.label);
                return _text;
            }
        }
        private static GUIStyle _text;

        #endregion

        #region Button
        
        /// <summary> 通用按钮 </summary>
        public static GUIStyle Button
        {
            get
            {
                _button ??= new GUIStyle(GUI.skin.button);
                return _button;
            }
        }
        private static GUIStyle _button;

        #endregion
    }
}