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
using UnityEngine.UIElements;

namespace EasyFramework.Edit.Optimal
{
    /// <summary>
    /// 工程优化
    /// </summary>
    public class OptimalSettingProvide : SettingsProvider
    {
        private const string m_HeaderName = "EF/Optimal Setting";
        private const string EFOptimalSettingPath = "Assets/EasyFramework/Resources/Settings/OptimalSetting.asset";
        private SerializedObject m_CustomSettings;

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            m_CustomSettings = new SerializedObject(OptimalSettingUtils.EFOptimalSettingss);
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_FrameworkOptimalSettings"));
            EditorGUILayout.Space(20);
            if (!changeCheckScope.changed) return;
            m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
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
