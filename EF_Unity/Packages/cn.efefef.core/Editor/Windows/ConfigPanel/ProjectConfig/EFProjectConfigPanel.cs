/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-17 14:02:47
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 16:47:08
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// 项目设置面板
    /// </summary>
    [EFConfigPanel(Priority = 0)]
    internal class EFProjectConfigPanel : EFConfigPanelBase
    {
        bool _systemInfoSwitch;
        string _editorUser;
        Vector2 _scrollPos;

        private SerializedObject _settingPanel;
        private SerializedProperty _scriptAuthor;
        private SerializedProperty _scriptVersion;
        private SerializedProperty _resourcesArea;
        private SerializedProperty _appConstConfig;
        private SerializedProperty _scriptNamespace;

        public override string Name => LC.Combine(Lc.Project, Lc.Settings);

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }

        public override void LoadWindowData()
        {
            _settingPanel = new SerializedObject(ConfigManager.Project);
            _appConstConfig = _settingPanel.FindProperty("_appConst");
            _scriptAuthor = _settingPanel.FindProperty("_scriptAuthor");
            _scriptVersion = _settingPanel.FindProperty("_scriptVersion");
            _resourcesArea = _settingPanel.FindProperty("_resourcesArea");
            _scriptNamespace = _settingPanel.FindProperty("_scriptNamespace");

            var type = typeof(UnityEditor.Connect.UnityOAuth).Assembly.GetType("UnityEditor.Connect.UnityConnect");
            MethodInfo methodInfo = type.GetMethod("GetUserInfo");
            PropertyInfo instance = type.GetProperty("instance");
            var userInfo = methodInfo.Invoke(instance.GetValue(null), null);
            var userInfoType = userInfo.GetType();
            PropertyInfo propertyInfo = userInfoType.GetProperty("displayName");
            _editorUser = (string)propertyInfo.GetValue(userInfo);
            EditorPrefs.SetString($"{ConfigManager.Project.AppConst.AppPrefix}EditorUser", _editorUser);

            _systemInfoSwitch = true;
        }

        public override void OnGUI()
        {
            SystemInfos();
            LC.DisplayLanguage = (ELanguage)EditorGUILayout.EnumPopup(LC.Combine(Lc.Framework, Lc.Display, Lc.Language),
                LC.DisplayLanguage);

            using var changeCheckScope = new EditorGUI.ChangeCheckScope();

            EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Current, Lc.Project, Lc.Information }));

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUIUtils.ScrollViewBackground());

            EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Editor, Lc.User }), _editorUser);
            _scriptAuthor.stringValue = EditorGUILayout.TextField(LC.Combine(Lc.Script, Lc.Author),
                _scriptAuthor.stringValue);
            _scriptVersion.stringValue = EditorGUILayout.TextField(LC.Combine(Lc.Script, Lc.Version),
                _scriptVersion.stringValue);
            _scriptNamespace.stringValue = EditorGUILayout.TextField(LC.Combine(Lc.Script, Lc.Namespace),
                _scriptNamespace.stringValue);

            EditorGUILayout.PropertyField(_appConstConfig);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Reset, Lc.Project, Lc.Settings })))
            {
                string appName = Application.productName;
                _appConstConfig.FindPropertyRelative("_appName").stringValue = appName;
                _appConstConfig.FindPropertyRelative("_appPrefix").stringValue = appName + "_";
                _appConstConfig.FindPropertyRelative("_appVersion").stringValue = "1.0";
                _appConstConfig.FindPropertyRelative("m_UIPath").stringValue = "Prefabs/UI/";
                _appConstConfig.FindPropertyRelative("m_AudioPath").stringValue = "Sources/";
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_resourcesArea);
            EditorGUILayout.Space(20);

            EditorGUILayout.EndScrollView();


            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            if (!changeCheckScope.changed) return;
            _settingPanel.ApplyModifiedPropertiesWithoutUndo();
            _settingPanel.ApplyModifiedProperties();
        }

        /// <summary>
        /// 系统信息
        /// </summary>
        private void SystemInfos()
        {
            _systemInfoSwitch = EditorGUILayout.BeginFoldoutHeaderGroup(_systemInfoSwitch,
                LC.Combine(new Lc[] { Lc.Current, Lc.Hardware, Lc.Information }));
            if (_systemInfoSwitch)
            {
                string contents =
                    "--------------------------------------------------------------------------------\n" +
                    $"{LC.Combine(new Lc[] { Lc.Operating, Lc.System })}: {SystemInfo.operatingSystem}\n" +
                    $"{LC.Combine(new Lc[] { Lc.System, Lc.Memory, Lc.Size })}: {SystemInfo.systemMemorySize} MB\n" +
                    $"{LC.Combine(new Lc[] { Lc.CPU, Lc.Name })}: {SystemInfo.processorType}\n" +
                    $"{LC.Combine(new Lc[] { Lc.CPU, Lc.Count })}: {SystemInfo.processorCount}\n" +
                    "--------------------------------------------------------------------------------\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Name })}: {SystemInfo.graphicsDeviceName}\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Type })}: {SystemInfo.graphicsDeviceType}\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Memory, Lc.Size })}: {SystemInfo.graphicsMemorySize} MB\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Id })}: {SystemInfo.graphicsDeviceID}\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Vendor })}: {SystemInfo.graphicsDeviceVendor}\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Vendor, Lc.Id })}: {SystemInfo.graphicsDeviceVendorID}\n" +
                    "--------------------------------------------------------------------------------\n" +
                    $"{LC.Combine(new Lc[] { Lc.Device, Lc.Version })}: {SystemInfo.deviceModel}\n" +
                    $"{LC.Combine(new Lc[] { Lc.Device, Lc.Name })}: {SystemInfo.deviceName}\n" +
                    $"{LC.Combine(new Lc[] { Lc.Device, Lc.Type })}: {SystemInfo.deviceType}\n" +
                    $"{LC.Combine(new Lc[] { Lc.Device, Lc.Id })}: {SystemInfo.deviceUniqueIdentifier}\n" +
                    "--------------------------------------------------------------------------------\n" +
                    $"{LC.Combine(new Lc[] { Lc.Screen, Lc.Current })} Dpi: {Screen.dpi}\n" +
                    $"{LC.Combine(new Lc[] { Lc.Screen, Lc.Resolution })}: {Screen.currentResolution}\n" +
                    "--------------------------------------------------------------------------------";

                GUILayout.Box(contents, "FrameBox", GUILayout.MinWidth(360f));
                //GUILayout.Box(GUIContent.none, GUILayout.Height(4.0f), GUILayout.ExpandWidth(true));
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }
    }
}