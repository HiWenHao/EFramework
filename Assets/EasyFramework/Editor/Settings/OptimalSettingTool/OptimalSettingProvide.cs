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

namespace EasyFramework.Edit.Optimal
{
    /// <summary>
    /// 工程优化
    /// </summary>
    public class OptimalSettingProvide : SettingsProvider
    {
        private const string m_HeaderName = "EF/Optimal Setting";
        private static readonly string EFOptimalSettingPath = ProjectSettingsUtils.projectSetting.FrameworkPath + "/Resources/Settings/OptimalSetting.asset";

        private SerializedProperty m_SublimePath;
        private SerializedProperty m_NotepadPath;
        private SerializedProperty m_AtlasFolder;
        private SerializedProperty m_ExtractPath;
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

            OptimalSetting _optimal = EditorUtils.LoadSettingAtPath<OptimalSetting>();
            m_CustomSettings = new SerializedObject(_optimal);

            m_SublimePath = m_CustomSettings.FindProperty("m_SublimePath");
            m_NotepadPath = m_CustomSettings.FindProperty("m_NotepadPath");
            m_AtlasFolder = m_CustomSettings.FindProperty("m_AtlasFolder");
            m_ExtractPath = m_CustomSettings.FindProperty("m_ExtractPath");
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            m_CustomSettings.Update();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.Space(20);

            SelectionEXEPath("Sublime文件路径：", "选择Sublime路径", new string[] { "sublime_text", "subl" }, m_SublimePath);
            SelectionEXEPath("Notepad++文件路径：", "选择Notepad++路径", new string[] { "notepad" }, m_NotepadPath);
            SelectionFolderPath("图集保存路径：", "选择图集保存路径", m_AtlasFolder);
            SelectionFolderPath("提取压缩后的动画保存路径：", "选择动画保存路径", m_ExtractPath);

            if (!changeCheckScope.changed) return;
            m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
            m_CustomSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void SelectionEXEPath(string label, string btnLabel, string[] containsName, SerializedProperty property)
        {
            EditorGUILayout.LabelField(label, UIStyle);
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
                        EditorUtility.DisplayDialog("路径错误", "Please configure the correct path to Sublime\n请配置正确的路径", "ok");
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }

        void SelectionFolderPath(string label, string btnLabel, SerializedProperty property)
        {
            EditorGUILayout.LabelField(label, UIStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(property.stringValue);
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

        /// <summary>
        /// 项目设置面板 (构造)
        /// </summary>
        public OptimalSettingProvide(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
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
                var provider = new OptimalSettingProvide(m_HeaderName, SettingsScope.Project)
                {
                    keywords = GetSearchKeywordsFromGUIContentProperties<OptimalSetting>()
                };
                return provider;
            }
            return null;
        }
    }
}
