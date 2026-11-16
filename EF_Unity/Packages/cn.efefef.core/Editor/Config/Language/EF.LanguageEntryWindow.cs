/*
 * ================================================
 * Describe:      编辑器多语言词条管理窗口（整合全部包词条，只读浏览）
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2026-11-16 13:00:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-16
 * ScriptVersion: 1.0
 * ================================================
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EasyFramework.Edit.Windows;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 编辑器多语言词条管理窗口：整合全部包的词条，按包分组展示（键 / 描述 / 中文 / 英文），支持搜索过滤，只读浏览。
    /// <para>Editor language entries window: merges entries from every package, shows them grouped
    /// by package (key / description / Chinese / English) with search filtering; read-only.</para>
    /// </summary>
    internal class EFLanguageEntryWindow : EditorWindowBase
    {
        #region Fields

        /// <summary>单个包的词条分组<para>Entry group of one package.</para></summary>
        private sealed class PackageGroup
        {
            // 包名
            public string PackageName;
            // 词条行（键序）
            public List<LanguageEntryInfo> Rows = new List<LanguageEntryInfo>();
            // 折叠状态
            public bool Foldout = true;
        }

        private const float KeyColumnWidth = 240f;      // 键列宽
        private const float DescColumnWidth = 170f;     // 描述列宽
        private const float TextColumnWidth = 170f;     // 中/英文列宽

        private List<PackageGroup> _groups = new List<PackageGroup>();  // 按包分组数据（LoadWindowData 构建）
        private int _totalEntries;                                      // 词条总数（未过滤）
        private string _searchText = string.Empty;                      // 搜索关键字（匹配键 / 描述 / 中英文本）
        private Vector2 _scrollPosition;                                // 滚动位置

        #endregion

        #region Methods

        /// <summary>
        /// 打开词条管理窗口
        /// <para>Open the language entries window.</para>
        /// </summary>
        [MenuItem(MenuItemToolkit.Utility + "🌐 Language Entries", false, MenuItemToolkit.UtilityPriority + 4)]
        private static void OpenWindow()
        {
            var window = GetWindow<EFLanguageEntryWindow>(LC.Tr("语言词条", "Language Entries"));
            window.minSize = new Vector2(760f, 420f);
        }

        /// <summary>
        /// 从 LC 整合接口加载全部包词条并按包分组
        /// <para>Load merged entries from LC and group them by package.</para>
        /// </summary>
        protected override void LoadWindowData()
        {
            var infos = LC.CollectEntriesForInspection();
            var groupMap = new Dictionary<string, PackageGroup>(StringComparer.Ordinal);
            _groups = new List<PackageGroup>();
            _totalEntries = infos.Count;
            _searchText = string.Empty;
            foreach (var info in infos)
            {
                if (!groupMap.TryGetValue(info.PackageName, out var group))
                {
                    group = new PackageGroup { PackageName = info.PackageName };
                    groupMap.Add(info.PackageName, group);
                    _groups.Add(group);
                }
                group.Rows.Add(info);
            }
        }

        /// <summary>
        /// 绘制工具栏（搜索 / 统计 / 刷新）与按包分组的词条列表
        /// <para>Draw the toolbar (search / stats / refresh) and the package-grouped entry list.</para>
        /// </summary>
        protected override void OnSmartGUI()
        {
            EditorGUILayout.BeginHorizontal();
            _searchText = EditorGUILayout.TextField(LC.Combine("Search"), _searchText);
            if (GUILayout.Button(LC.Combine("Refresh"), GUILayout.Width(64f)))
            {
                LC.ReloadLanguages();
                LoadWindowData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(LC.Tr($"共 {_totalEntries} 词条，当前显示 {CountVisible()}。",
                $"{_totalEntries} entries in total, {CountVisible()} shown."));
            if (_totalEntries == 0)
            {
                EditorGUILayout.HelpBox(
                    LC.Tr("未整合到任何词条：请确认各包存在 Editor Resources/Description/Editorlanguages.json，并查看 Console 的 [ LC ] 告警。",
                        "No entries merged: make sure each package has Editor Resources/Description/Editorlanguages.json and check [ LC ] warnings in the Console."),
                    MessageType.Warning);
            }
            EditorGUILayout.Space(4f);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (var group in _groups)
            {
                var visibleRows = FilterRows(group.Rows);
                if (visibleRows.Count == 0)
                    continue;
                group.Foldout = EditorGUILayout.Foldout(group.Foldout, $"{group.PackageName} ({visibleRows.Count}/{group.Rows.Count})", true);
                if (!group.Foldout)
                    continue;

                DrawHeader();
                foreach (var row in visibleRows)
                    DrawRow(row);
                EditorGUILayout.Space(6f);
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 过滤当前组内可见词条（关键字匹配键 / 描述 / 中英文，忽略大小写）
        /// <para>Filter visible rows in one group (keyword matches key / description / zh-en text, case-insensitive).</para>
        /// </summary>
        /// <param name="rows">组内全部词条<para>All rows in the group.</para></param>
        /// <returns>可见词条列表<para>Visible rows.</para></returns>
        private List<LanguageEntryInfo> FilterRows(List<LanguageEntryInfo> rows)
        {
            if (string.IsNullOrEmpty(_searchText))
                return rows;
            var result = new List<LanguageEntryInfo>();
            foreach (var row in rows)
            {
                if (Contains(row.Key, _searchText) || Contains(row.Desc, _searchText)
                    || Contains(row.Chinese, _searchText) || Contains(row.English, _searchText))
                    result.Add(row);
            }
            return result;
        }

        /// <summary>
        /// 当前搜索条件下的可见词条总数
        /// <para>Total count of visible entries under the current search text.</para>
        /// </summary>
        /// <returns>可见词条数<para>Visible entry count.</para></returns>
        private int CountVisible()
        {
            int count = 0;
            foreach (var group in _groups)
                count += FilterRows(group.Rows).Count;
            return count;
        }

        /// <summary>
        /// 判断源文本是否包含关键字（忽略大小写；空文本不命中）
        /// <para>Whether the source text contains the keyword (case-insensitive; empty text never matches).</para>
        /// </summary>
        /// <param name="source">源文本<para>Source text.</para></param>
        /// <param name="keyword">关键字<para>Keyword.</para></param>
        /// <returns>包含返回 true<para>True when contained.</para></returns>
        private static bool Contains(string source, string keyword)
        {
            return !string.IsNullOrEmpty(source) && source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 绘制词条列表列头
        /// <para>Draw the column headers of the entry list.</para>
        /// </summary>
        private static void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LC.Combine("Key"), EditorStyles.boldLabel, GUILayout.Width(KeyColumnWidth));
            EditorGUILayout.LabelField(LC.Combine("Description"), EditorStyles.boldLabel, GUILayout.Width(DescColumnWidth));
            EditorGUILayout.LabelField(LC.Tr("中文", nameof(SystemLanguage.Chinese)), EditorStyles.boldLabel, GUILayout.Width(TextColumnWidth));
            EditorGUILayout.LabelField(nameof(SystemLanguage.English), EditorStyles.boldLabel, GUILayout.MinWidth(TextColumnWidth));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制单个词条行
        /// <para>Draw one entry row.</para>
        /// </summary>
        /// <param name="row">词条信息<para>Entry info.</para></param>
        private static void DrawRow(LanguageEntryInfo row)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(row.Key, EditorStyles.boldLabel, GUILayout.Width(KeyColumnWidth));
            EditorGUILayout.LabelField(row.Desc ?? string.Empty, EditorStyles.miniLabel, GUILayout.Width(DescColumnWidth));
            EditorGUILayout.LabelField(row.Chinese ?? "—", GUILayout.Width(TextColumnWidth));
            EditorGUILayout.LabelField(row.English ?? "—", GUILayout.MinWidth(TextColumnWidth));
            EditorGUILayout.EndHorizontal();
        }

        #endregion
    }
}