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
        private const string m_EFProjectSettingPath = "Assets/EasyFramework/Resources/Settings/ProjectSetting.asset";
        private SerializedObject m_CustomSettings;

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            m_CustomSettings = new SerializedObject(ProjectSettingsUtils.EFProjectSettings);
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_FrameworkGlobalSettings"));
            EditorGUILayout.Space(20);
            if (!changeCheckScope.changed) return;
            m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
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
