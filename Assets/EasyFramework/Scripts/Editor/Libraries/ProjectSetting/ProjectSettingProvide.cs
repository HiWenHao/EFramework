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
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            SystemInfo();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            m_LanguageIndex.intValue = (int)(ELanguage)EditorGUILayout.EnumPopup(LC.Language.EditorLanguage, (ELanguage)m_LanguageIndex.intValue);
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
        private void SystemInfo()
        {
            GUI.enabled = true;

            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(GUILayout.Height(2)), default);

            m_ComputerInfoScroll = GUILayout.BeginScrollView(m_ComputerInfoScroll);

            GUILayout.BeginVertical("Box");

            GUILayout.Label("操作系统：" + UnityEngine.SystemInfo.operatingSystem);
            GUILayout.Label("系统内存：" + UnityEngine.SystemInfo.systemMemorySize + "MB");
            GUILayout.Label("处理器：" + UnityEngine.SystemInfo.processorType);
            GUILayout.Label("处理器数量：" + UnityEngine.SystemInfo.processorCount);
            GUILayout.Space(14);
            GUILayout.Label("显卡：" + UnityEngine.SystemInfo.graphicsDeviceName);
            GUILayout.Label("显卡类型：" + UnityEngine.SystemInfo.graphicsDeviceType);
            GUILayout.Label("显存：" + UnityEngine.SystemInfo.graphicsMemorySize + "MB");
            GUILayout.Label("显卡标识：" + UnityEngine.SystemInfo.graphicsDeviceID);
            GUILayout.Label("显卡供应商：" + UnityEngine.SystemInfo.graphicsDeviceVendor);
            GUILayout.Label("显卡供应商标识码：" + UnityEngine.SystemInfo.graphicsDeviceVendorID);
            GUILayout.Space(14);
            GUILayout.Label("设备模式：" + UnityEngine.SystemInfo.deviceModel);
            GUILayout.Label("设备名称：" + UnityEngine.SystemInfo.deviceName);
            GUILayout.Label("设备类型：" + UnityEngine.SystemInfo.deviceType);
            GUILayout.Label("设备标识：" + UnityEngine.SystemInfo.deviceUniqueIdentifier);

            GUILayout.Label("DPI：" + Screen.dpi);
            GUILayout.Label("分辨率：" + Screen.currentResolution.ToString());
            GUILayout.EndVertical();
            GUILayout.EndScrollView();


            GUILayout.FlexibleSpace();

        }
    }
}
