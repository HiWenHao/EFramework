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

        private SerializedProperty m_NotepadPath;
        private SerializedProperty m_SublimePath;
        private SerializedProperty m_AtlasFolder;
        private SerializedProperty m_ExtractPath;
        private SerializedObject m_CustomSettings;

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
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

            EditorGUILayout.LabelField("Sublime文件路径：");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_SublimePath.stringValue);
            if (GUILayout.Button("选择Sublime路径", GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, m_SublimePath.stringValue);
                if (!Directory.Exists(folder))
                    folder = Application.dataPath;
                string path = EditorUtility.OpenFilePanel("选择Sublime路径", folder, "exe");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.Contains("sublime_text") || path.Contains("subl"))
                        m_SublimePath.stringValue = path;
                    else
                        EditorUtility.DisplayDialog("路径错误", "Please configure the correct path to Sublime\n请配置正确的Sublime路径", "ok");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Notepad++文件路径：");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_NotepadPath.stringValue);
            if (GUILayout.Button("选择Notepad++路径", GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, m_NotepadPath.stringValue);
                if (!Directory.Exists(folder))
                    folder = Application.dataPath;
                string path = EditorUtility.OpenFilePanel("选择Notepad++路径", folder, "exe");
                if (!string.IsNullOrEmpty(path))
                {
                    if (!path.Contains("notepad"))
                        EditorUtility.DisplayDialog("路径错误", "Please configure the correct path to Notepad++\n请配置正确的Notepad++路径", "ok");
                    else
                        m_NotepadPath.stringValue = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("图集保存路径：");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_AtlasFolder.stringValue);
            if (GUILayout.Button("选择图集保存路径", GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, m_AtlasFolder.stringValue);
                if (!Directory.Exists(folder))
                {
                    folder = Application.dataPath;
                }
                string path = EditorUtility.OpenFolderPanel("选择图集保存路径", folder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.Equals(Application.dataPath))
                        m_AtlasFolder.stringValue = "Assets/";
                    else
                        m_AtlasFolder.stringValue = "Assets" + path.Replace(Application.dataPath, "") + "/";
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("提取压缩后的动画保存路径：");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_ExtractPath.stringValue);
            if (GUILayout.Button("选择动画保存路径", GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath,  m_ExtractPath.stringValue);
                if (!Directory.Exists(folder))
                {
                    folder = Application.dataPath;
                }
                string path = EditorUtility.OpenFolderPanel("选择动画保存路径", folder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.Equals(Application.dataPath))
                        m_ExtractPath.stringValue = "Assets/";
                    else
                        m_ExtractPath.stringValue = "Assets" + path.Replace(Application.dataPath, "") + "/";
                }
            }
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(20);
            if (!changeCheckScope.changed) return;
            m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
            m_CustomSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
