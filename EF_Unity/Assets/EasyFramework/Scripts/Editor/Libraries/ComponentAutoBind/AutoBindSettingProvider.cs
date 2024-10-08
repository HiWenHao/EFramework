/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-13 17:17:49
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-13 17:17:49
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace EasyFramework.Edit.AutoBind
{
    /// <summary>
    /// 依据全局设置进行组件自动绑定
    /// </summary>
    public class AutoBindSettingProvider : SettingsProvider
    {
        /// <summary>
        /// 自动绑定全局设置路径
        /// </summary>
        static string m_AutoBindSettingPath = ProjectUtility.Path.FrameworkPath + "EFAssets/Settings/AutoBindSetting.asset";
        
        /// <summary>
        /// 在设置中的标题名称
        /// </summary>
        private const string m_HeaderName = "EF/Auto Bind Setting";

        private SerializedProperty m_Namespace;
        private SerializedProperty m_RulePrefixes;
        private SerializedObject m_CustomSettings;
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            AutoBindSetting m_Setting = EditorUtils.LoadSettingAtPath<AutoBindSetting>();
            m_CustomSettings =  new SerializedObject(m_Setting);
            m_Namespace = m_CustomSettings.FindProperty("m_Namespace");
            m_RulePrefixes = m_CustomSettings.FindProperty("m_RulePrefixes");
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            m_CustomSettings.Update();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();

            EditorGUILayout.Space();

            m_Namespace.stringValue = EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Default, Lc.Script, Lc.Namespace }), m_Namespace.stringValue);

            EditorGUILayout.Space();

            EditorGUILayout.Space(12f, true);

            //EditorGUILayout.LabelField(LC.Language.SetRulePrefixes);
            EditorGUILayout.PropertyField(m_RulePrefixes);
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            if (!changeCheckScope.changed) return;
            m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
            m_CustomSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 依据全局设置进行组件自动绑定(构造)
        /// </summary>
        public AutoBindSettingProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        /// <summary>
        /// 负责显示在 Project Setting 面板上
        /// </summary>
        [SettingsProvider]
        private static SettingsProvider CreateSettingProvider()
        {
            if (File.Exists(m_AutoBindSettingPath))
            {
                var provider = new AutoBindSettingProvider(m_HeaderName, SettingsScope.Project)
                {
                    keywords = GetSearchKeywordsFromGUIContentProperties<AutoBindSetting>()
                };
                return provider;
            }
            return null;
        }
    }
}
