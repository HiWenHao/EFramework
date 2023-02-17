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
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 依据全局设置进行组件自动绑定
    /// </summary>
    public class AutoBindSettingProvider : SettingsProvider
    {
        /// <summary>
        /// 自动绑定全局设置路径
        /// </summary>
        const string m_AutoBindSettingPath = "Assets/EasyFramework/Resources/Settings/AutoBindSetting.asset";
        
        /// <summary>
        /// 在设置中的标题名称
        /// </summary>
        private const string m_HeaderName = "EF/AutoBindSetting";

        private SerializedProperty m_PrefabPath;
        private SerializedProperty m_ComCodePath;
        private SerializedProperty m_RulePrefixes;
        private SerializedObject m_CustomSettings;
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            m_CustomSettings = GetSerializedSettings();
            m_PrefabPath = m_CustomSettings.FindProperty("m_PrefabPath");
            m_ComCodePath = m_CustomSettings.FindProperty("m_ComCodePath");
            m_RulePrefixes = m_CustomSettings.FindProperty("m_RulePrefixes");
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            m_CustomSettings.Update();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.PropertyField(m_CustomSettings.FindProperty("m_Namespace"));

            EditorGUILayout.Space(12f, true);

            EditorGUILayout.LabelField("默认组件代码保存路径：");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_ComCodePath.stringValue);
            if (GUILayout.Button("选择组件代码路径", GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, m_ComCodePath.stringValue);
                if (!Directory.Exists(folder))
                {
                    folder = Application.dataPath;
                }
                string path = EditorUtility.OpenFolderPanel("选择组件代码保存路径", folder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    m_ComCodePath.stringValue = path.Replace(Application.dataPath + "/", "");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("默认UI预制件的保存路径：");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(m_PrefabPath.stringValue);
            if (GUILayout.Button("选择UI预制件保存路径", GUILayout.Width(140f)))
            {
                string folder = Path.Combine(Application.dataPath, m_PrefabPath.stringValue);
                if (!Directory.Exists(folder))
                {
                    folder = Application.dataPath;
                }
                string path = EditorUtility.OpenFolderPanel("选择UI预制件保存路径", folder, "");
                if (!string.IsNullOrEmpty(path))
                {
                    m_PrefabPath.stringValue = path.Replace(Application.dataPath + "/", "");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12f, true);

            EditorGUILayout.LabelField("组件规则设置");
            EditorGUILayout.LabelField("RulePrefixes");
            EditorGUILayout.PropertyField(m_RulePrefixes);
            EditorGUILayout.Space(20);
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

        /// <summary>
        /// 获取序列化设置
        /// </summary>
        private static SerializedObject GetSerializedSettings()
        {
            var m_Setting = AutoBindSetting.GetAutoBindSetting();
            return new SerializedObject(m_Setting);
        }
    }
}
