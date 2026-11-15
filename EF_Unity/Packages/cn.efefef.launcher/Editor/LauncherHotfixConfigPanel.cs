/*
 * ================================================
 * Describe:        EF config panel for auto-binding hotfix dll entries.
 * Author:          Alvin8412
 * CreationTime:    2026-11-15 18:20:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows.ConfigPanel;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Launcher.Editor
{
    /// <summary>
    /// 热更程序集绑定面板：配置 .dll.bytes 搜索目录与 Location 规则，预览并一键写回 LauncherConfig。
    /// <para>Configures the dll.bytes lookup and Location rule, previews the result, and writes back in one click.</para>
    /// </summary>
    [EFConfigPanel(Priority = 210)]
    internal sealed class LauncherHotfixConfigPanel : EFConfigPanelBase
    {
        private const string DialogTitle = "EF.Launcher";

        private static readonly string[] RuleNamesZh =
        {
            "按文件名 (AddressByFileName)",
            "资产路径去掉 .bytes",
            "自定义前缀 + 程序集名",
        };

        private static readonly string[] RuleNamesEn =
        {
            "File name (AddressByFileName)",
            "Asset path without .bytes",
            "Custom prefix + assembly",
        };

        private LauncherConfig[] _configs = Array.Empty<LauncherConfig>();
        private string[] _configNames = Array.Empty<string>();
        private int _configIndex;
        private HotfixBindOptions _options;
        private HotfixBindResult _preview;
        private Vector2 _scroll;
        private string _status = string.Empty;

        public override string Name => LC.Tr("热更程序集", "Hotfix Assemblies");

        public override void OnEnable(string assetsPath) => LoadWindowData();

        public override void LoadWindowData()
        {
            _options = HotfixBindOptions.Load();
            RefreshConfigs();
            _preview = null;
            _status = string.Empty;
        }

        protected override void OnPanelGUI()
        {
            if (_options == null)
                LoadWindowData();

            EditorGUILayout.HelpBox(LC.Tr(
                "按 HybridCLR 热更列表生成 HotfixDlls 条目，并自动填 Location / FallbackAsset。" +
                "条目全集取自 hotUpdateAssemblyDefinitions，不会漏掉业务热更程序集。",
                "Builds HotfixDlls entries from the HybridCLR hot update list and fills Location / FallbackAsset. " +
                "The entry set comes from hotUpdateAssemblyDefinitions, so business assemblies are never missed."),
                MessageType.Info);

            DrawTargetConfig();
            EditorGUILayout.Space(6f);
            DrawOptions();
            EditorGUILayout.Space(6f);
            DrawActions();

            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.None);

            DrawPreview();
        }

        public override void OnDestroy()
        {
            _options?.Save();
        }

        #region 面板绘制

        private void DrawTargetConfig()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LC.Tr("目标配置", "Target config"), GUILayout.Width(80f));

            if (_configs.Length == 0)
            {
                EditorGUILayout.LabelField(LC.Tr("工程里没有 LauncherConfig", "No LauncherConfig found"));
                EditorGUILayout.EndHorizontal();
                return;
            }

            int next = EditorGUILayout.Popup(_configIndex, _configNames);
            if (next != _configIndex && next >= 0 && next < _configs.Length)
            {
                _configIndex = next;
                _options.LastConfigGuid = AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(_configs[_configIndex]));
                _options.Save();
                _preview = null;
            }

            if (GUILayout.Button(LC.Tr("定位", "Ping"), GUILayout.Width(60f)))
            {
                Selection.activeObject = _configs[_configIndex];
                EditorGUIUtility.PingObject(_configs[_configIndex]);
            }

            if (GUILayout.Button(LC.Tr("刷新", "Refresh"), GUILayout.Width(60f)))
                RefreshConfigs();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawOptions()
        {
            EditorGUILayout.LabelField(LC.Tr("── 绑定规则 ──", "── Binding rules ──"), EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LC.Tr(".bytes 目录", ".bytes folder"), GUILayout.Width(80f));
            _options.SearchRoot = EditorGUILayout.TextField(_options.SearchRoot);
            if (GUILayout.Button(LC.Tr("选择", "Browse"), GUILayout.Width(60f)))
            {
                string picked = EditorUtility.OpenFolderPanel(LC.Tr("选择热更 dll 目录", "Pick hotfix dll folder"),
                    Application.dataPath, string.Empty);

                if (!string.IsNullOrEmpty(picked))
                {
                    string dataPath = Application.dataPath.Replace('\\', '/');
                    string normalized = picked.Replace('\\', '/');
                    _options.SearchRoot = normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase)
                        ? "Assets" + normalized.Substring(dataPath.Length)
                        : normalized;
                    _preview = null;
                }
            }

            EditorGUILayout.EndHorizontal();

            _options.SearchWholeAssets = EditorGUILayout.ToggleLeft(
                LC.Tr("目录里找不到时，全 Assets 搜索同名 .dll.bytes", "Fall back to a project-wide .dll.bytes search"),
                _options.SearchWholeAssets);

            string[] ruleNames = LC.IsEnglish ? RuleNamesEn : RuleNamesZh;
            _options.Rule = (HotfixLocationRule)EditorGUILayout.Popup(
                LC.Tr("Location 规则", "Location rule"), (int)_options.Rule, ruleNames);

            if (_options.Rule == HotfixLocationRule.CustomPrefix)
            {
                _options.CustomPrefix = EditorGUILayout.TextField(
                    LC.Tr("自定义前缀", "Custom prefix"), _options.CustomPrefix);
            }

            _options.OverwriteExisting = EditorGUILayout.ToggleLeft(
                LC.Tr("覆盖已填的 Location / FallbackAsset（默认只补空字段）",
                    "Overwrite existing Location / FallbackAsset (off = fill blanks only)"),
                _options.OverwriteExisting);

            EditorGUILayout.HelpBox(LC.Tr(
                "当前工程收集器为 AddressByFileName，Location 应为纯文件名（如 EF.UI.Runtime.dll）。" +
                "若手工填的是 Assets/... 全路径，可切到「按文件名」并勾选覆盖来批量修正。",
                "This project's collector uses AddressByFileName, so Location should be a bare file name " +
                "(e.g. EF.UI.Runtime.dll). To fix hand-written full paths, pick the file-name rule and enable overwrite."),
                MessageType.Info);
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(LC.Tr("🔍 扫描预览", "🔍 Scan preview"), GUILayout.Height(26f)))
            {
                _options.Save();
                _preview = LauncherHotfixEntryBinder.Build(CurrentConfig, _options, apply: false);
                _status = Describe(_preview, applied: false);
            }

            if (GUILayout.Button(LC.Tr("✅ 应用到配置", "✅ Apply to config"), GUILayout.Height(26f)))
            {
                var config = CurrentConfig;
                if (config == null)
                {
                    EditorUtility.DisplayDialog(DialogTitle,
                        LC.Tr("请先选择一个 LauncherConfig。", "Select a LauncherConfig first."), LC.Tr("确定", "OK"));
                }
                else
                {
                    _options.Save();
                    _preview = LauncherHotfixEntryBinder.Build(config, _options, apply: true);
                    _status = Describe(_preview, applied: true);
                    Debug.Log("[EF.Launcher] " + _status.Replace("\n", " | "));
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreview()
        {
            if (_preview == null || _preview.Rows.Count == 0)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(LC.Tr("── 预览 ──", "── Preview ──"), EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(120f));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(LC.Tr("程序集", "Assembly"), GUILayout.Width(170f));
            EditorGUILayout.LabelField("Location", GUILayout.Width(220f));
            EditorGUILayout.LabelField("Fallback", GUILayout.Width(90f));
            EditorGUILayout.LabelField(LC.Tr("状态", "Status"));
            EditorGUILayout.EndHorizontal();

            foreach (var row in _preview.Rows)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(row.AssemblyName, GUILayout.Width(170f));
                EditorGUILayout.LabelField(row.Location ?? "-", GUILayout.Width(220f));
                EditorGUILayout.LabelField(row.Fallback != null ? "OK" : "-", GUILayout.Width(90f));
                EditorGUILayout.LabelField(row.Status);
                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(row.AssetPath))
                {
                    EditorGUILayout.LabelField("    " + row.AssetPath, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region 数据

        private LauncherConfig CurrentConfig =>
            _configs.Length > 0 && _configIndex >= 0 && _configIndex < _configs.Length
                ? _configs[_configIndex]
                : null;

        private void RefreshConfigs()
        {
            var list = new List<LauncherConfig>();
            foreach (string guid in AssetDatabase.FindAssets("t:LauncherConfig"))
            {
                var config = AssetDatabase.LoadAssetAtPath<LauncherConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (config != null)
                    list.Add(config);
            }

            _configs = list.ToArray();
            _configNames = new string[_configs.Length];

            _configIndex = 0;
            for (int i = 0; i < _configs.Length; i++)
            {
                _configNames[i] = _configs[i].name;
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_configs[i]));
                if (!string.IsNullOrEmpty(_options.LastConfigGuid) && guid == _options.LastConfigGuid)
                    _configIndex = i;
            }
        }

        private static string Describe(HotfixBindResult result, bool applied)
        {
            if (!string.IsNullOrEmpty(result.Error))
                return result.Error;

            string head = applied
                ? LC.Tr("已写入配置", "Config updated")
                : LC.Tr("预览结果", "Preview");

            return LC.Tr(
                $"{head}：共 {result.Rows.Count} 条，新增 {result.Added} 条，" +
                $"绑定 Location {result.LocationBound} 条 / FallbackAsset {result.FallbackBound} 条，缺失 {result.Missing} 条。",
                $"{head}: {result.Rows.Count} entries, {result.Added} added, " +
                $"{result.LocationBound} locations / {result.FallbackBound} fallbacks bound, {result.Missing} missing.");
        }

        #endregion

        #region 菜单

        [MenuItem(MenuItemToolkit.Launcher + "🔗 Auto Bind Hotfix Dll Entries", false, MenuItemToolkit.LauncherPriority + 55)]
        public static void AutoBindFromMenu()
        {
            var options = HotfixBindOptions.Load();
            var config = ResolveConfig(options);

            if (config == null)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                    "没找到唯一的 LauncherConfig。\n\n请在 Project 窗口选中配置，或在「EF 配置面板 → 热更程序集」里选一个。",
                    "No single LauncherConfig found.\n\nSelect one in the Project window, or pick one in the EF config panel."),
                    LC.Tr("确定", "OK"));
                return;
            }

            var result = LauncherHotfixEntryBinder.Build(config, options, apply: true);
            string message = Describe(result, applied: true);
            Debug.Log("[EF.Launcher] " + message + LC.Tr($"\n目标配置：{config.name}", $"\nConfig: {config.name}"));
            EditorUtility.DisplayDialog(DialogTitle, message + LC.Tr($"\n\n目标配置：{config.name}", $"\n\nConfig: {config.name}"),
                LC.Tr("好", "OK"));
        }

        private static LauncherConfig ResolveConfig(HotfixBindOptions options)
        {
            if (!string.IsNullOrEmpty(options.LastConfigGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(options.LastConfigGuid);
                if (!string.IsNullOrEmpty(path))
                {
                    var remembered = AssetDatabase.LoadAssetAtPath<LauncherConfig>(path);
                    if (remembered != null)
                        return remembered;
                }
            }

            if (Selection.activeObject is LauncherConfig selected)
                return selected;

            string[] guids = AssetDatabase.FindAssets("t:LauncherConfig");
            return guids.Length == 1
                ? AssetDatabase.LoadAssetAtPath<LauncherConfig>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        #endregion
    }
}
