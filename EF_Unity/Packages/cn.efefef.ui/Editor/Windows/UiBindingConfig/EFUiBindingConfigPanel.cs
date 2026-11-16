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
    internal class EFUiBindingConfigPanel : EFSerializedConfigPanelBase<UiBindingConfig>
    {
        private const string DefaultFileName = "UiBindingConfig";
        private const string FileExtension = "json";

        private SerializedProperty _rulePrefixes;
        private SerializedObject _customSettings => SerializedConfig;

        // —— 以下为自 EFPathConfigPanel 迁入的「工程内 UI 路径」配置（数据现与规则前缀同存于 UiBindingConfig 资产）——
        // Moved from EFPathConfigPanel: in-project UI paths (now stored in the UiBindingConfig asset alongside rule prefixes)
        private SerializedProperty _atlasFolder;
        private SerializedProperty _uiPrefabPath;
        private SerializedProperty _uiCodePath;

        public override string Name => "UI " + LC.Combine("Ui.DataConfig");

        public override void LoadWindowData()
        {
            BindConfig(UiBindingConfig.Instance);
            _atlasFolder = FindConfigProperty("_atlasFolder");
            _uiPrefabPath = FindConfigProperty("_uiPrefabPath");
            _uiCodePath = FindConfigProperty("_uiCodePath");
            _rulePrefixes = FindConfigProperty("_rulePrefixes");
        }

        protected override void OnPanelGUI()
        {
            SerializedConfig.Update();

            using var changeCheckScope = new EditorGUI.ChangeCheckScope();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine("Ui.ImportConfig")))
            {
                ImportConfig();
            }

            if (GUILayout.Button(LC.Combine("Ui.ExportConfig")))
            {
                ExportConfig();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGuiToolkit.SelectionFolderPath(LC.Combine("Ui.AtlasSavePath"), _atlasFolder);
            EditorGuiToolkit.SelectionFolderPath(
                LC.Combine("Default") + "UI" + LC.Combine("Ui.PrefabSavePath"),
                _uiPrefabPath);
            EditorGuiToolkit.SelectionFolderPath(LC.Combine("Default") + "UI" + LC.Combine("Ui.CodeSavePath"),
                _uiCodePath);

            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);
            EditorGUILayout.PropertyField(_rulePrefixes);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            ApplyConfigIfChanged(changeCheckScope.changed);
        }


        #region Import / Export

        /// <summary>
        /// 从 JSON 文件导入配置（规则前缀 + 工程内 UI 路径）
        /// <para>Import config (rule prefixes + in-project UI paths) from a JSON file</para>
        /// </summary>
        private void ImportConfig()
        {
            string path = EditorUtility.OpenFilePanel(LC.Combine("Ui.ImportConfig"),
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
                        LC.Combine("Ui.ImportConfig"),
                        LC.Combine("Ui.ImportDataIsEmpty"),
                        LC.Combine("Ok"));
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
                    LC.Combine("Ui.ImportConfig"),
                    $"{LC.Combine("Ui.ImportElementTotalIs")}:  {(hasItems ? wrapper.Items.Length : 0)}\n" +
                    $"{LC.Combine("Ui.ImportPathTotalIs")}:  {pathCount}",
                    LC.Combine("Ok"));
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    LC.Combine("Ui.ImportConfigFailed"),
                    e.Message,
                    LC.Combine("Ok"));
            }
        }

        /// <summary>
        /// 导出配置（规则前缀 + 工程内 UI 路径）到 JSON 文件
        /// <para>Export config (rule prefixes + in-project UI paths) to a JSON file</para>
        /// </summary>
        private void ExportConfig()
        {
            string path = EditorUtility.SaveFilePanel(
                LC.Combine("Ui.ExportConfig"),
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
                    LC.Combine("Ui.ExportConfig"),
                    $"{LC.Combine("Ui.ExportElementTotalIs")}:  {wrapper.Items.Length}\n" +
                    $"{LC.Combine("Ui.ExportPathTotalIs")}:  {CountPaths(wrapper)}",
                    LC.Combine("Ok"));
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    LC.Combine("Ui.ExportConfigFailed"),
                    e.Message,
                    LC.Combine("Ok"));
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