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

        public override string Name => LC.Combine("Launcher.HotfixAssemblies");

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

            EditorGUILayout.HelpBox(LC.Combine("Launcher.BinderIntro"),
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
            EditorGUILayout.LabelField(LC.Combine("Launcher.TargetConfig"), GUILayout.Width(80f));

            if (_configs.Length == 0)
            {
                EditorGUILayout.LabelField(LC.Combine("Launcher.NoConfig"));
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

            if (GUILayout.Button(LC.Combine("Launcher.Ping"), GUILayout.Width(60f)))
            {
                Selection.activeObject = _configs[_configIndex];
                EditorGUIUtility.PingObject(_configs[_configIndex]);
            }

            if (GUILayout.Button(LC.Combine("Refresh"), GUILayout.Width(60f)))
                RefreshConfigs();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawOptions()
        {
            EditorGUILayout.LabelField(LC.Combine("Launcher.BindingRules"), EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(LC.Combine("Launcher.BytesFolder"), GUILayout.Width(80f));
            _options.SearchRoot = EditorGUILayout.TextField(_options.SearchRoot);
            if (GUILayout.Button(LC.Combine("Launcher.Browse"), GUILayout.Width(60f)))
            {
                string picked = EditorUtility.OpenFolderPanel(LC.Combine("Launcher.PickBytesFolder"),
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
                LC.Combine("Launcher.WholeAssetsSearch"),
                _options.SearchWholeAssets);

            string[] ruleNames = LC.IsEnglish ? RuleNamesEn : RuleNamesZh;
            _options.Rule = (HotfixLocationRule)EditorGUILayout.Popup(
                LC.Combine("Launcher.LocationRule"), (int)_options.Rule, ruleNames);

            if (_options.Rule == HotfixLocationRule.CustomPrefix)
            {
                _options.CustomPrefix = EditorGUILayout.TextField(
                    LC.Combine("Launcher.CustomPrefix"), _options.CustomPrefix);
            }

            _options.OverwriteExisting = EditorGUILayout.ToggleLeft(
                LC.Combine("Launcher.OverwriteOption"),
                _options.OverwriteExisting);

            EditorGUILayout.HelpBox(LC.Combine("Launcher.AddressRuleHint"),
                MessageType.Info);
        }

        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(LC.Combine("Launcher.ScanPreview"), GUILayout.Height(26f)))
            {
                _options.Save();
                _preview = LauncherHotfixEntryBinder.Build(CurrentConfig, _options, apply: false);
                _status = Describe(_preview, applied: false);
            }

            if (GUILayout.Button(LC.Combine("Launcher.ApplyToConfig"), GUILayout.Height(26f)))
            {
                var config = CurrentConfig;
                if (config == null)
                {
                    EditorUtility.DisplayDialog(DialogTitle,
                        LC.Combine("Launcher.SelectConfig"), LC.Combine("Launcher.OkConfirm"));
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
            EditorGUILayout.LabelField(LC.Combine("Launcher.PreviewHeader"), EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(120f));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(LC.Combine("Launcher.Assembly"), GUILayout.Width(170f));
            EditorGUILayout.LabelField("Location", GUILayout.Width(220f));
            EditorGUILayout.LabelField("Fallback", GUILayout.Width(90f));
            EditorGUILayout.LabelField(LC.Combine("Launcher.Status"));
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
                ? LC.Combine("Launcher.ConfigUpdated")
                : LC.Combine("Launcher.PreviewResult");

            return LC.Format("Launcher.PreviewSummary", head, result.Rows.Count, result.Added, result.LocationBound, result.FallbackBound, result.Missing);
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
                EditorUtility.DisplayDialog(DialogTitle, LC.Combine("Launcher.SelectConfigMenu"),
                    LC.Combine("Launcher.OkConfirm"));
                return;
            }

            var result = LauncherHotfixEntryBinder.Build(config, options, apply: true);
            string message = Describe(result, applied: true);
            Debug.Log("[EF.Launcher] " + message + LC.Format("Launcher.ConfigLabel", config.name));
            EditorUtility.DisplayDialog(DialogTitle, message + LC.Format("Launcher.ConfigLabel2", config.name),
                LC.Combine("Launcher.Ok"));
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
