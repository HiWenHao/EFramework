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
        private SerializedProperty m_ScriptAuthor;
        private SerializedProperty m_LanguageIndex;
        private SerializedProperty m_ScriptVersion;
        private SerializedProperty m_ResourcesArea;
        private SerializedProperty m_AppConstConfig;

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            m_SettingPanel = new SerializedObject(ProjectUtility.Project);
            m_ScriptAuthor = m_SettingPanel.FindProperty("m_ScriptAuthor");
            m_LanguageIndex = m_SettingPanel.FindProperty("m_LanguageIndex");
            m_ScriptVersion = m_SettingPanel.FindProperty("m_ScriptVersion");
            m_ResourcesArea = m_SettingPanel.FindProperty("m_ResourcesArea");
            m_AppConstConfig = m_SettingPanel.FindProperty("m_AppConst");

        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            m_LanguageIndex.intValue = (int)(ELanguage)EditorGUILayout.EnumPopup(LC.Language.EditorLanguage, (ELanguage)m_LanguageIndex.intValue);
            m_ScriptAuthor.stringValue = EditorGUILayout.TextField(LC.Language.ScriptAuthor, m_ScriptAuthor.stringValue);
            m_ScriptVersion.stringValue = EditorGUILayout.TextField(LC.Language.ScriptVersion, m_ScriptVersion.stringValue);
            EditorGUILayout.PropertyField(m_AppConstConfig);
            EditorGUILayout.PropertyField(m_ResourcesArea);
            EditorGUILayout.Space(20);
            if (!changeCheckScope.changed) return;
            m_SettingPanel.ApplyModifiedPropertiesWithoutUndo();
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
