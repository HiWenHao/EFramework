/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-16 16:39:56
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-23 16:30:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    [EFConfigPanel]
    internal class EFPathConfigPanel : EFSerializedConfigPanelBase<PathConfig>
    {
        // 注：图集 / UI预制件 / UI代码 三个路径已迁至 cn.efefef.ui 的 EFUiBindingConfigPanel
        // Note: atlas / UI-prefab / UI-code paths were moved to EFUiBindingConfigPanel in cn.efefef.ui
        private SerializedProperty _sublimePath;
        private SerializedProperty _notepadPath;
        private SerializedProperty _extractPath;

        public override string Name => LC.Combine("Core.PathConfig");

        public override void LoadWindowData()
        {
            //PathConfig pathConfig = EditorUtils.LoadSettingAtPath<PathConfig>();
            BindConfig(ConfigManager.Path);

            _sublimePath = FindConfigProperty("_sublimePath");
            _notepadPath = FindConfigProperty("_notepadPath");
            _extractPath = FindConfigProperty("_extractPath");
        }

        protected override void OnPanelGUI()
        {
            SerializedConfig.Update();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition, GUIUtils.ScrollViewBackground());

            EditorGUILayout.LabelField(
                $"----- {LC.Combine("Core.InProjectPath")} -----",
                GUIUtils.ColorText(14, textColor: GUIUtils.LightGreen));
            EditorGuiToolkit.SelectionFolderPath(LC.Combine("Core.AnimatExtractPath"), _extractPath);

            EditorGUILayout.LabelField(
                $"----- {LC.Combine("Core.NonProjectPath")} -----",
                GUIUtils.ColorText(14, textColor: GUIUtils.LightRed));
            SelectionEXEPath("Sublime" + LC.Combine("Path"), new[] { "sublime_text" }, _sublimePath);
            SelectionEXEPath("Notepad" + LC.Combine("Path"), new[] { "notepad" }, _notepadPath);

            EditorGUILayout.EndScrollView();

            ApplyConfigIfChanged(changeCheckScope.changed, withoutUndo: true);
        }

        public override void OnDestroy()
        {
            FlushConfig();
        }

        void SelectionEXEPath(string label, string[] containsName, SerializedProperty property)
        {
            EditorGUILayout.LabelField(label, GUIUtils.ColorText(textColor: Color.white));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(property.stringValue);
            if (GUILayout.Button(LC.Combine("SelectPath"), GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, property.stringValue);
                if (!Directory.Exists(folder))
                    folder = Application.dataPath;
                string path =
                    EditorUtility.OpenFilePanel(LC.Combine("SelectPath"), folder, "exe");
                if (!string.IsNullOrEmpty(path))
                {
                    bool exit = false;
                    for (int i = containsName.Length - 1; i >= 0; i--)
                    {
                        if (path.Contains(containsName[i]))
                        {
                            exit = true;
                            continue;
                        }
                    }

                    if (exit)
                    {
                        property.stringValue = path;
                        SaveAndRefresh();
                    }
                    else
                        EditorUtility.DisplayDialog(LC.Combine("Core.PathSelectError"),
                            LC.Combine("Core.PathSelectError"), LC.Combine("Ok"));
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }
    }
}
