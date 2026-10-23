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
    internal class EFPathConfigPanel : EFConfigPanelBase
    {
        Vector2 _scrollPos;

        GUIStyle _uiStyle;

        // 注：图集 / UI预制件 / UI代码 三个路径已迁至 cn.efefef.ui 的 EFUiBindingConfigPanel
        // Note: atlas / UI-prefab / UI-code paths were moved to EFUiBindingConfigPanel in cn.efefef.ui
        private SerializedProperty _sublimePath;
        private SerializedProperty _notepadPath;
        private SerializedProperty _extractPath;
        private SerializedObject _customSettings;

        public override string Name => LC.Combine(new Lc[] { Lc.Path, Lc.Config });
        
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

            //PathConfig pathConfig = EditorUtils.LoadSettingAtPath<PathConfig>();
            _customSettings = new SerializedObject(ConfigManager.Path);

            _sublimePath = _customSettings.FindProperty("_sublimePath");
            _notepadPath = _customSettings.FindProperty("_notepadPath");
            _extractPath = _customSettings.FindProperty("_extractPath");
        }

        public override void OnGUI()
        {
            _customSettings.Update();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUIUtils.ScrollViewBackground());


            EditorGUILayout.LabelField(
                $"----- {LC.Combine(new Lc[] { Lc.In, Lc.Project, Lc.Path, Lc.Under })} -----",
                SetUIStyle(GUIUtils.LightGreen, 14));
            SelectionFolderPath(LC.Combine(new Lc[] { Lc.Animat, Lc.Extract, Lc.Path }), _extractPath);

            EditorGUILayout.LabelField(
                $"----- {LC.Combine(new Lc[] { Lc.Non, Lc.Project, Lc.Path, Lc.Under })} -----",
                SetUIStyle(GUIUtils.LightRed, 14));
            SelectionEXEPath("Sublime" + LC.Combine(Lc.Path), new string[] { "sublime_text" }, _sublimePath);
            SelectionEXEPath("Notepad" + LC.Combine(Lc.Path), new string[] { "notepad" }, _notepadPath);


            EditorGUILayout.EndScrollView();

            if (!changeCheckScope.changed) return;
            _customSettings.ApplyModifiedPropertiesWithoutUndo();
            _customSettings.ApplyModifiedProperties();
        }

        public override void OnDestroy()
        {
            SaveAssetsInfo();
        }

        GUIStyle SetUIStyle(Color color, int fontSize = 12)
        {
            _uiStyle.normal.textColor = color;
            _uiStyle.fontSize = fontSize;
            return _uiStyle;
        }

        void SelectionEXEPath(string label, string[] containsName, SerializedProperty property)
        {
            EditorGUILayout.LabelField(label, SetUIStyle(Color.white));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(property.stringValue);
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Path, Lc.Select }), GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, property.stringValue);
                if (!Directory.Exists(folder))
                    folder = Application.dataPath;
                string path =
                    EditorUtility.OpenFilePanel(LC.Combine(new Lc[] { Lc.Path, Lc.Select }), folder, "exe");
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
                        SaveAssetsInfo();
                    }
                    else
                        EditorUtility.DisplayDialog(LC.Combine(new Lc[] { Lc.Path, Lc.Select, Lc.Error }),
                            LC.Combine(new Lc[] { Lc.Path, Lc.Select, Lc.Error }), LC.Combine(Lc.Ok));
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }

        void SelectionFolderPath(string label, SerializedProperty property)
        {
            EditorGUILayout.LabelField(label, SetUIStyle(Color.white));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.DelayedTextField(property.stringValue);
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Path, Lc.Select }), GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, property.stringValue);
                if (!Directory.Exists(folder))
                {
                    folder = Application.dataPath;
                }

                string path =
                    EditorUtility.OpenFolderPanel(LC.Combine(new Lc[] { Lc.Path, Lc.Select }), folder, "");
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

        void SaveAssetsInfo()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
