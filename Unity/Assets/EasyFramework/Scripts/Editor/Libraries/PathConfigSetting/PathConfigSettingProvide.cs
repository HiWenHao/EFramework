/* 
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-20 19:31:17
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-20 19:31:17
 * ScriptVersion: 0.1
 * ===============================================
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyFramework.Edit.PathConfig
{
    /// <summary>
    /// 工程优化
    /// </summary>
    public class PathConfigSettingProvide : SettingsProvider
    {
        private const string m_HeaderName = "EF/Path Config Setting";
        private static readonly string EFOptimalSettingPath = ProjectUtility.Path.FrameworkPath + "Resources/Settings/PathConfigSetting.asset";

        private SerializedProperty m_FrameworkPath;
        private SerializedProperty m_SublimePath;
        private SerializedProperty m_NotepadPath;
        private SerializedProperty m_AtlasFolder;
        private SerializedProperty m_ExtractPath;
        private SerializedProperty m_UICodePath;
        private SerializedProperty m_UIPrefabPath;
        private SerializedObject m_CustomSettings;

        GUIStyle UIStyle;
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            UIStyle = new GUIStyle()
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

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            m_CustomSettings.Update();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.Space(20);

            EditorGUILayout.LabelField($"--------------- {LC.Language.UnderProjectPath} ---------------", SetUIStyle(new Color(0.3f, 0.8f, 0.3f), 14));
            SelectionFolderPath(LC.Language.FrameworkPath, LC.Language.PathSelect, m_FrameworkPath);
            SelectionFolderPath(LC.Language.AtlasSavePath, LC.Language.PathSelect, m_AtlasFolder);
            SelectionFolderPath(LC.Language.DefaultUIPrefabSavePath, LC.Language.PathSelect, m_UIPrefabPath);
            SelectionFolderPath(LC.Language.DefaultUICodeSavePath, LC.Language.PathSelect, m_UICodePath);
            SelectionFolderPath(LC.Language.AnimatorExtractPath, LC.Language.PathSelect, m_ExtractPath);

            EditorGUILayout.LabelField($"--------------- {LC.Language.NonProjectPath} ---------------", SetUIStyle(new Color(0.9f, 0.4f, 0.4f), 14));
            SelectionEXEPath(LC.Language.SublimePath, LC.Language.PathSelect, new string[] { "sublime_text", "subl" }, m_SublimePath);
            SelectionEXEPath(LC.Language.NotepadPath, LC.Language.PathSelect, new string[] { "notepad" }, m_NotepadPath);

            if (!changeCheckScope.changed) return;
            m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
            m_CustomSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void SelectionEXEPath(string label, string btnLabel, string[] containsName, SerializedProperty property)
        {
            EditorGUILayout.LabelField(label, SetUIStyle(Color.white));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(property.stringValue);
            if (GUILayout.Button(btnLabel, GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, property.stringValue);
                if (!Directory.Exists(folder))
                    folder = Application.dataPath;
                string path = EditorUtility.OpenFilePanel(btnLabel, folder, "exe");
                if (!string.IsNullOrEmpty(path))
                {
                    bool _exit = false;
                    for (int i = containsName.Length - 1; i >= 0; i--)
                    {
                        if (path.Contains(containsName[i]))
                        {
                            _exit = true;
                            continue;
                        }
                    }
                    if (_exit)
                        property.stringValue = path;
                    else
                        EditorUtility.DisplayDialog(LC.Language.PathSelecteError, LC.Language.PathSelecteErrorContent, LC.Language.Ok);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }

        void SelectionFolderPath(string label, string btnLabel, SerializedProperty property)
        {
            EditorGUILayout.LabelField(label, SetUIStyle(Color.white));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.DelayedTextField(property.stringValue);
            if (GUILayout.Button(btnLabel, GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, property.stringValue);
                if (!Directory.Exists(folder))
                {
                    folder = Application.dataPath;
                }
                string path = EditorUtility.OpenFolderPanel(btnLabel, folder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.Equals(Application.dataPath))
                        property.stringValue = "Assets/";
                    else
                        property.stringValue = "Assets" + path.Replace(Application.dataPath, "") + "/";
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }

        GUIStyle SetUIStyle(Color color, int fontSize = 12)
        {
            UIStyle.normal.textColor = color;
            UIStyle.fontSize = fontSize;
            return UIStyle;
        }

        /// <summary>
        /// 项目设置面板 (构造)
        /// </summary>
        public PathConfigSettingProvide(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        /// <summary>
        /// 用来在 Project Setting 面板上显示
        /// </summary>
        /// <returns></returns>
        [SettingsProvider]
        private static SettingsProvider CreateSettingProvider()
        {
            if (File.Exists(EFOptimalSettingPath))
            {
                var provider = new PathConfigSettingProvide(m_HeaderName, SettingsScope.Project)
                {
                    keywords = GetSearchKeywordsFromGUIContentProperties<PathConfigSetting>()
                };
                return provider;
            }
            return null;
        }
    }
}
