/*
 * ================================================
 * Describe:        Key 搜索弹窗：输入过滤 + 列表选择
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 13:45:42
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-14 13:45:42
 * ScriptVersion:   0.1
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
    /// <para>key 数量较多时比下拉更高效；支持 key / 说明 双字段过滤。</para>
    /// </summary>
    internal class KeySearchWindow : EditorWindow
    {
        /// <summary> 窗口默认宽 </summary>
        private const float WINDOW_WIDTH = 420f;

        /// <summary> 窗口默认高 </summary>
        private const float WINDOW_HEIGHT = 320f;

        /// <summary> 当前筛选文本 </summary>
        private string _filter = string.Empty;

        /// <summary> 过滤后的条目 </summary>
        private readonly List<LocalizationKeyEntry> _visible = new List<LocalizationKeyEntry>();

        /// <summary> 滚动位置 </summary>
        private Vector2 _scroll;

        /// <summary> 选中后的回调（参数为key 名，空串表示清空） </summary>
        private Action<string> _onPicked;

        /// <summary>
        /// 打开弹窗
        /// </summary>
        /// <param name="keys">全量 key</param>
        /// <param name="onPicked">选中回调；关闭不选则不触发</param>
        public static void Show(List<LocalizationKeyEntry> keys, Action<string> onPicked)
        {
            var window = CreateInstance<KeySearchWindow>();
            window.titleContent = new GUIContent(LC.Combine(Lc.Key, Lc.Select, Lc.Panel));
            window.minSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
            window._onPicked = onPicked;
            window.Filter(keys);
            window.ShowUtility();
            window.Focus();
        }

        /// <summary>
        /// 「清空选择」项的显示文案
        /// <para>仅用于界面显示；实际写入组件的是空串。</para>
        /// </summary>
        private const string NONE_LABEL = "(无)";

        /// <summary>
        /// 按当前筛选文本过滤 key
        /// <para>同时匹配 name 与 des，大小写不敏感；筛选为空时返回全部。</para>
        /// </summary>
        /// <param name="keys">全量 key 条目</param>
        private void Filter(List<LocalizationKeyEntry> keys)
        {
            _visible.Clear();

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
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍", GUILayout.Width(22f));
            _filter = EditorGUILayout.TextField(_filter);
            EditorGUILayout.EndHorizontal();

            var keys = LocalizedTextEditor.LoadKeys();
            Filter(keys);

            EditorGUILayout.LabelField($"{_visible.Count} / {keys.Count}", EditorStyles.miniLabel);
            EditorGUILayout.Space(2f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // 首项固定为「清空选择」：显示 NONE_LABEL，实际写入空串
            if (GUI.Button(EditorGUILayout.GetControlRect(false),
                    NONE_LABEL, EditorStyles.label))
            {
                Pick(string.Empty);
                return;
            }

            if (_visible.Count == 0)
            {
                EditorGUILayout.LabelField("无匹配结果", EditorStyles.miniLabel);
            }

            foreach (var entry in _visible)
            {
                var rect = EditorGUILayout.GetControlRect(false);

                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    Pick(entry.name);
                    return;
                }

                // 无说明时只显示 key
                var label = string.IsNullOrEmpty(entry.des) ? entry.name : $"{entry.name}    {entry.des}";

                if (GUI.Button(rect, label, EditorStyles.label))
                {
                    Pick(entry.name);
                    return;
                }
            }

            EditorGUILayout.EndScrollView();
        }

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
    }
}