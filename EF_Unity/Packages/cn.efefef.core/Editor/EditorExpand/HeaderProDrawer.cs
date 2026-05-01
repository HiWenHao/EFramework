/*
 * ================================================
 * Describe:      用来给拓展HeaderAttribute做绘制
 * Author:        Alvin8412
 * CreationTime:  2026-04-26 00:11:48
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-26 00:11:48
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;
using UnityEditor;

namespace EasyFramework.Edit
{
    [CustomPropertyDrawer(typeof(HeaderProAttribute))]
    public class HeaderProDrawer : DecoratorDrawer
    {
        public override float GetHeight() => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position)
        {
            HeaderProAttribute headerAttribute = attribute as HeaderProAttribute;
            if (headerAttribute == null)
                return;
            
            bool isEnglish = LC.DisplayLanguage == ELanguage.English;
            string headerText =  isEnglish ? headerAttribute.English : headerAttribute.Chinese;
            string toolText =  isEnglish ? headerAttribute.Chinese : headerAttribute.English;
            position.yMin += EditorGUIUtility.singleLineHeight * 0.2f;
            GUI.Label(position, new GUIContent(headerText, toolText), EditorStyles.label);
        }
    }
}