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
    internal class EFProjectConfigPanel : EFSerializedConfigPanelBase<ProjectConfig>
    {
        bool _systemInfoSwitch;
        string _editorUser;

        private SerializedProperty _scriptAuthor;
        private SerializedProperty _scriptVersion;
        private SerializedProperty _resourcesArea;
        private SerializedProperty _appConstConfig;
        private SerializedProperty _scriptNamespace;

        public override string Name => LC.Combine("Core.ProjectSettings");

        public override void OnEnable(string assetsPath)
        {
            LC.ReloadLanguages();
            base.OnEnable(assetsPath);
        }

        public override void LoadWindowData()
        {
            BindConfig(ConfigManager.Project);
            _appConstConfig = FindConfigProperty("_appConst");
            _scriptAuthor = FindConfigProperty("_scriptAuthor");
            _scriptVersion = FindConfigProperty("_scriptVersion");
            _resourcesArea = FindConfigProperty("_resourcesArea");
            _scriptNamespace = FindConfigProperty("_scriptNamespace");

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

        protected override void OnPanelGUI()
        {
            SystemInfos();
            DrawLanguageSelector();

            using var changeCheckScope = new EditorGUI.ChangeCheckScope();

            EditorGUILayout.LabelField(LC.Combine("Core.CurrentProjectInfo"));

            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition, GUIUtils.ScrollViewBackground());

            EditorGUILayout.LabelField(LC.Combine("Core.EditorUser"), _editorUser);
            _scriptAuthor.stringValue = EditorGUILayout.TextField(LC.Combine("Core.ScriptAuthor"),
                _scriptAuthor.stringValue);
            _scriptVersion.stringValue = EditorGUILayout.TextField(LC.Combine("Core.ScriptVersion"),
                _scriptVersion.stringValue);
            _scriptNamespace.stringValue = EditorGUILayout.TextField(LC.Combine("ScriptNamespace"),
                _scriptNamespace.stringValue);

            EditorGUILayout.PropertyField(_appConstConfig);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine("Core.ResetProjectSettings")))
            {
                string appName = Application.productName;
                _appConstConfig.FindPropertyRelative("_appPrefix").stringValue = appName + "_";
                _appConstConfig.FindPropertyRelative("_appVersion").stringValue = "1.0";
                _appConstConfig.FindPropertyRelative("_uiPath").stringValue = "Prefabs/UI/";
                _appConstConfig.FindPropertyRelative("_audioPath").stringValue = "Sources/";
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_resourcesArea);
            EditorGUILayout.Space(20);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            ApplyConfigIfChanged(changeCheckScope.changed, withoutUndo: true);
        }

        /// <summary>
        /// 绘制编辑器语言选择行：下拉 + 新增语言 + 刷新，附当前语言摘要（复合语句）
        /// <para>Draw the editor language row: dropdown + add + refresh, with a composite-sentence summary.</para>
        /// </summary>
        private void DrawLanguageSelector()
        {
            var options = LC.GetLanguageOptions();
            var displayNames = new string[options.Count];
            for (int i = 0; i < options.Count; i++)
                displayNames[i] = options[i].DisplayName;

            EditorGUILayout.BeginHorizontal();
            int previousIndex = LC.GetSelectedIndex();
            int selectedIndex = EditorGUILayout.Popup(
                LC.Combine("Core.DisplayLanguage"), previousIndex, displayNames);
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(22f)))
                EFEditorLanguageCreateWindow.OpenWindow();
            if (GUILayout.Button("↻", EditorStyles.miniButton, GUILayout.Width(22f)))
                LC.ReloadLanguages();
            EditorGUILayout.EndHorizontal();

            if (selectedIndex != previousIndex)
                LC.EditorLanguageCode = options[selectedIndex].Code;
        }

        /// <summary>
        /// 系统信息
        /// </summary>
        private void SystemInfos()
        {
            _systemInfoSwitch = EditorGUILayout.BeginFoldoutHeaderGroup(_systemInfoSwitch,
                LC.Combine("Core.CurrentHardwareInfo"));
            if (_systemInfoSwitch)
            {
                string contents =
                    "--------------------------------------------------------------------------------\n" +
                    $"{LC.Combine("Core.OperatingSystem")}: {SystemInfo.operatingSystem}\n" +
                    $"{LC.Combine("Core.SystemMemorySize")}: {SystemInfo.systemMemorySize} MB\n" +
                    $"{LC.Combine("Core.CpuName")}: {SystemInfo.processorType}\n" +
                    $"{LC.Combine("Core.CpuCount")}: {SystemInfo.processorCount}\n" +
                    "--------------------------------------------------------------------------------\n" +
                    $"{LC.Combine("Core.GpuName")}: {SystemInfo.graphicsDeviceName}\n" +
                    $"{LC.Combine("Core.GpuType")}: {SystemInfo.graphicsDeviceType}\n" +
                    $"{LC.Combine("Core.GpuMemorySize")}: {SystemInfo.graphicsMemorySize} MB\n" +
                    $"{LC.Combine("Core.GpuId")}: {SystemInfo.graphicsDeviceID}\n" +
                    $"{LC.Combine("Core.GpuVendor")}: {SystemInfo.graphicsDeviceVendor}\n" +
                    $"{LC.Combine("Core.GpuVendorId")}: {SystemInfo.graphicsDeviceVendorID}\n" +
                    "--------------------------------------------------------------------------------\n" +
                    $"{LC.Combine("Core.DeviceVersion")}: {SystemInfo.deviceModel}\n" +
                    $"{LC.Combine("Core.DeviceName")}: {SystemInfo.deviceName}\n" +
                    $"{LC.Combine("Core.DeviceType")}: {SystemInfo.deviceType}\n" +
                    $"{LC.Combine("Core.DeviceId")}: {SystemInfo.deviceUniqueIdentifier}\n" +
                    "--------------------------------------------------------------------------------\n" +
                    $"{LC.Combine("Core.ScreenCurrent")} Dpi: {Screen.dpi}\n" +
                    $"{LC.Combine("Core.ScreenResolution")}: {Screen.currentResolution}\n" +
                    "--------------------------------------------------------------------------------";

                GUILayout.Box(contents, "FrameBox", GUILayout.MinWidth(360f));
                //GUILayout.Box(GUIContent.none, GUILayout.Height(4.0f), GUILayout.ExpandWidth(true));
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
        }
    }
}