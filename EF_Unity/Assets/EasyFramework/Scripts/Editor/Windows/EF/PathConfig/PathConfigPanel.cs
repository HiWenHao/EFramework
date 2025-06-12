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
            Vector2 m_ScrollPos;

            GUIStyle m_UIStyle;

            private SerializedProperty m_FrameworkPath;
            private SerializedProperty m_SublimePath;
            private SerializedProperty m_NotepadPath;
            private SerializedProperty m_AtlasFolder;
            private SerializedProperty m_ExtractPath;
            private SerializedProperty m_UICodePath;
            private SerializedProperty m_UIPrefabPath;
            private SerializedObject m_CustomSettings;

            public PathConfigPanel(string name) : base(name)
            {
            }

            internal override void OnEnable(string assetsPath)
            {
                if (m_IsInitialzed)
                    return;
                m_IsInitialzed = true;
                m_UIStyle = new GUIStyle()
                {
                    fontSize = 14,
                    normal =
                    {
                        textColor = Color.white,
                    }
                };

                PathConfigSetting _pathConfig = EditorUtils.LoadSettingAtPath<PathConfigSetting>();
                m_CustomSettings = new SerializedObject(_pathConfig);

                m_FrameworkPath = m_CustomSettings.FindProperty("m_FrameworkPath");
                m_SublimePath = m_CustomSettings.FindProperty("m_SublimePath");
                m_NotepadPath = m_CustomSettings.FindProperty("m_NotepadPath");
                m_AtlasFolder = m_CustomSettings.FindProperty("m_AtlasFolder");
                m_ExtractPath = m_CustomSettings.FindProperty("m_ExtractPath");
                m_UICodePath = m_CustomSettings.FindProperty("m_UICodePath");
                m_UIPrefabPath = m_CustomSettings.FindProperty("m_UIPrefabPath");
            }

            internal override void OnGUI()
            {
                m_CustomSettings.Update();
                using var changeCheckScope = new EditorGUI.ChangeCheckScope();
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos); //"Badge"


                EditorGUILayout.LabelField($"----- {LC.Combine(new Lc[] { Lc.In, Lc.Project, Lc.Path, Lc.Under })} -----", SetUIStyle(new Color(0.3f, 0.8f, 0.3f), 14));
                SelectionFolderPath(LC.Combine(new Lc[] { Lc.Framework, Lc.Path }), m_FrameworkPath);
                SelectionFolderPath(LC.Combine(new Lc[] { Lc.Atlas, Lc.Save, Lc.Path }), m_AtlasFolder);
                SelectionFolderPath(LC.Combine(Lc.Default) + "UI" + LC.Combine(new Lc[] { Lc.Prefab, Lc.Save, Lc.Path }), m_UIPrefabPath);
                SelectionFolderPath(LC.Combine(Lc.Default) + "UI" + LC.Combine(new Lc[] { Lc.Code, Lc.Save, Lc.Path }), m_UICodePath);
                SelectionFolderPath(LC.Combine(new Lc[] { Lc.Animat, Lc.Extract, Lc.Path }), m_ExtractPath);

                EditorGUILayout.LabelField($"----- {LC.Combine(new Lc[] { Lc.Non, Lc.Project, Lc.Path, Lc.Under })} -----", SetUIStyle(new Color(0.9f, 0.4f, 0.4f), 14));
                SelectionEXEPath("Sublime" + LC.Combine(Lc.Path), new string[] { "sublime_text" }, m_SublimePath);
                SelectionEXEPath("Notepad" + LC.Combine(Lc.Path), new string[] { "notepad" }, m_NotepadPath);


                EditorGUILayout.EndScrollView();

                if (!changeCheckScope.changed) return;
                m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
                m_CustomSettings.ApplyModifiedProperties();
            }

            internal override void OnDestroy()
            {
                SaveAssetsInfo();
            }

            GUIStyle SetUIStyle(Color color, int fontSize = 12)
            {
                m_UIStyle.normal.textColor = color;
                m_UIStyle.fontSize = fontSize;
                return m_UIStyle;
            }

            void SelectionEXEPath(string label, string[] containsName, SerializedProperty property)
            {
                EditorGUILayout.LabelField(label, SetUIStyle(Color.white));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(property.stringValue);
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Path, Lc.Select }), GUILayout.Width(140f)))
                {
                    string _folder = Path.Combine(Application.dataPath, property.stringValue);
                    if (!Directory.Exists(_folder))
                        _folder = Application.dataPath;
                    string _path = EditorUtility.OpenFilePanel(LC.Combine(new Lc[] { Lc.Path, Lc.Select }), _folder, "exe");
                    if (!string.IsNullOrEmpty(_path))
                    {
                        bool _exit = false;
                        for (int i = containsName.Length - 1; i >= 0; i--)
                        {
                            if (_path.Contains(containsName[i]))
                            {
                                _exit = true;
                                continue;
                            }
                        }
                        if (_exit)
                        {
                            property.stringValue = _path; 
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
                    string _folder = Path.Combine(Application.dataPath, property.stringValue);
                    if (!Directory.Exists(_folder))
                    {
                        _folder = Application.dataPath;
                    }
                    string _path = EditorUtility.OpenFolderPanel(LC.Combine(new Lc[] { Lc.Path, Lc.Select }), _folder, "");
                    if (!string.IsNullOrEmpty(_path))
                    {
                        if (_path.Equals(Application.dataPath))
                            property.stringValue = "Assets/";
                        else
                            property.stringValue = "Assets" + _path.Replace(Application.dataPath, "") + "/";
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
