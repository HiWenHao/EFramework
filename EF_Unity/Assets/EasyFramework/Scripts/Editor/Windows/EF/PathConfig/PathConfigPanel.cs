/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-16 16:39:56
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-16 16:39:56
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.Edit;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace SettingPanel
    {
        /// <summary>
        /// Please modify the description。
        /// </summary>
        internal class PathConfigPanel : EFSettingBase
        {
            Vector2 _scrollPos;

            GUIStyle _uiStyle;

            private SerializedProperty _frameworkPath;
            private SerializedProperty _sublimePath;
            private SerializedProperty _notepadPath;
            private SerializedProperty _atlasFolder;
            private SerializedProperty _extractPath;
            private SerializedProperty _uiCodePath;
            private SerializedProperty _uiPrefabPath;
            private SerializedObject _customSettings;

            public PathConfigPanel(string name) : base(name)
            {
            }

            internal override void OnEnable(string assetsPath)
            {
                if (IsInitialzed)
                    return;
                IsInitialzed = true;
                _uiStyle = new GUIStyle()
                {
                    fontSize = 14,
                    normal =
                    {
                        textColor = Color.white,
                    }
                };

                PathConfigSetting pathConfig = EditorUtils.LoadSettingAtPath<PathConfigSetting>();
                _customSettings = new SerializedObject(pathConfig);

                _frameworkPath = _customSettings.FindProperty("_frameworkPath");
                _sublimePath = _customSettings.FindProperty("_sublimePath");
                _notepadPath = _customSettings.FindProperty("_notepadPath");
                _atlasFolder = _customSettings.FindProperty("_atlasFolder");
                _extractPath = _customSettings.FindProperty("_extractPath");
                _uiCodePath = _customSettings.FindProperty("_uiCodePath");
                _uiPrefabPath = _customSettings.FindProperty("_uiPrefabPath");
            }

            internal override void OnGUI()
            {
                _customSettings.Update();
                using var changeCheckScope = new EditorGUI.ChangeCheckScope();
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos); //"Badge"


                EditorGUILayout.LabelField($"----- {LC.Combine(new Lc[] { Lc.In, Lc.Project, Lc.Path, Lc.Under })} -----", SetUIStyle(new Color(0.3f, 0.8f, 0.3f), 14));
                SelectionFolderPath(LC.Combine(new Lc[] { Lc.Framework, Lc.Path }), _frameworkPath);
                SelectionFolderPath(LC.Combine(new Lc[] { Lc.Atlas, Lc.Save, Lc.Path }), _atlasFolder);
                SelectionFolderPath(LC.Combine(Lc.Default) + "UI" + LC.Combine(new Lc[] { Lc.Prefab, Lc.Save, Lc.Path }), _uiPrefabPath);
                SelectionFolderPath(LC.Combine(Lc.Default) + "UI" + LC.Combine(new Lc[] { Lc.Code, Lc.Save, Lc.Path }), _uiCodePath);
                SelectionFolderPath(LC.Combine(new Lc[] { Lc.Animat, Lc.Extract, Lc.Path }), _extractPath);

                EditorGUILayout.LabelField($"----- {LC.Combine(new Lc[] { Lc.Non, Lc.Project, Lc.Path, Lc.Under })} -----", SetUIStyle(new Color(0.9f, 0.4f, 0.4f), 14));
                SelectionEXEPath("Sublime" + LC.Combine(Lc.Path), new string[] { "sublime_text" }, _sublimePath);
                SelectionEXEPath("Notepad" + LC.Combine(Lc.Path), new string[] { "notepad" }, _notepadPath);


                EditorGUILayout.EndScrollView();

                if (!changeCheckScope.changed) return;
                _customSettings.ApplyModifiedPropertiesWithoutUndo();
                _customSettings.ApplyModifiedProperties();
            }

            internal override void OnDestroy()
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
                    string path = EditorUtility.OpenFilePanel(LC.Combine(new Lc[] { Lc.Path, Lc.Select }), folder, "exe");
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
                            EditorUtility.DisplayDialog(LC.Combine(new Lc[] { Lc.Path, Lc.Select, Lc.Error }), LC.Combine(new Lc[] { Lc.Path, Lc.Select, Lc.Error }), LC.Combine(Lc.Ok));
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
                    string path = EditorUtility.OpenFolderPanel(LC.Combine(new Lc[] { Lc.Path, Lc.Select }), folder, "");
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
}
