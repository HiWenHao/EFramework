/*
 * ================================================
 * Describe:      用来给拓展HeaderAttribute做绘制
 * Author:        Alvin8412
 * CreationTime:  2026-04-26 00:11:48
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-19 11:33:00
 * ScriptVersion: 0.3
 * ===============================================
 */

using EasyFramework.Edit;
using EasyFramework.Edit.Windows;
using UnityEngine;
using UnityEditor;

namespace EasyFramework
{
    [CustomPropertyDrawer(typeof(HeaderProAttribute))]
    public class HeaderProDrawer : DecoratorDrawer
    {
        // 分隔线高度（像素）
        private const float DividerHeight = 1f;

        // 独立分隔线类型：线与上下留白的间距（像素）
        private const float SeparatorPadding = 5f;

        // 独立分隔线颜色
        private static readonly Color SeparatorColor = new Color(1f, 1f, 1f, 0.25f);

        private static GUIStyle _titleStyle;
        private static GUIStyle _subtitleStyle;
        private static GUIStyle _boldNoteStyle;
        private static GUIStyle _noteStyle;

        // 标题：13 加粗暖黄
        private static GUIStyle TitleStyle => _titleStyle ??= CreateStyle(13, FontStyle.Bold, GUIUtils.LightYellow);

        // 副标题：12 常规暖黄（分组内次级）
        private static GUIStyle SubtitleStyle =>
            _subtitleStyle ??= CreateStyle(12, FontStyle.Normal, GUIUtils.LightYellow);

        // 加粗注释：12 加粗普通色（强调说明）
        private static GUIStyle BoldNoteStyle => _boldNoteStyle ??= CreateStyle(12, FontStyle.Bold, default);

        // 普通注释：12 常规普通色
        private static GUIStyle NoteStyle => _noteStyle ??= CreateStyle(12, FontStyle.Normal, default);

        private static GUIStyle CreateStyle(int fontSize, FontStyle fontStyle, Color textColor)
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = TextAnchor.MiddleLeft
            };

            // 锁死所有交互态文字色：GUI.Label 虽不响应点击，但 hover 态继承 boldLabel 的悬停色会变色。
            // 注释类沿用 boldLabel 的主题色（跟随深浅主题），标题/副标题用传入的强调色。
            Color color = textColor != default ? textColor : EditorStyles.boldLabel.normal.textColor;
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
            return style;
        }

        // 分隔线类型 = 线 + 上下留白；其余（含标题）= 单行
        public override float GetHeight()
        {
            var header = attribute as HeaderProAttribute;
            if (header != null && header.Type == HeaderProType.Separator)
                return SeparatorPadding * 2 + DividerHeight;
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position)
        {
            if (attribute is not HeaderProAttribute headerAttribute)
                return;

            // 分隔线类型：不画文字，只画一条铺满面板宽度的横线
            if (headerAttribute.Type == HeaderProType.Separator)
            {
                float lineY = position.y + SeparatorPadding;
                var lineRect = new Rect(position.x, lineY, position.width, DividerHeight);
                EditorGUI.DrawRect(lineRect, SeparatorColor);
                return;
            }

            bool isEnglish = LC.IsEnglish;
            string headerText = isEnglish ? headerAttribute.English : headerAttribute.Chinese;
            string toolText = isEnglish ? headerAttribute.Chinese : headerAttribute.English;

            GUIStyle style = headerAttribute.Type switch
            {
                HeaderProType.Subtitle => SubtitleStyle,
                HeaderProType.BoldNote => BoldNoteStyle,
                HeaderProType.Note => NoteStyle,
                _ => TitleStyle
            };

            var labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            GUI.Label(labelRect, new GUIContent(headerText, toolText), style);
        }
    }
}