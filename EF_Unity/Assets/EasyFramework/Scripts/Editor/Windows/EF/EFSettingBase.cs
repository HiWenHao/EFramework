/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-10 15:43:25
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-10 15:43:25
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace SettingPanel
    {
        /// <summary>
        /// 设置面板基类
        /// </summary>
        public abstract class EFSettingBase
        {
            public abstract void OnEnable();
            public abstract void OnGUI();
        }

        public class ManagerSwitch : EFSettingBase
        {
            int m_ManagerIndex;
            int m_ManagerCount;
            Vector2 m_ManagersPos;
            Vector2 m_ManagersDesPos;


            int m_PluginsIndex;
            int m_PluginsCount;
            Vector2 m_PluginsPos;

            bool m_ManagerSwitch;
            bool m_PluginsSwitch;
            string m_AssetsConfigPath;
            AssetsInformation m_Assets;

            public override void OnEnable()
            {
                DirectoryInfo _jsonFolder = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent;

                m_AssetsConfigPath = Path.Combine(_jsonFolder.FullName, "EF_Assets/Description/ManagerInformation.json");

                m_Assets  = JsonUtility.FromJson<AssetsInformation>(File.ReadAllText(m_AssetsConfigPath));

                m_PluginsCount = m_Assets.Plugins.Count;
                m_ManagerCount = m_Assets.Managers.Count;
            }
            public override void OnGUI()
            {
                #region Managers
                m_ManagerSwitch = EditorGUILayout.BeginFoldoutHeaderGroup(m_ManagerSwitch, LC.Combine(new Lc[] { Lc.Manager, Lc.Switch }));
                if (m_ManagerSwitch)
                {
                    m_PluginsSwitch = false;
                    EditorGUILayout.BeginHorizontal("Badge");

                    //Left List
                    m_ManagersPos = EditorGUILayout.BeginScrollView(m_ManagersPos, GUILayout.Width(130f), GUILayout.Height(255f));
                    for (int i = 0; i < m_ManagerCount; i++)
                    {
                        LeftItemButton(i, m_Assets.Managers[i], ref m_ManagerIndex);
                    }
                    EditorGUILayout.EndScrollView();

                    //Right Contents
                    ManagerInfo();

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Space();
                #endregion

                #region Plugins
                m_PluginsSwitch = EditorGUILayout.BeginFoldoutHeaderGroup(m_PluginsSwitch, LC.Combine(new Lc[] { Lc.Plugins, Lc.Switch }));
                if (m_PluginsSwitch)
                {
                    m_ManagerSwitch = false;
                    EditorGUILayout.BeginHorizontal("Badge");

                    //Left List
                    m_PluginsPos = EditorGUILayout.BeginScrollView(m_PluginsPos, GUILayout.Width(130f), GUILayout.Height(255f));
                    for (int i = 0; i < m_PluginsCount; i++)
                    {
                        LeftItemButton(i, m_Assets.Plugins[i], ref m_PluginsIndex);

                    }
                    EditorGUILayout.EndScrollView();

                    //Right Contents
                    PluginsInfo();


                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUILayout.Space();
                #endregion

                m_Assets.ExampleSwitch = EditorGUILayout.ToggleLeft(LC.Combine(new Lc[] { Lc.Import, Lc.Example }), m_Assets.ExampleSwitch);


                #region Confirm
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Confirm, Lc.Config })))
                {
                    string _temp = JsonUtility.ToJson(m_Assets);
                    File.WriteAllText(m_AssetsConfigPath, _temp, System.Text.Encoding.UTF8);
                }
                #endregion
            }

            #region Manager
            void ManagerInfo()
            {
                EditorGUILayout.BeginVertical();
                #region Title
                EditorGUILayout.LabelField(m_Assets.Managers[m_ManagerIndex].Name, new GUIStyle(GUI.skin.label)
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

                EditorGUILayout.TextArea(m_Assets.Managers[m_ManagerIndex].Des[ProjectUtility.Project.LanguageIndex], "CN Message");

                if (null != m_Assets.Managers[m_ManagerIndex].Rely)
                {
                    GUILayout.Box(GUIContent.none, GUILayout.Height(2.0f), GUILayout.ExpandWidth(true));

                    m_ManagersDesPos = EditorGUILayout.BeginScrollView(m_ManagersDesPos, GUILayout.Height(100f));
                    for (int i = 0; i < m_Assets.Managers[m_ManagerIndex].Rely.Count; i++)
                    {
                        EditorGUILayout.LabelField(m_Assets.Managers[m_ManagerIndex].Rely[i], GUILayout.Width(150f));
                    }
                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                #endregion

                DrawBottomButton(m_Assets.Managers[m_ManagerIndex]);

                EditorGUILayout.EndVertical();
            }
            #endregion

            #region Plugins
            void PluginsInfo()
            {
                EditorGUILayout.BeginVertical();
                #region Title
                EditorGUILayout.LabelField(m_Assets.Plugins[m_PluginsIndex].Name, new GUIStyle(GUI.skin.label)
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

                EditorGUILayout.TextArea(m_Assets.Plugins[m_PluginsIndex].Des[ProjectUtility.Project.LanguageIndex], "CN Message");

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();

                #endregion

                DrawBottomButton(m_Assets.Plugins[m_PluginsIndex]);

                EditorGUILayout.EndVertical();
            }
            #endregion

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
            void DrawBottomButton(InfoBase info)
            {
                GUILayout.Box(GUIContent.none, GUILayout.Height(3.0f), GUILayout.ExpandWidth(true));
                bool _isLoad = info.IsLoad;
                if (GUILayout.Button(_isLoad ? LC.Combine(Lc.Unload) : LC.Combine(Lc.Import), GUILayout.MinWidth(100f)))
                {
                    info.IsLoad = !_isLoad;
                }
                EditorGUILayout.Space();
            }
        }
    }
}
