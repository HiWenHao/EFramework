/*
 * ================================================
 * Describe:        Key 搜索弹窗：输入过滤 + 列表选择
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 13:45:42
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 11:26:00
 * ScriptVersion:   0.4
 * ================================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using EasyFramework.Edit;
using UnityEditor;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// Key 搜索弹窗：输入过滤 + 列表选择
    /// <para>key 数量较多时比下拉更高效；支持 key / 说明 双字段过滤。
    /// 行内以「加粗 key + 灰色说明」呈现，悬停高亮，当前已选 key 行带 ✓ 标记。</para>
    /// </summary>
    internal class KeySearchWindow : EditorWindow
    {
        /// <summary> 窗口默认宽 </summary>
        private const float WINDOW_WIDTH = 420f;

        /// <summary> 窗口默认高 </summary>
        private const float WINDOW_HEIGHT = 320f;

        /// <summary>
        /// 行高（单行文本 + RowStyle 上下内边距 5+5 的余量）
        /// <para>GetControlRect 不传高度时只保留单行高，会被内边距挤到裁掉下半截文字。</para>
        /// </summary>
        private const float ROW_HEIGHT = 22f;

        /// <summary> 当前筛选文本 </summary>
        private string _filter = string.Empty;

        /// <summary> 过滤后的条目 </summary>
        private readonly List<LocalizationKeyEntry> _visible = new List<LocalizationKeyEntry>();

        /// <summary> 滚动位置 </summary>
        private Vector2 _scroll;

        /// <summary> 选中后的回调（参数为key 名，空串表示清空） </summary>
        private Action<string> _onPicked;

        /// <summary> 打开时的当前 key（列表中给该行打标记，便于重选时定位） </summary>
        private string _currentKey = string.Empty;

        /// <summary> 过滤前总数（Filter 时记录，供头部计数显示） </summary>
        private int _filterTotal;

        /// <summary>
        /// 打开弹窗
        /// </summary>
        /// <param name="keys">全量 key</param>
        /// <param name="currentKey">组件当前的 key（列表中标记该行）；可为空</param>
        /// <param name="onPicked">选中回调；关闭不选则不触发</param>
        public static void Show(List<LocalizationKeyEntry> keys, string currentKey, Action<string> onPicked)
        {
            var window = CreateInstance<KeySearchWindow>();
            window.titleContent = new GUIContent(LC.Combine(Lc.Key, Lc.Select, Lc.Panel));
            window.minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            window._onPicked = onPicked;
            window._currentKey = currentKey ?? string.Empty;
            window.Filter(keys);
            window.ShowUtility();
            window.Focus();
        }

        /// <summary>
        /// 按当前筛选文本过滤 key
        /// <para>同时匹配 name 与 des，大小写不敏感；筛选为空时返回全部。</para>
        /// </summary>
        /// <param name="keys">全量 key 条目</param>
        private void Filter(List<LocalizationKeyEntry> keys)
        {
            _visible.Clear();
            _filterTotal = keys?.Count ?? 0;

            foreach (var entry in keys)
            {
                if (string.IsNullOrEmpty(_filter))
                {
                    _visible.Add(entry);
                    continue;
                }

                bool hitName = entry.name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;
                bool hitDes = !string.IsNullOrEmpty(entry.des) &&
                              entry.des.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;

                if (hitName || hitDes) _visible.Add(entry);
            }
        }

        /// <summary>
        /// 绘制弹窗
        /// <para>每帧重新读盘并过滤，保证面板改动后无需重开即可看到最新 key。</para>
        /// <para><b>结构约束：</b>选中处理统一放在所有 Begin/End 作用域闭合之后——
        /// OnGUI 内任何提前 return 都会跳过配对的 End 调用，使 GUI 布局组失衡，
        /// 报「pushing more GUIClips than you are popping」。因此本方法用局部变量收集
        /// 本帧命中的选中项，布局树完整走完后再统一触发回调与关窗。</para>
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(BodyStyle);

            EditorGUILayout.BeginHorizontal();
            _filter = EditorGUILayout.TextField(_filter, SearchFieldStyle);
            if (GUILayout.Button(GUIContent.none, SearchCancelStyle, GUILayout.Width(18f)))
            {
                _filter = string.Empty;
            }

            EditorGUILayout.EndHorizontal();

            var keys = LocalizedTextEditor.LoadKeys();
            Filter(keys);

            EditorGUILayout.LabelField($"{_visible.Count} / {_filterTotal}", CountStyle);
            EditorGUILayout.Space(2f);

            // 本帧命中的选中项；null 表示本帧无点击（string.Empty 语义为「清空选择」，不可用 null 判断）
            string picked = null;

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // 首项固定为「清空选择」：显示 NONE_LABEL，实际写入空串
            if (GUI.Button(EditorGUILayout.GetControlRect(false, ROW_HEIGHT), ClearRowContent(), RowStyle))
                picked = string.Empty;

            if (_visible.Count == 0)
                EditorGUILayout.HelpBox(LC.Combine(Lc.No, Lc.Match), MessageType.Info);

            foreach (var entry in _visible)
            {
                if (GUI.Button(EditorGUILayout.GetControlRect(false, ROW_HEIGHT), RowContent(entry), RowStyle))
                {
                    picked = entry.name;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();

            if (picked != null)
            {
                Pick(picked);
            }
        }

        /// <summary>
        /// 行内容：加粗 key + 灰色说明；当前已选 key 行加 ✓ 标记
        /// </summary>
        private GUIContent RowContent(LocalizationKeyEntry entry)
        {
            var gray = ProSkin ? "#9A9A9A" : "#6E6E6E";
            var text = $"<b>{EscapeRich(entry.name)}</b>";

            if (!string.IsNullOrEmpty(entry.des))
            {
                text += $"    <color={gray}>{EscapeRich(entry.des)}</color>";
            }

            if (string.Equals(entry.name, _currentKey, StringComparison.Ordinal))
            {
                var accent = ProSkin ? "#5AA2E8" : "#1D6FBF";
                text = $"<color={accent}>✓ </color>" + text;
            }

            return new GUIContent(text);
        }

        /// <summary>
        /// 「清空选择」行内容（灰色斜体，弱于普通条目）
        /// </summary>
        private static GUIContent ClearRowContent()
        {
            var gray = ProSkin ? "#9A9A9A" : "#6E6E6E";
            return new GUIContent($"<color={gray}><i>{LC.Combine(Lc.Clear)}</i></color>");
        }

        /// <summary>
        /// 富文本转义（key 虽有命名规范，des 是自由文本，防 &lt; &gt; &amp; 破坏解析）
        /// </summary>
        private static string EscapeRich(string text)
            => text?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;") ?? string.Empty;

        /// <summary>
        /// 窗口销毁：释放对外部对象的引用
        /// <para>避免 Inspector 被 GC 后，闭包仍持有 LocalizedText 引用。</para>
        /// </summary>
        private void OnDestroy()
        {
            // 关闭即清空回调，避免窗口被 GC 后仍持有外部引用
            _onPicked = null;
        }

        /// <summary>
        /// 触发选中回调并关闭窗口
        /// <para>回调只允许触发一次，故调用后立刻置空。</para>
        /// </summary>
        /// <param name="keyName">选中的 key 名；空串表示清空选择</param>
        private void Pick(string keyName)
        {
            var cb = _onPicked;
            _onPicked = null;
            cb?.Invoke(keyName);
            Close();
        }

        #region 样式 Styles

        /// <summary> 是否深色主题（样式取色用） </summary>
        private static bool ProSkin => EditorGUIUtility.isProSkin;

        /// <summary> 窗体内容边距 </summary>
        private static GUIStyle _bodyStyle;

        private static GUIStyle BodyStyle
        {
            get
            {
                _bodyStyle ??= new GUIStyle { padding = new RectOffset(10, 10, 8, 8) };

                return _bodyStyle;
            }
        }

        /// <summary> 搜索框（原生 ToolbarSearchTextField，缺失时回落普通输入框） </summary>
        private static GUIStyle _searchFieldStyle;

        private static GUIStyle SearchFieldStyle
        {
            get
            {
                if (_searchFieldStyle == null)
                {
                    try
                    {
                        _searchFieldStyle = new GUIStyle("ToolbarSearchTextField");
                    }
                    catch (ArgumentException)
                    {
                        _searchFieldStyle = new GUIStyle(EditorStyles.textField);
                    }
                }

                return _searchFieldStyle;
            }
        }

        /// <summary> 搜索清除按钮（与搜索框样式配套；缺失时回落空白按钮） </summary>
        private static GUIStyle _searchCancelStyle;

        private static GUIStyle SearchCancelStyle
        {
            get
            {
                if (_searchCancelStyle == null)
                {
                    try
                    {
                        _searchCancelStyle = new GUIStyle("ToolbarSearchCancelButton");
                    }
                    catch (ArgumentException)
                    {
                        _searchCancelStyle = new GUIStyle();
                    }
                }

                return _searchCancelStyle;
            }
        }

        /// <summary> 头部计数（右对齐灰字） </summary>
        private static GUIStyle _countStyle;

        private static GUIStyle CountStyle
        {
            get
            {
                _countStyle ??= new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.UpperRight,
                    padding = new RectOffset(0, 4, 4, 0),
                };

                return _countStyle;
            }
        }

        /// <summary> 行样式：富文本 + 内边距 + 悬停 / 按下高亮（按主题取色） </summary>
        private static GUIStyle _rowStyle;

        private static GUIStyle RowStyle
        {
            get
            {
                _rowStyle ??= new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    padding = new RectOffset(10, 10, 2, 2),
                };
                return _rowStyle;
            }
        }

        #endregion
    }
}