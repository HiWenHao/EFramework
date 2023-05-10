/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-14 11:49:37
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-14 11:49:37
 * ScriptVersion: 0.1
 * ===============================================
*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyFramework.Edit.Setting
{
    /// <summary>
    /// 项目设置面板
    /// </summary>
    public class ProjectSettingProvide : SettingsProvider
    {
        private const string m_HeaderName = "EF/Project Setting";
        private static readonly string m_EFProjectSettingPath = ProjectUtility.Path.FrameworkPath + "Resources/Settings/ProjectSetting.asset";

        private SerializedObject m_SettingPanel;
        private SerializedObject m_ResourcesArea;
        private SerializedProperty m_ScriptAuthor;
        private SerializedProperty m_ScriptVersion;

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            m_SettingPanel = new SerializedObject(EditorUtils.LoadSettingAtPath<ProjectSetting>());
            m_ResourcesArea = new SerializedObject(ProjectUtility.Project);
            m_ScriptAuthor = m_SettingPanel.FindProperty("m_ScriptAuthor");
            m_ScriptVersion = m_SettingPanel.FindProperty("m_ScriptVersion");

        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            GUILayout.Label(" Framework Setting 框架设置",new GUIStyle()
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = Color.gray,
                }
            });
            EditorGUILayout.LabelField("脚本作者名");
            m_ScriptAuthor.stringValue = EditorGUILayout.TextField("Script author name", m_ScriptAuthor.stringValue);
            EditorGUILayout.LabelField("脚本版本");
            m_ScriptVersion.stringValue = EditorGUILayout.TextField("Script version", m_ScriptVersion.stringValue);
            EditorGUILayout.PropertyField(m_ResourcesArea.FindProperty("m_ResourcesArea"));
            EditorGUILayout.Space(20);
            if (!changeCheckScope.changed) return;
            m_SettingPanel.ApplyModifiedPropertiesWithoutUndo();
            m_ResourcesArea.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 项目设置面板 (构造)
        /// </summary>
        public ProjectSettingProvide(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        /// <summary>
        /// 用来在 Project Setting 面板上显示
        /// </summary>
        /// <returns></returns>
        [SettingsProvider]
        private static SettingsProvider CreateSettingProvider()
        {
            if (File.Exists(m_EFProjectSettingPath))
            {
                var provider = new ProjectSettingProvide(m_HeaderName, SettingsScope.Project)
                {
                    keywords = GetSearchKeywordsFromGUIContentProperties<ProjectSetting>()
                };
                return provider;
            }
            return null;
        }
    }
}
