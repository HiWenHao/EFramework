/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-17 14:02:47
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-17 14:02:47
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using EasyFramework.Edit.Setting;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace SettingPanel
    {
        /// <summary>
        /// 项目设置面板
        /// </summary>
        internal class EFProjectPanel : EFSettingBase
        {
            bool _systemInfoSwitch;
            string _editorUser;
            int _languageByIndex;
            int _rendererPiplines;
            Vector2 _scrollPos;

            private SerializedObject _settingPanel;
            private SerializedProperty _scriptAuthor;
            private SerializedProperty _languageIndex;
            private SerializedProperty _scriptVersion;
            private SerializedProperty _resourcesArea;
            private SerializedProperty _appConstConfig;
            private SerializedProperty _rendererPipline;
            private SerializedProperty _appConstManagerList;

            public EFProjectPanel(string name) : base(name)
            {
            }

            internal override void OnEnable(string assetsPath)
            {
                if (IsInitialzed)
                    return;
                IsInitialzed = true;

                _settingPanel = new SerializedObject(ProjectUtility.Project);
                _scriptAuthor = _settingPanel.FindProperty("_scriptAuthor");
                _languageIndex = _settingPanel.FindProperty("_languageIndex");
                _scriptVersion = _settingPanel.FindProperty("_scriptVersion");
                _resourcesArea = _settingPanel.FindProperty("_resourcesArea");
                _appConstConfig = _settingPanel.FindProperty("_appConst");
                _rendererPipline = _settingPanel.FindProperty("_rendererPipline");
                _appConstManagerList = _appConstConfig.FindPropertyRelative("_managerLevel");

                var type = typeof(UnityEditor.Connect.UnityOAuth).Assembly.GetType("UnityEditor.Connect.UnityConnect");
                MethodInfo methodInfo = type.GetMethod("GetUserInfo");
                PropertyInfo instance = type.GetProperty("instance");
                var userInfo = methodInfo.Invoke(instance.GetValue(null), null);
                var userInfoType = userInfo.GetType();
                PropertyInfo propertyInfo = userInfoType.GetProperty("displayName");
                _editorUser = (string)propertyInfo.GetValue(userInfo);

                _languageByIndex = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
                _languageIndex.intValue = _languageByIndex;
                _rendererPiplines = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "RendererPipline", 0);
                _rendererPipline.intValue = _rendererPiplines;

                FindAllManager();

                _systemInfoSwitch = true;
            }

            internal override void OnGUI()
            {
                SystemInfos();
                using var changeCheckScope = new EditorGUI.ChangeCheckScope();

                EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Current, Lc.Project, Lc.Information }));
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, "Badge");
                _languageByIndex = (int)(ELanguage)EditorGUILayout.EnumPopup(LC.Combine(new Lc[] { Lc.Editor, Lc.Language }), (ELanguage)_languageIndex.intValue);
                if (_languageByIndex != _languageIndex.intValue)
                {
                    _languageIndex.intValue = _languageByIndex;
                    EditorPrefs.SetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", _languageByIndex);
                }

                _rendererPiplines = (int)(RenderingTypeEnum)EditorGUILayout.EnumPopup(LC.Combine(new Lc[] { Lc.Rendering, Lc.Type }), (RenderingTypeEnum)_rendererPipline.intValue);
                if (_rendererPiplines != _rendererPipline.intValue)
                {
                    ChangedRendererPipline();
                }

                EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Editor, Lc.User }), _editorUser);
                _scriptAuthor.stringValue = EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Script, Lc.Author }), _scriptAuthor.stringValue);
                _scriptVersion.stringValue = EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Script, Lc.Version }), _scriptVersion.stringValue);

                EditorGUILayout.PropertyField(_appConstConfig);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Reset, Lc.Project, Lc.Settings })))
                {
                    ResetManagerLevel();

                    string _name = Application.dataPath.Split('/')[^2];
                    _appConstConfig.FindPropertyRelative("_appName").stringValue = _name;
                    _appConstConfig.FindPropertyRelative("_appPrefix").stringValue = _name + "_";
                    _appConstConfig.FindPropertyRelative("_appVersion").stringValue = "1.0";
                    _appConstConfig.FindPropertyRelative("m_UIPath").stringValue = "Prefabs/UI/";
                    _appConstConfig.FindPropertyRelative("m_AudioPath").stringValue = "Sources/";
                }
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Only, Lc.Reset, Lc.Manager, Lc.Level })))
                {
                    ResetManagerLevel();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6);
                EditorGUILayout.PropertyField(_resourcesArea);
                EditorGUILayout.Space(20);

                EditorGUILayout.EndScrollView();


                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

                if (!changeCheckScope.changed) return;
                _settingPanel.ApplyModifiedPropertiesWithoutUndo();
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

            /// <summary>
            /// 改变渲染管线
            /// </summary>
            private void ChangedRendererPipline()
            {
                if (_rendererPiplines == 1)
                {
                    string path = Path.Combine(Application.dataPath[..^7], "Packages/manifest.json");
                    string manifest = File.ReadAllText(path);

                    if (!manifest.Contains("com.unity.render-pipelines.universal"))
                    {
                        if (EditorUtility.DisplayDialog(LC.Combine(Lc.Error), LC.Combine(new Lc[] { Lc.Related, Lc.Resource, Lc.Package, Lc.Lost }), LC.Combine(Lc.Import), LC.Combine(Lc.Cancel)))
                        {
                            EditorApplication.ExecuteMenuItem("Window/Package Manager");
                        }
                        return;
                    }
                }

                _rendererPipline.intValue = _rendererPiplines;
                EditorPrefs.SetInt(ProjectUtility.Project.AppConst.AppPrefix + "RendererPipline", _rendererPiplines);

                string uiPath = Path.Combine(Application.dataPath, ProjectUtility.Path.FrameworkPath[7..], "Scripts/Runtime/Managers/UI/UIManager.Private.cs");
                string[] contentsArray = File.ReadAllLines(uiPath);

                contentsArray[17] = _rendererPiplines == 0 ? contentsArray[17].Insert(0, "//") : contentsArray[17].Replace("//", "");
                for (int i = 60; i < 63; i++)
                {
                    contentsArray[i] = _rendererPiplines == 0 ? contentsArray[i].Insert(16, "//") : contentsArray[i].Replace("//", "");
                }

                File.WriteAllLines(uiPath, contentsArray);

                AssetDatabase.Refresh();
            }

            /// <summary>
            /// 重置管理者级别
            /// </summary>
            private void ResetManagerLevel()
            {
                _appConstManagerList.ClearArray();
                for (int i = 0; i < 10; i++)
                {
                    _appConstManagerList.InsertArrayElementAtIndex(0);
                }
                _appConstManagerList.GetArrayElementAtIndex(0).stringValue = "TimeManager";
                _appConstManagerList.GetArrayElementAtIndex(1).stringValue = "ToolManager";
                _appConstManagerList.GetArrayElementAtIndex(2).stringValue = "EventManager";
                _appConstManagerList.GetArrayElementAtIndex(3).stringValue = "HttpsManager";
                _appConstManagerList.GetArrayElementAtIndex(4).stringValue = "SocketManager";
                _appConstManagerList.GetArrayElementAtIndex(5).stringValue = "FolderManager";
                _appConstManagerList.GetArrayElementAtIndex(6).stringValue = "LoadManager";
                _appConstManagerList.GetArrayElementAtIndex(7).stringValue = "ScenesManager";
                _appConstManagerList.GetArrayElementAtIndex(8).stringValue = "AudioManager";
                _appConstManagerList.GetArrayElementAtIndex(9).stringValue = "UIManager";

            }

            /// <summary>
            /// 查找全部管理器
            /// </summary>
            private void FindAllManager()
            {
                List<string> managerNameList = new List<string>();
                for (int i = 0; i < _appConstManagerList.arraySize; i++)
                {
                    managerNameList.Add(_appConstManagerList.GetArrayElementAtIndex(i).stringValue);
                }

                bool changed = false;
                TypeCache.TypeCollection collection = TypeCache.GetTypesDerivedFrom(typeof(IManager));
                for (int i = 0; i < collection.Count; i++)
                {
                    if (!managerNameList.Contains(collection[i].Name))
                    {
                        changed = true;
                        int _cot = managerNameList.Count;
                        _appConstManagerList.InsertArrayElementAtIndex(_cot);
                        _appConstManagerList.GetArrayElementAtIndex(_cot).stringValue = collection[i].Name;
                    }
                }
                if (changed)
                {
                    _settingPanel.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }
}
