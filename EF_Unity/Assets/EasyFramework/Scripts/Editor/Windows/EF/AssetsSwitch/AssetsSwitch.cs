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
        internal class AssetsSwitch : EFSettingBase
        {
            const string ASSETSINFO = "Description/AssetsInfo.json";
            const string EXAMPLEFOLDER = "ExampleGame";

            int _managerIndex;
            int _managerCount;
            Vector2 _managersPos;
            Vector2 _managersDesPos;

            int pluginsIndex;
            int _pluginsCount;
            Vector2 _pluginsPos;

            string _assetsPath;
            Vector2 _allPostation;
            AssetsInformation _assets;

            internal AssetsSwitch(string name) : base(name)
            {

            }

            internal override void OnEnable(string assetsPath)
            {
                if (IsInitialzed)
                    return;
                IsInitialzed = true;

                _assetsPath = assetsPath;
                string configPath = Path.Combine(_assetsPath, ASSETSINFO);
                _assets = JsonUtility.FromJson<AssetsInformation>(File.ReadAllText(configPath));

                _pluginsCount = _assets.Plugins.Count;
                _managerCount = _assets.Managers.Count;
            }

            internal override void OnGUI()
            {
                //  ScrollView
                _allPostation = EditorGUILayout.BeginScrollView(_allPostation);
                // Managers
                FoldoutHeaderGroup(ref _assets.ManagerListSwitch, ref _managersPos, ref _managerIndex, _managerCount, managers: _assets.Managers, null);

                // Plugins
                FoldoutHeaderGroup(ref _assets.PluginsListSwitch, ref _pluginsPos, ref pluginsIndex, _pluginsCount, managers: null, _assets.Plugins);

                #region Example
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ToggleLeft(LC.Combine(new Lc[] { Lc.Example, Lc.Project }), _assets.ExampleSwitch);
                if (GUILayout.Button(LC.Combine(_assets.ExampleSwitch ? Lc.Unload : Lc.Import), GUILayout.Width(160f)))
                {
                    if (_assets.ExampleSwitch)
                    {
                        string path = Path.Combine(Application.dataPath, EXAMPLEFOLDER);
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                            File.Delete(path + ".meta");
                        }
                        _assets.ExampleSwitch = false;
                    }
                    else
                    {
                        string sorPath = Path.Combine(_assetsPath, EXAMPLEFOLDER);
                        string desPath = Path.Combine(Application.dataPath, EXAMPLEFOLDER);
                        EditorUtils.CopyFolder(sorPath, desPath);

                        _assets.ExampleSwitch = true;
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

            internal override void OnDestroy()
            {
                SaveAssetsInfo();
            }

            #region Managers and Plugins
            void FoldoutHeaderGroup(ref bool mySwitch, ref Vector2 pos, ref int index, int count, List<ManagerInfo> managers = null, List<PluginsInfo> plugins = null)
            {
                mySwitch = EditorGUILayout.BeginFoldoutHeaderGroup(mySwitch, LC.Combine(new Lc[] { (managers == null) ? Lc.Plugins : Lc.Manager, Lc.Switch }));
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
                bool isLoad = info.IsLoad;
                textButtonStyle.normal.textColor = isLoad ? Color.white : new Color(0.8f, 0.3f, 0.3f);
                if (GUILayout.Button($"{(isLoad ? "√" : "X")}  {info.Name}", textButtonStyle, GUILayout.Width(120f)))
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
                    _managersDesPos = EditorGUILayout.BeginScrollView(_managersDesPos, GUILayout.Height(100f));
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
                bool isLoad = info.IsLoad;
                if (GUILayout.Button(isLoad ? LC.Combine(Lc.Unload) : LC.Combine(Lc.Import), GUILayout.MinWidth(100f)))
                {
                    string path = Path.Combine(
                        Application.dataPath,
                        isManager ? "EasyFramework/Scripts/Runtime/Managers" : "EasyFramework/ThirdPartyAssets", 
                        info.Name);

                    if (isLoad)
                    {
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path, true);
                            File.Delete(path + ".meta");
                        }
                    }
                    else
                    {
                        string sorPath = Path.Combine(_assetsPath, isManager ? "Scripts" : "Plugins", info.Name);
                        EditorUtils.CopyFolder(sorPath, path);
                    }

                    if (isManager)
                    {
                        string monoPath = Path.Combine(Application.dataPath, "EasyFramework/Scripts/Runtime/EF.Start.cs");
                        string[] lines = File.ReadAllLines(monoPath);
                        ManagerInfo _manager = (ManagerInfo)info;
                        if (isLoad) 
                            lines[_manager.MonoIndex] = "//" + lines[_manager.MonoIndex];
                        else
                            lines[_manager.MonoIndex] = lines[_manager.MonoIndex][2..];

                        File.WriteAllLines(monoPath, lines);
                    }

                    info.IsLoad = !isLoad;
                    SaveAssetsInfo();
                }
                EditorGUILayout.Space();
                EditorGUILayout.EndVertical();
            }
            #endregion

            void SaveAssetsInfo()
            {
                string configPath = Path.Combine(_assetsPath, ASSETSINFO);
                File.WriteAllText(configPath, JsonUtility.ToJson(_assets), System.Text.Encoding.UTF8);
                AssetDatabase.Refresh();
            }
        }
    }
}
