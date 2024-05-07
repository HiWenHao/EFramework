/* 
 * ================================================
 * Describe:      This script is used to  . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-14 11:49:37
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-05-07 15:50:37
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

        int m_languageIndex;
        string m_EditorUser;
        Vector2 m_ComputerInfoScroll;

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

            var type = typeof(UnityEditor.Connect.UnityOAuth).Assembly.GetType("UnityEditor.Connect.UnityConnect");
            var m = type.GetMethod("GetUserInfo");
            var instance = type.GetProperty("instance");
            var userInfo = m.Invoke(instance.GetValue(null), null);
            var _type = userInfo.GetType();
            var p = _type.GetProperty("displayName");
            m_EditorUser = (string)p.GetValue(userInfo);

            m_languageIndex = PlayerPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
            m_LanguageIndex.intValue = m_languageIndex;
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            SystemInfos();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            m_languageIndex = (int)(ELanguage)EditorGUILayout.EnumPopup(LC.Language.EditorLanguage, (ELanguage)m_LanguageIndex.intValue);
            if (m_languageIndex != m_LanguageIndex.intValue)
            {
                m_LanguageIndex.intValue = m_languageIndex;
                PlayerPrefs.SetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", m_languageIndex);
            }
            EditorGUILayout.LabelField(LC.Language.EditorUser, m_EditorUser);
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

        /// <summary>
        /// 系统信息
        /// </summary>
        private void SystemInfos()
        {
            m_ComputerInfoScroll = EditorGUILayout.BeginScrollView(m_ComputerInfoScroll, "Box", GUILayout.Height(210));

            GUILayout.Label("---------------------------------------------------------------------------------------");
            GUILayout.Label(LC.Language.OperatingSystem + SystemInfo.operatingSystem);
            GUILayout.Label(LC.Language.SystemMemorySize + SystemInfo.systemMemorySize + "MB");
            GUILayout.Label(LC.Language.ProcessorType + SystemInfo.processorType);
            GUILayout.Label(LC.Language.ProcessorType + SystemInfo.processorCount);
            GUILayout.Label("---------------------------------------------------------------------------------------");
            GUILayout.Label(LC.Language.GraphicsDeviceName + SystemInfo.graphicsDeviceName);
            GUILayout.Label(LC.Language.GraphicsDeviceType + SystemInfo.graphicsDeviceType);
            GUILayout.Label(LC.Language.GraphicsMemorySize + SystemInfo.graphicsMemorySize + "MB");
            GUILayout.Label(LC.Language.GraphicsDeviceID + SystemInfo.graphicsDeviceID);
            GUILayout.Label(LC.Language.GraphicsDeviceVendor + SystemInfo.graphicsDeviceVendor);
            GUILayout.Label(LC.Language.GraphicsDeviceVendorID + SystemInfo.graphicsDeviceVendorID);
            GUILayout.Label("---------------------------------------------------------------------------------------");
            GUILayout.Label(LC.Language.DeviceModel + SystemInfo.deviceModel);
            GUILayout.Label(LC.Language.DeviceName + SystemInfo.deviceName);
            GUILayout.Label(LC.Language.DeviceType + SystemInfo.deviceType);
            GUILayout.Label(LC.Language.DeviceUniqueIdentifier + SystemInfo.deviceUniqueIdentifier);
            GUILayout.Label("---------------------------------------------------------------------------------------");
            GUILayout.Label(LC.Language.ScreenDpi + Screen.dpi);
            GUILayout.Label(LC.Language.ScreenCurrentResolution + Screen.currentResolution.ToString());
            GUILayout.Label("---------------------------------------------------------------------------------------");
            EditorGUILayout.EndScrollView();
        }
    }
}
