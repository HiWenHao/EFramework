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

        string m_EditorUser;
        int m_languageIndex;
        int m_rendererPipline;
        Vector2 m_ComputerInfoScroll;

        private SerializedObject m_SettingPanel;
        private SerializedProperty m_ScriptAuthor;
        private SerializedProperty m_LanguageIndex;
        private SerializedProperty m_ScriptVersion;
        private SerializedProperty m_ResourcesArea;
        private SerializedProperty m_AppConstConfig;
        private SerializedProperty m_RendererPipline;

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            m_SettingPanel = new SerializedObject(ProjectUtility.Project);
            m_ScriptAuthor = m_SettingPanel.FindProperty("m_ScriptAuthor");
            m_LanguageIndex = m_SettingPanel.FindProperty("m_LanguageIndex");
            m_ScriptVersion = m_SettingPanel.FindProperty("m_ScriptVersion");
            m_ResourcesArea = m_SettingPanel.FindProperty("m_ResourcesArea");
            m_AppConstConfig = m_SettingPanel.FindProperty("m_AppConst");
            m_RendererPipline = m_SettingPanel.FindProperty("m_RendererPipline");

            var type = typeof(UnityEditor.Connect.UnityOAuth).Assembly.GetType("UnityEditor.Connect.UnityConnect");
            var m = type.GetMethod("GetUserInfo");
            var instance = type.GetProperty("instance");
            var userInfo = m.Invoke(instance.GetValue(null), null);
            var _type = userInfo.GetType();
            var p = _type.GetProperty("displayName");
            m_EditorUser = (string)p.GetValue(userInfo);

            m_languageIndex = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
            m_LanguageIndex.intValue = m_languageIndex;
            m_rendererPipline = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "RendererPipline", 0);
            m_RendererPipline.intValue = m_rendererPipline;
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            SystemInfos();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            m_languageIndex = (int)(ELanguage)EditorGUILayout.EnumPopup(LC.Combine("Editor", "Language"), (ELanguage)m_LanguageIndex.intValue);
            if (m_languageIndex != m_LanguageIndex.intValue)
            {
                m_LanguageIndex.intValue = m_languageIndex;
                EditorPrefs.SetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", m_languageIndex);
            }

            m_rendererPipline = (int)(RenderingTypeEnum)EditorGUILayout.EnumPopup(LC.Combine("Rendering", "Type"), (RenderingTypeEnum)m_RendererPipline.intValue);
            if (m_rendererPipline != m_RendererPipline.intValue)
            {
                ChangedRendererPipline();
            }

            EditorGUILayout.LabelField(LC.Combine("Editor", "User"), m_EditorUser);
            m_ScriptAuthor.stringValue = EditorGUILayout.TextField(LC.Combine("Script", "Author"), m_ScriptAuthor.stringValue);
            m_ScriptVersion.stringValue = EditorGUILayout.TextField(LC.Combine("Script", "Version"), m_ScriptVersion.stringValue);
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
            GUILayout.Label($"{LC.Combine("Operating", "System")}: {SystemInfo.operatingSystem}");
            GUILayout.Label($"{LC.Combine("System", "Memory", "Size")}: {SystemInfo.systemMemorySize} MB");
            GUILayout.Label($"{LC.Combine("CPU", "Name")}: {SystemInfo.processorType}");
            GUILayout.Label($"{LC.Combine("CPU", "Count")}: {SystemInfo.processorCount}");
            GUILayout.Label("---------------------------------------------------------------------------------------");
            GUILayout.Label($"{LC.Combine("GPU", "Name")}: {SystemInfo.graphicsDeviceName}");
            GUILayout.Label($"{LC.Combine("GPU", "Type")}: {SystemInfo.graphicsDeviceType}");
            GUILayout.Label($"{LC.Combine("GPU", "Memory", "Size")}: {SystemInfo.graphicsMemorySize} MB");
            GUILayout.Label($"{LC.Combine("GPU", "Id")}: {SystemInfo.graphicsDeviceID}");
            GUILayout.Label($"{LC.Combine("GPU", "Vendor")}: {SystemInfo.graphicsDeviceVendor}");
            GUILayout.Label($"{LC.Combine("GPU", "Vendor", "Id")}: {SystemInfo.graphicsDeviceVendorID}");
            GUILayout.Label("---------------------------------------------------------------------------------------");
            GUILayout.Label($"{LC.Combine("Device", "Version")}: {SystemInfo.deviceModel}");
            GUILayout.Label($"{LC.Combine("Device", "Name")}: {SystemInfo.deviceName}");
            GUILayout.Label($"{LC.Combine("Device", "Type")}: {SystemInfo.deviceType}");
            GUILayout.Label($"{LC.Combine("Device", "Id")}: {SystemInfo.deviceUniqueIdentifier}");
            GUILayout.Label("---------------------------------------------------------------------------------------");
            GUILayout.Label($"{LC.Combine("Screen", "Current")} Dpi: {Screen.dpi}");
            GUILayout.Label($"{LC.Combine("Screen", "Resolution")}: {Screen.currentResolution}");
            GUILayout.Label("---------------------------------------------------------------------------------------");
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 改变渲染管线
        /// </summary>
        private void ChangedRendererPipline()
        {
            if (m_rendererPipline == 1)
            {
                string _path = Path.Combine(Application.dataPath[..^7], "Packages/manifest.json");
                string _manifest = File.ReadAllText(_path);

                if (!_manifest.Contains("com.unity.render-pipelines.universal"))
                {
                    if (EditorUtility.DisplayDialog(LC.Combine("Error"), LC.Combine("Related ", "Resource", "Package", "Lost"), LC.Combine("Import"), LC.Combine("Cancel")))
                    {
                        EditorApplication.ExecuteMenuItem("Window/Package Manager");
                    }
                    return;
                }
            }

            m_RendererPipline.intValue = m_rendererPipline;
            EditorPrefs.SetInt(ProjectUtility.Project.AppConst.AppPrefix + "RendererPipline", m_rendererPipline);

            string _uiPath = Path.Combine(Application.dataPath, ProjectUtility.Path.FrameworkPath[7..], "Scripts/Runtime/Managers/UI/UIManager.Private.cs");
            string[] _contentsArray = File.ReadAllLines(_uiPath);

            _contentsArray[18] = m_rendererPipline == 0 ? _contentsArray[18].Insert(0, "//") : _contentsArray[18].Replace("//", "");
            for (int i = 57; i < 60; i++)
            {
                _contentsArray[i] = m_rendererPipline == 0 ? _contentsArray[i].Insert(16, "//") : _contentsArray[i].Replace("//", "");
            }

            File.WriteAllLines(_uiPath, _contentsArray);
        }
    }
}