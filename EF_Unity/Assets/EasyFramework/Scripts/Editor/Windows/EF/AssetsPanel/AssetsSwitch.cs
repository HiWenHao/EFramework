/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-16 10:16:59
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-16 10:16:59
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace SettingPanel
    {
        /// <summary>
        /// 资源开关面板
        /// </summary>
        public class AssetsSwitch : EFSettingBase
        {
            const string ASSETSINFO = "Description/AssetsInfo.json";
            const string EXAMPLEFOLDER = "ExampleGame";

            int m_ManagerIndex;
            int m_ManagerCount;
            Vector2 m_ManagersPos;
            Vector2 m_ManagersDesPos;

            int m_PluginsIndex;
            int m_PluginsCount;
            Vector2 m_PluginsPos;

            string m_AssetsPath;
            Vector2 m_AllPostation;
            AssetsInformation m_Assets;

            public override void OnEnable(string assetsPath)
            {
                m_AssetsPath = assetsPath;
                string _configPath = Path.Combine(m_AssetsPath, ASSETSINFO);
                m_Assets = JsonUtility.FromJson<AssetsInformation>(File.ReadAllText(_configPath));

                m_PluginsCount = m_Assets.Plugins.Count;
                m_ManagerCount = m_Assets.Managers.Count;
            }

            public override void OnGUI()
            {
                //  ScrollView
                m_AllPostation = EditorGUILayout.BeginScrollView(m_AllPostation);
                // Managers
                FoldoutHeaderGroup(ref m_Assets.ManagerListSwitch, ref m_ManagersPos, ref m_ManagerIndex, m_ManagerCount, managers: m_Assets.Managers, null);

                // Plugins
                FoldoutHeaderGroup(ref m_Assets.PluginsListSwitch, ref m_PluginsPos, ref m_PluginsIndex, m_PluginsCount, managers: null, m_Assets.Plugins);

                #region Example
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ToggleLeft(LC.Combine(Lc.Example), m_Assets.ExampleSwitch);
                if (GUILayout.Button(LC.Combine(m_Assets.ExampleSwitch ? Lc.Unload : Lc.Import), GUILayout.Width(160f)))
                {
                    if (m_Assets.ExampleSwitch)
                    {
                        string _path = Path.Combine(Application.dataPath, EXAMPLEFOLDER);
                        if (Directory.Exists(_path))
                        {
                            Directory.Delete(_path, true);
                            File.Delete(_path + ".meta");
                        }
                        m_Assets.ExampleSwitch = false;
                    }
                    else
                    {
                        string _sorPath = Path.Combine(m_AssetsPath, EXAMPLEFOLDER);
                        string _DesPath = Path.Combine(Application.dataPath, EXAMPLEFOLDER);
                        EditorUtils.CopyFolder(_sorPath, _DesPath);

                        m_Assets.ExampleSwitch = true;
                    }
                    SaveAssetsInfo();
                }
                EditorGUILayout.EndHorizontal();
                #endregion


                EditorGUILayout.EndScrollView();

                #region Confirm
                GUILayout.FlexibleSpace();
                //EditorGUILayout.BeginHorizontal();
                ////if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Reset, Lc.Current, Lc.Config })))
                ////{

                ////}
                //if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Confirm, Lc.Config })))
                //{

                //}
                //EditorGUILayout.EndHorizontal();
                #endregion
            }

            public override void OnDestroy()
            {
                SaveAssetsInfo();
            }

            #region Managers and Plugins
            void FoldoutHeaderGroup(ref bool mySwitch, ref Vector2 pos, ref int index, int count, List<ManagerInfo> managers = null, List<PluginsInfo> plugins = null)
            {
                mySwitch = EditorGUILayout.BeginFoldoutHeaderGroup(mySwitch, LC.Combine(new Lc[] { Lc.Plugins, Lc.Switch }));
                if (mySwitch)
                {
                    EditorGUILayout.BeginHorizontal("Badge");

                    //Left List
                    pos = EditorGUILayout.BeginScrollView(pos, GUILayout.Width(130f), GUILayout.Height(255f));
                    for (int i = 0; i < count; i++)
                    {
                        if (null != managers)
                            LeftItemButton(i, managers[i], ref index);

                        if (null != plugins)
                            LeftItemButton(i, plugins[i], ref index);
                    }
                    EditorGUILayout.EndScrollView();

                    //Right Contents
                    if (null != managers)
                        ItemShowPanel(managers[index], true);

                    if (null != plugins)
                        ItemShowPanel(plugins[index], false);


                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Space();
            }

            void LeftItemButton(int index, InfoBase info, ref int outIndex)
            {
                GUIStyle textButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                };
                bool _isLoad = info.IsLoad;
                textButtonStyle.normal.textColor = _isLoad ? Color.white : new Color(0.8f, 0.3f, 0.3f);
                if (GUILayout.Button($"{(_isLoad ? "√" : "X")}  {info.Name}", textButtonStyle, GUILayout.Width(120f)))
                {
                    outIndex = index;
                }
            }

            void ItemShowPanel(InfoBase info, bool isManager)
            {
                EditorGUILayout.BeginVertical();
                #region Title
                EditorGUILayout.LabelField(info.Name, new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 24
                }, GUILayout.Height(35f));
                GUILayout.Box(GUIContent.none, GUILayout.Height(3.0f), GUILayout.ExpandWidth(true));
                EditorGUILayout.Space();
                #endregion

                #region Body
                EditorGUILayout.BeginHorizontal();
                GUILayout.Box(GUIContent.none, GUILayout.Width(3.0f), GUILayout.ExpandHeight(true));
                EditorGUILayout.BeginVertical();

                EditorGUILayout.TextArea(info.Des[ProjectUtility.Project.LanguageIndex], "CN Message");

                if (isManager)
                {
                    GUILayout.Box(GUIContent.none, GUILayout.Height(2.0f), GUILayout.ExpandWidth(true));
                    ManagerInfo _manager = (ManagerInfo)info;
                    m_ManagersDesPos = EditorGUILayout.BeginScrollView(m_ManagersDesPos, GUILayout.Height(100f));
                    for (int i = 0; i < _manager.Rely.Count; i++)
                    {
                        EditorGUILayout.LabelField(_manager.Rely[i], GUILayout.Width(150f));
                    }
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                #endregion

                GUILayout.Box(GUIContent.none, GUILayout.Height(3.0f), GUILayout.ExpandWidth(true));
                bool _isLoad = info.IsLoad;
                if (GUILayout.Button(_isLoad ? LC.Combine(Lc.Unload) : LC.Combine(Lc.Import), GUILayout.MinWidth(100f)))
                {
                    string _path = Path.Combine(
                        Application.dataPath,
                        isManager ? "EasyFramework/Scripts/Runtime/Managers" : "EasyFramework/ThirdPartyAssets", 
                        info.Name);

                    if (_isLoad)
                    {
                        if (Directory.Exists(_path))
                        {
                            Directory.Delete(_path, true);
                            File.Delete(_path + ".meta");
                        }
                    }
                    else
                    {
                        string _sorPath = Path.Combine(m_AssetsPath, isManager ? "Scripts" : "Plugins", info.Name);
                        EditorUtils.CopyFolder(_sorPath, _path);
                    }

                    if (isManager)
                    {
                        string _monoPath = Path.Combine(Application.dataPath, "EasyFramework/Scripts/Runtime/EF.Start.cs");
                        string[] _lines = File.ReadAllLines(_monoPath);
                        ManagerInfo _manager = (ManagerInfo)info;
                        if (_isLoad) 
                            _lines[_manager.MonoIndex] = "//" + _lines[_manager.MonoIndex];
                        else
                            _lines[_manager.MonoIndex] = _lines[_manager.MonoIndex][2..];

                        File.WriteAllLines(_monoPath, _lines);
                    }

                    info.IsLoad = !_isLoad;
                    SaveAssetsInfo();
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }
            #endregion

            void SaveAssetsInfo()
            {
                string _configPath = Path.Combine(m_AssetsPath, ASSETSINFO);
                File.WriteAllText(_configPath, JsonUtility.ToJson(m_Assets), System.Text.Encoding.UTF8);
                AssetDatabase.Refresh();
            }
        }
    }
}
