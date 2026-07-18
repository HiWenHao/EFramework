/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2024-10-17 11:31:01
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-06-24 20:54:00
 * ScriptVersion:   0.2
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
    /// UI自动绑定设置
    /// </summary>
    [EFConfigPanel]
    internal class EFUiBindingConfigPanel : EFConfigPanelBase
    {
        private const string DefaultFileName = "UiBindingConfig";
        private const string FileExtension = "json";

        private Vector2 _scrollPos;

        private SerializedProperty _rulePrefixes;
        private SerializedObject _customSettings;

        public override string Name => LC.Combine(Lc.Code, Lc.Auto, Lc.Bind);

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }

        public override void LoadWindowData()
        {
            _customSettings = new SerializedObject(UiBindingConfig.Instance);
            _rulePrefixes = _customSettings.FindProperty("_rulePrefixes");
        }

        public override void OnGUI()
        {
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(Lc.Import, Lc.Config)))
            {
                ImportRulePrefixes();
            }

            if (GUILayout.Button(LC.Combine(Lc.Export, Lc.Config)))
            {
                ExportRulePrefixes();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12f, true);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.PropertyField(_rulePrefixes);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            if (!changeCheckScope.changed) return;

            _customSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        #region Import / Export

        /// <summary>
        /// 从 JSON 文件导入 _rulePrefixes 配置
        /// </summary>
        private void ImportRulePrefixes()
        {
            string path = EditorUtility.OpenFilePanel(LC.Combine(Lc.Import, Lc.Config),
                Application.dataPath, FileExtension);

            if (string.IsNullOrEmpty(path)) return;

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var wrapper = JsonUtility.FromJson<RulePrefixesWrapper>(json);

                if (wrapper?.Items == null || wrapper.Items.Length == 0)
                {
                    EditorUtility.DisplayDialog(
                        LC.Combine(Lc.Import, Lc.Config),
                        LC.Combine(Lc.Import, Lc.Data, Lc.Is, Lc.Empty),
                        LC.Combine(Lc.Ok));
                    return;
                }

                Undo.RecordObject(UiBindingConfig.Instance, "Import Rule Prefixes");

                UiBindingConfig.Instance.RulePrefixes.Clear();
                UiBindingConfig.Instance.RulePrefixes.AddRange(wrapper.Items);

                EditorUtility.SetDirty(UiBindingConfig.Instance);
                AssetDatabase.SaveAssets();

                _customSettings.Update();

                EditorUtility.DisplayDialog(
                    LC.Combine(Lc.Import, Lc.Config),
                    $"{LC.Combine(Lc.Import, Lc.Element, Lc.Total, Lc.Is)}:  {wrapper.Items.Length}",
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
        /// 导出 _rulePrefixes 配置到 JSON 文件
        /// </summary>
        private void ExportRulePrefixes()
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
                var wrapper = new RulePrefixesWrapper
                {
                    Items = UiBindingConfig.Instance.RulePrefixes.ToArray()
                };

                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(path, json, Encoding.UTF8);
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    LC.Combine(Lc.Export, Lc.Config),
                    $"{LC.Combine(Lc.Export, Lc.Element, Lc.Total, Lc.Is)}:  {wrapper.Items.Length}",
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

        #endregion

        #region Nested Types

        /// <summary>
        /// JSON 序列化包装器 —— List&lt;T&gt; 不能直接被 JsonUtility 序列化，通过数组桥接
        /// </summary>
        [Serializable]
        private class RulePrefixesWrapper
        {
            public RulePrefixes[] Items;
        }

        #endregion
    }
}