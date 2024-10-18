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
            bool m_SystemInfoSwitch;
            string m_EditorUser;
            int m_languageIndex;
            int m_rendererPipline;
            Vector2 m_ScrollPos;
            Vector2 m_ComputerInfoScroll;

            private SerializedObject m_SettingPanel;
            private SerializedProperty m_ScriptAuthor;
            private SerializedProperty m_LanguageIndex;
            private SerializedProperty m_ScriptVersion;
            private SerializedProperty m_ResourcesArea;
            private SerializedProperty m_AppConstConfig;
            private SerializedProperty m_RendererPipline;
            private SerializedProperty m_AppConstManagerList;

            public EFProjectPanel(string name) : base(name)
            {
            }

            internal override void OnEnable(string assetsPath)
            {
                if (m_IsInitialzed)
                    return;
                m_IsInitialzed = true;

                m_SettingPanel = new SerializedObject(ProjectUtility.Project);
                m_ScriptAuthor = m_SettingPanel.FindProperty("m_ScriptAuthor");
                m_LanguageIndex = m_SettingPanel.FindProperty("m_LanguageIndex");
                m_ScriptVersion = m_SettingPanel.FindProperty("m_ScriptVersion");
                m_ResourcesArea = m_SettingPanel.FindProperty("m_ResourcesArea");
                m_AppConstConfig = m_SettingPanel.FindProperty("m_AppConst");
                m_RendererPipline = m_SettingPanel.FindProperty("m_RendererPipline");
                m_AppConstManagerList = m_AppConstConfig.FindPropertyRelative("m_ManagerLevel");

                var _Type = typeof(UnityEditor.Connect.UnityOAuth).Assembly.GetType("UnityEditor.Connect.UnityConnect");
                MethodInfo m = _Type.GetMethod("GetUserInfo");
                PropertyInfo instance = _Type.GetProperty("instance");
                var _userInfo = m.Invoke(instance.GetValue(null), null);
                var _type = _userInfo.GetType();
                PropertyInfo _p = _type.GetProperty("displayName");
                m_EditorUser = (string)_p.GetValue(_userInfo);

                m_languageIndex = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", 0);
                m_LanguageIndex.intValue = m_languageIndex;
                m_rendererPipline = EditorPrefs.GetInt(ProjectUtility.Project.AppConst.AppPrefix + "RendererPipline", 0);
                m_RendererPipline.intValue = m_rendererPipline;

                FindAllManager();

                m_SystemInfoSwitch = true;
            }

            internal override void OnGUI()
            {
                SystemInfos();
                using var changeCheckScope = new EditorGUI.ChangeCheckScope();

                EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Current, Lc.Project, Lc.Information }));
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, "Badge");
                m_languageIndex = (int)(ELanguage)EditorGUILayout.EnumPopup(LC.Combine(new Lc[] { Lc.Editor, Lc.Language }), (ELanguage)m_LanguageIndex.intValue);
                if (m_languageIndex != m_LanguageIndex.intValue)
                {
                    m_LanguageIndex.intValue = m_languageIndex;
                    EditorPrefs.SetInt(ProjectUtility.Project.AppConst.AppPrefix + "LanguageIndex", m_languageIndex);
                }

                m_rendererPipline = (int)(RenderingTypeEnum)EditorGUILayout.EnumPopup(LC.Combine(new Lc[] { Lc.Rendering, Lc.Type }), (RenderingTypeEnum)m_RendererPipline.intValue);
                if (m_rendererPipline != m_RendererPipline.intValue)
                {
                    ChangedRendererPipline();
                }

                EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Editor, Lc.User }), m_EditorUser);
                m_ScriptAuthor.stringValue = EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Script, Lc.Author }), m_ScriptAuthor.stringValue);
                m_ScriptVersion.stringValue = EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Script, Lc.Version }), m_ScriptVersion.stringValue);

                EditorGUILayout.PropertyField(m_AppConstConfig);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Reset, Lc.Project, Lc.Settings })))
                {
                    ResetManagerLevel();

                    string _name = Application.dataPath.Split('/')[^2];
                    m_AppConstConfig.FindPropertyRelative("m_AppName").stringValue = _name;
                    m_AppConstConfig.FindPropertyRelative("m_AppPrefix").stringValue = _name + "_";
                    m_AppConstConfig.FindPropertyRelative("m_AppVersion").stringValue = "1.0";
                    m_AppConstConfig.FindPropertyRelative("m_UIPath").stringValue = "Prefabs/UI/";
                    m_AppConstConfig.FindPropertyRelative("m_AudioPath").stringValue = "Sources/";
                }
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Only, Lc.Reset, Lc.Manager, Lc.Level })))
                {
                    ResetManagerLevel();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6);
                EditorGUILayout.PropertyField(m_ResourcesArea);
                EditorGUILayout.Space(20);

                EditorGUILayout.EndScrollView();


                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

                if (!changeCheckScope.changed) return;
                m_SettingPanel.ApplyModifiedPropertiesWithoutUndo();
            }

            /// <summary>
            /// 系统信息
            /// </summary>
            private void SystemInfos()
            {
                m_SystemInfoSwitch = EditorGUILayout.BeginFoldoutHeaderGroup(m_SystemInfoSwitch,
                    LC.Combine(new Lc[] { Lc.Current, Lc.Hardware, Lc.Information }));
                if (m_SystemInfoSwitch)
                {
                    string _contents =
                    "---------------------------------------------------------------------------------------\n" +
                    $"{LC.Combine(new Lc[] { Lc.Operating, Lc.System })}: {SystemInfo.operatingSystem}\n" +
                    $"{LC.Combine(new Lc[] { Lc.System, Lc.Memory, Lc.Size })}: {SystemInfo.systemMemorySize} MB\n" +
                    $"{LC.Combine(new Lc[] { Lc.CPU, Lc.Name })}: {SystemInfo.processorType}\n" +
                    $"{LC.Combine(new Lc[] { Lc.CPU, Lc.Count })}: {SystemInfo.processorCount}\n" +
                    "---------------------------------------------------------------------------------------\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Name })}: {SystemInfo.graphicsDeviceName}\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Type })}: {SystemInfo.graphicsDeviceType}\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Memory, Lc.Size })}: {SystemInfo.graphicsMemorySize} MB\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Id })}: {SystemInfo.graphicsDeviceID}\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Vendor })}: {SystemInfo.graphicsDeviceVendor}\n" +
                    $"{LC.Combine(new Lc[] { Lc.GPU, Lc.Vendor, Lc.Id })}: {SystemInfo.graphicsDeviceVendorID}\n" +
                    "---------------------------------------------------------------------------------------\n" +
                    $"{LC.Combine(new Lc[] { Lc.Device, Lc.Version })}: {SystemInfo.deviceModel}\n" +
                    $"{LC.Combine(new Lc[] { Lc.Device, Lc.Name })}: {SystemInfo.deviceName}\n" +
                    $"{LC.Combine(new Lc[] { Lc.Device, Lc.Type })}: {SystemInfo.deviceType}\n" +
                    $"{LC.Combine(new Lc[] { Lc.Device, Lc.Id })}: {SystemInfo.deviceUniqueIdentifier}\n" +
                    "---------------------------------------------------------------------------------------\n" +
                    $"{LC.Combine(new Lc[] { Lc.Screen, Lc.Current })} Dpi: {Screen.dpi}\n" +
                    $"{LC.Combine(new Lc[] { Lc.Screen, Lc.Resolution })}: {Screen.currentResolution}\n" +
                    "---------------------------------------------------------------------------------------";

                    GUILayout.Box(_contents, "Badge");
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
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
                        if (EditorUtility.DisplayDialog(LC.Combine(Lc.Error), LC.Combine(new Lc[] { Lc.Related, Lc.Resource, Lc.Package, Lc.Lost }), LC.Combine(Lc.Import), LC.Combine(Lc.Cancel)))
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

                _contentsArray[17] = m_rendererPipline == 0 ? _contentsArray[17].Insert(0, "//") : _contentsArray[17].Replace("//", "");
                for (int i = 60; i < 63; i++)
                {
                    _contentsArray[i] = m_rendererPipline == 0 ? _contentsArray[i].Insert(16, "//") : _contentsArray[i].Replace("//", "");
                }

                File.WriteAllLines(_uiPath, _contentsArray);

                AssetDatabase.Refresh();
            }

            /// <summary>
            /// 重置管理者级别
            /// </summary>
            private void ResetManagerLevel()
            {
                m_AppConstManagerList.ClearArray();
                for (int i = 0; i < 10; i++)
                {
                    m_AppConstManagerList.InsertArrayElementAtIndex(0);
                }
                m_AppConstManagerList.GetArrayElementAtIndex(0).stringValue = "TimeManager";
                m_AppConstManagerList.GetArrayElementAtIndex(1).stringValue = "ToolManager";
                m_AppConstManagerList.GetArrayElementAtIndex(2).stringValue = "EventManager";
                m_AppConstManagerList.GetArrayElementAtIndex(3).stringValue = "HttpsManager";
                m_AppConstManagerList.GetArrayElementAtIndex(4).stringValue = "SocketManager";
                m_AppConstManagerList.GetArrayElementAtIndex(5).stringValue = "FolderManager";
                m_AppConstManagerList.GetArrayElementAtIndex(6).stringValue = "LoadManager";
                m_AppConstManagerList.GetArrayElementAtIndex(7).stringValue = "ScenesManager";
                m_AppConstManagerList.GetArrayElementAtIndex(8).stringValue = "AudioManager";
                m_AppConstManagerList.GetArrayElementAtIndex(9).stringValue = "UIManager";

            }

            /// <summary>
            /// 查找全部管理器
            /// </summary>
            private void FindAllManager()
            {
                List<string> _managerNameList = new List<string>();
                for (int i = 0; i < m_AppConstManagerList.arraySize; i++)
                {
                    _managerNameList.Add(m_AppConstManagerList.GetArrayElementAtIndex(i).stringValue);
                }

                bool _changed = false;
                TypeCache.TypeCollection _collection = TypeCache.GetTypesDerivedFrom(typeof(IManager));
                for (int i = 0; i < _collection.Count; i++)
                {
                    if (!_managerNameList.Contains(_collection[i].Name))
                    {
                        _changed = true;
                        int _cot = _managerNameList.Count;
                        m_AppConstManagerList.InsertArrayElementAtIndex(_cot);
                        m_AppConstManagerList.GetArrayElementAtIndex(_cot).stringValue = _collection[i].Name;
                    }
                }
                if (_changed)
                {
                    m_SettingPanel.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }
}
