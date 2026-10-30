/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2024-10-17 11:31:01
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-23 17:20:00
 * ScriptVersion:   0.4
 * ===============================================
 */

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// UI自动绑定设置（含工程内 UI 路径配置）
    /// <para>UI auto-binding settings (also hosts in-project UI path settings)</para>
    /// </summary>
    [EFConfigPanel]
    internal class EFUiBindingConfigPanel : EFConfigPanelBase
    {
        private const string DefaultFileName = "UiBindingConfig";
        private const string FileExtension = "json";

        private Vector2 _scrollPos;

        private SerializedProperty _rulePrefixes;
        private SerializedObject _customSettings;

        // —— 以下为自 EFPathConfigPanel 迁入的「工程内 UI 路径」配置（数据现与规则前缀同存于 UiBindingConfig 资产）——
        // Moved from EFPathConfigPanel: in-project UI paths (now stored in the UiBindingConfig asset alongside rule prefixes)
        private SerializedProperty _atlasFolder;
        private SerializedProperty _uiPrefabPath;
        private SerializedProperty _uiCodePath;
        private GUIStyle _uiStyle;

        public override string Name => "UI " + LC.Combine(Lc.Data, Lc.Config);

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }

        public override void LoadWindowData()
        {
            _uiStyle = new GUIStyle()
            {
                fontSize = 14,
                normal =
                {
                    textColor = Color.white,
                }
            };

            _customSettings = new SerializedObject(UiBindingConfig.Instance);
            _atlasFolder = _customSettings.FindProperty("_atlasFolder");
            _uiPrefabPath = _customSettings.FindProperty("_uiPrefabPath");
            _uiCodePath = _customSettings.FindProperty("_uiCodePath");
            _rulePrefixes = _customSettings.FindProperty("_rulePrefixes");
        }

        protected override void OnPanelGUI()
        {
            _customSettings.Update();

            using var changeCheckScope = new EditorGUI.ChangeCheckScope();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(Lc.Import, Lc.Config)))
            {
                ImportConfig();
            }

            if (GUILayout.Button(LC.Combine(Lc.Export, Lc.Config)))
            {
                ExportConfig();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            SelectionFolderPath(LC.Combine(Lc.Atlas, Lc.Save, Lc.Path), _atlasFolder);
            SelectionFolderPath(
                LC.Combine(Lc.Default) + "UI" + LC.Combine(Lc.Prefab, Lc.Save, Lc.Path),
                _uiPrefabPath);
            SelectionFolderPath(LC.Combine(Lc.Default) + "UI" + LC.Combine(Lc.Code, Lc.Save, Lc.Path),
                _uiCodePath);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.PropertyField(_rulePrefixes);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            if (!changeCheckScope.changed) return;

            _customSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        #region UI Paths

        /// <summary>
        /// 绘制目录选择行：标签 + 当前路径 + 选择按钮
        /// <para>Draw a folder-selection row: label + current path + browse button</para>
        /// </summary>
        void SelectionFolderPath(string label, SerializedProperty property)
        {
            EditorGUILayout.LabelField(label, SetUIStyle(Color.white));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.DelayedTextField(property.stringValue);
            if (GUILayout.Button(LC.Combine(Lc.Path, Lc.Select), GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, property.stringValue);
                if (!Directory.Exists(folder))
                {
                    folder = Application.dataPath;
                }

                string path =
                    EditorUtility.OpenFolderPanel(LC.Combine(Lc.Path, Lc.Select), folder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.Equals(Application.dataPath))
                        property.stringValue = "Assets/";
                    else
                        property.stringValue = "Assets" + path.Replace(Application.dataPath, "") + "/";
                    SaveAssetsInfo();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }

        /// <summary>
        /// 获取（复用）面板文本样式
        /// <para>Get (reuse) the panel text style</para>
        /// </summary>
        GUIStyle SetUIStyle(Color color, int fontSize = 12)
        {
            _uiStyle.normal.textColor = color;
            _uiStyle.fontSize = fontSize;
            return _uiStyle;
        }

        /// <summary>
        /// 保存并刷新资源数据库
        /// <para>Save assets and refresh the AssetDatabase</para>
        /// </summary>
        void SaveAssetsInfo()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #endregion

        #region Import / Export

        /// <summary>
        /// 从 JSON 文件导入配置（规则前缀 + 工程内 UI 路径）
        /// <para>Import config (rule prefixes + in-project UI paths) from a JSON file</para>
        /// </summary>
        private void ImportConfig()
        {
            string path = EditorUtility.OpenFilePanel(LC.Combine(Lc.Import, Lc.Config),
                Application.dataPath, FileExtension);

            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var wrapper = JsonUtility.FromJson<UiBindingConfigWrapper>(json);

                // 旧版导出文件只有 Items、没有路径字段：按字段是否存在分别处理，缺失的字段不覆盖现有值
                // Legacy exports only carry Items: apply each part only when present, never overwrite missing fields
                bool hasItems = wrapper?.Items != null && wrapper.Items.Length > 0;
                int pathCount = CountPaths(wrapper);
                bool hasPaths = pathCount > 0;

                if (!hasItems && !hasPaths)
                {
                    EditorUtility.DisplayDialog(
                        LC.Combine(Lc.Import, Lc.Config),
                        LC.Combine(Lc.Import, Lc.Data, Lc.Is, Lc.Empty),
                        LC.Combine(Lc.Ok));
                    return;
                }

                // 路径与规则前缀同存于 UiBindingConfig：先经 SerializedObject 写路径，再直接改规则列表，
                // 避免 SerializedObject 缓存把直接修改的规则列表回写覆盖
                // Paths and rules now share UiBindingConfig: write paths via SerializedObject first,
                // then modify the rule list directly, so the SO cache never overwrites the direct change
                Undo.RecordObject(UiBindingConfig.Instance, "Import Ui Binding Config");

                if (hasPaths)
                {
                    _customSettings.Update();
                    if (!string.IsNullOrEmpty(wrapper.AtlasFolder))
                        _atlasFolder.stringValue = wrapper.AtlasFolder;
                    if (!string.IsNullOrEmpty(wrapper.UiPrefabPath))
                        _uiPrefabPath.stringValue = wrapper.UiPrefabPath;
                    if (!string.IsNullOrEmpty(wrapper.UiCodePath))
                        _uiCodePath.stringValue = wrapper.UiCodePath;
                    _customSettings.ApplyModifiedProperties();
                }

                if (hasItems)
                {
                    UiBindingConfig.Instance.RulePrefixes.Clear();
                    UiBindingConfig.Instance.RulePrefixes.AddRange(wrapper.Items);
                }

                EditorUtility.SetDirty(UiBindingConfig.Instance);
                AssetDatabase.SaveAssets();

                _customSettings.Update();

                EditorUtility.DisplayDialog(
                    LC.Combine(Lc.Import, Lc.Config),
                    $"{LC.Combine(Lc.Import, Lc.Element, Lc.Total, Lc.Is)}:  {(hasItems ? wrapper.Items.Length : 0)}\n" +
                    $"{LC.Combine(Lc.Import, Lc.Path, Lc.Total, Lc.Is)}:  {pathCount}",
                    LC.Combine(Lc.Ok));
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    LC.Combine(Lc.Import, Lc.Config, Lc.Failed),
                    e.Message,
                    LC.Combine(Lc.Ok));
            }
        }

        /// <summary>
        /// 导出配置（规则前缀 + 工程内 UI 路径）到 JSON 文件
        /// <para>Export config (rule prefixes + in-project UI paths) to a JSON file</para>
        /// </summary>
        private void ExportConfig()
        {
            string path = EditorUtility.SaveFilePanel(
                LC.Combine(Lc.Export, Lc.Config),
                Application.dataPath,
                DefaultFileName,
                FileExtension);

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var wrapper = new UiBindingConfigWrapper
                {
                    Items = UiBindingConfig.Instance.RulePrefixes.ToArray(),
                    AtlasFolder = UiBindingConfig.Instance.AtlasFolder,
                    UiPrefabPath = UiBindingConfig.Instance.UIPrefabPath,
                    UiCodePath = UiBindingConfig.Instance.UICodePath,
                };

                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(path, json, Encoding.UTF8);
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    LC.Combine(Lc.Export, Lc.Config),
                    $"{LC.Combine(Lc.Export, Lc.Element, Lc.Total, Lc.Is)}:  {wrapper.Items.Length}\n" +
                    $"{LC.Combine(Lc.Export, Lc.Path, Lc.Total, Lc.Is)}:  {CountPaths(wrapper)}",
                    LC.Combine(Lc.Ok));
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    LC.Combine(Lc.Export, Lc.Config, Lc.Failed),
                    e.Message,
                    LC.Combine(Lc.Ok));
            }
        }

        /// <summary>
        /// 统计包装器中非空路径字段的数量
        /// <para>Count the non-empty path fields in the wrapper</para>
        /// </summary>
        private static int CountPaths(UiBindingConfigWrapper wrapper)
        {
            if (wrapper == null) return 0;

            int count = 0;
            if (!string.IsNullOrEmpty(wrapper.AtlasFolder)) count++;
            if (!string.IsNullOrEmpty(wrapper.UiPrefabPath)) count++;
            if (!string.IsNullOrEmpty(wrapper.UiCodePath)) count++;
            return count;
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// JSON 序列化包装器 —— List&lt;T&gt; 不能直接被 JsonUtility 序列化，通过数组桥接；同时携带工程内 UI 路径
        /// <para>JSON serialization wrapper — bridges List&lt;T&gt; via an array; also carries in-project UI paths</para>
        /// </summary>
        [Serializable]
        private class UiBindingConfigWrapper
        {
            /// <summary> 组件的缩略名字映射（字段名保持 Items 不变，兼容旧版导出文件） </summary>
            public RulePrefixes[] Items;

            /// <summary> 图集保存路径（旧版文件无此字段，导入时缺省不覆盖） </summary>
            public string AtlasFolder;

            /// <summary> UI 预制件保存路径（旧版文件无此字段，导入时缺省不覆盖） </summary>
            public string UiPrefabPath;

            /// <summary> UI 代码保存路径（旧版文件无此字段，导入时缺省不覆盖） </summary>
            public string UiCodePath;
        }

        #endregion
    }
}