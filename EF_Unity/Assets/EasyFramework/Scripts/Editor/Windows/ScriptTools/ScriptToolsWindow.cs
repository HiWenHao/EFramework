/* 
 * ================================================
 * Describe:      This script is used to help user handle scripts .   Possible reference: --> plyoung and other author <-- Thanks in advance. ^_^
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-12 14:41:18
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-12 14:41:18
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace ScriptTools
    {
        /// <summary>
        /// The script tools panel
        /// </summary>
        public class ScriptToolsWindow : EditorWindow
        {
            private struct Info
            {
                public Object Target;
                public int LayersCount;
                public string ParentName;
                public string DetailsPath;
            }

            int m_typeIndex;
            int m_MissingOpt;
            int m_MissingTempOpt;
            int m_MinssingMaxCount;
            int m_MissingTempCount;
            int m_DependenciesMaxCount;

            bool m_ShouldRecurse = false;

            Vector2 m_MissingScroll;
            Vector2 m_DependenciesScroll;

            MonoScript targetComponent;

            List<Info> m_Entries = new List<Info>();
            List<string> m_Results = new List<string>();

            GUIStyle m_buttonStyle;

            [MenuItem("EFTools/Tools/Script Tools", priority = 200)]
            private static void OpenWindow()
            {
                ScriptToolsWindow window = GetWindow<ScriptToolsWindow>(false, "Script Tools");
                window.minSize = new Vector2(360.0f, 200.0f);
                window.Show();
            }

            private void OnEnable()
            {
                if (m_typeIndex == 1)
                {
                    MissingFind();
                }
            }

            private void OnGUI()
            {
                #region Style Initialize
                m_buttonStyle = new GUIStyle("button")
                {
                    alignment = TextAnchor.MiddleLeft
                };
                #endregion

                EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Script, Lc.Tool }), new GUIStyle("label")
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    fixedHeight = 30
                });
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                m_typeIndex = EditorGUILayout.Popup(LC.Combine(new Lc[] { Lc.Select, Lc.Find, Lc.Type }),
                    m_typeIndex, 
                    new[]
                    {
                        LC.Combine(new Lc[]{ Lc.Rely, Lc.This, Lc.Script, Lc.Of, Lc.Prefab }),
                        LC.Combine(new Lc[]{ Lc.Lost, Lc.Script, Lc.Of, Lc.Object }),
                    });
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

                //Dependencies
                if (m_typeIndex == 0)
                {
                    targetComponent = (MonoScript)EditorGUILayout.ObjectField(LC.Combine(new Lc[] { Lc.Select, Lc.Target, Lc.Script }), targetComponent, typeof(MonoScript), false);

                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                    m_ShouldRecurse = EditorGUILayout.ToggleLeft(LC.Combine(Lc.Stw_RecurseDependencies), m_ShouldRecurse);
                    if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Find, Lc.Script, Lc.Rely })))
                    {
                        ActionSearchForComponent();
                    }
                    if (m_DependenciesMaxCount != 0)
                        EditorGUILayout.LabelField($"{LC.Combine(new Lc[] { Lc.Rely, Lc.Count })}:  [ {m_DependenciesMaxCount} ] ");

                    DependenciesListInfoShow();
                }
                //Missing
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(LC.Combine(Lc.Find)))
                    {
                        MissingFind();
                    }
                    m_MissingOpt = EditorGUILayout.Popup(m_MissingOpt, new string[]
                        {
                            LC.Combine(new Lc[]{ Lc.In, Lc.All, Lc.Activity, Lc.Scene }),
                            LC.Combine(new Lc[]{ Lc.In, Lc.All, Lc.Prefab })
                        });
                    if (m_MissingOpt != m_MissingTempOpt)
                    {
                        m_MissingTempOpt = m_MissingOpt;
                        MissingFind();
                    }
                    EditorGUILayout.EndHorizontal();
                    if (m_MinssingMaxCount != 0)
                        EditorGUILayout.LabelField($"{LC.Combine(new Lc[] { Lc.Lost, Lc.Count })}:  [ {m_MinssingMaxCount} ] ");
                    EditorGUILayout.Space();

                    MissingListInfoShow();
                }
            }

            #region Dependencies
            void DependenciesListInfoShow()
            {
                if (m_Results == null)
                    return;

                if (m_DependenciesMaxCount == 0)
                {
                    EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Not, Lc.Found, Lc.Match }));
                }
                else
                {
                    m_DependenciesScroll = EditorGUILayout.BeginScrollView(m_DependenciesScroll);

                    for (int i = 0; i < m_DependenciesMaxCount; i++)
                    {
                        string _res = m_Results[i];
                        EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
                        if (GUILayout.Button(_res, m_buttonStyle, GUILayout.Height(25f)))
                        {
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(_res);
                            EditorGUIUtility.PingObject(Selection.activeObject);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"----- {LC.Combine(Lc.End)} -----", new GUIStyle(EditorStyles.whiteLabel) { alignment = TextAnchor.MiddleCenter });
                    EditorGUILayout.Space(18f);

                    EditorGUILayout.EndScrollView();
                }
            }

            private void ActionSearchForComponent()
            {
                string _targetPath = AssetDatabase.GetAssetPath(targetComponent);
                string[] allPrefabs = GetAllPrefabs();

                m_Results.Clear();
                m_DependenciesMaxCount = allPrefabs.Length;
                for (int i = 0; i < m_DependenciesMaxCount; i++)
                {
                    string _prefab = allPrefabs[i];
                    string[] _single = new string[] { _prefab };
                    string[] _dependencies = AssetDatabase.GetDependencies(_single, m_ShouldRecurse);
                    foreach (string dependentAsset in _dependencies)
                    {
                        if (dependentAsset == _targetPath)
                        {
                            m_Results.Add(_prefab);
                        }
                    }
                }
                m_DependenciesMaxCount = m_Results.Count;
            }

            public static string[] GetAllPrefabs()
            {
                string[] temp = AssetDatabase.GetAllAssetPaths();
                List<string> result = new List<string>();
                foreach (string s in temp)
                {
                    if (s.Contains(".prefab"))
                    {
                        result.Add(s);
                    }
                }
                return result.ToArray();
            }

            #endregion

            #region Missing
            void MissingListInfoShow()
            {
                if (m_Entries == null || m_Entries.Count != m_MinssingMaxCount)
                    return;

                m_MissingScroll = EditorGUILayout.BeginScrollView(m_MissingScroll);
                for (int i = 0; i < m_MinssingMaxCount; i++)
                {
                    Info _info = m_Entries[i];
                    if (!_info.Target)
                    {
                        m_MissingTempCount--;
                        continue;
                    }
                    EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
                    if (GUILayout.Button((_info.LayersCount == 0 ? $" [ {LC.Combine(Lc.Root)} ]  >>> " : $" {LC.Combine(new Lc[] { Lc.Root, Lc.Target })} [ {_info.ParentName} ]   {_info.LayersCount} {LC.Combine(Lc.Layer)}  >>> ")
                        + _info.DetailsPath, ChangedColor(_info.LayersCount == 0), GUILayout.Height(25f)))
                    {
                        EditorGUIUtility.PingObject(_info.Target);
                        Selection.activeObject = _info.Target;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (m_MissingTempCount != m_MinssingMaxCount)
                {
                    MissingFind();
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"----- {LC.Combine(Lc.End)} -----", new GUIStyle(EditorStyles.whiteLabel) { alignment = TextAnchor.MiddleCenter });
                EditorGUILayout.Space(18f);
                EditorGUILayout.EndScrollView();
            }

            void MissingFind()
            {
                m_MinssingMaxCount = 0;
                m_Entries.Clear();
                GameObject[] gos = Resources.FindObjectsOfTypeAll<GameObject>();
                m_MinssingMaxCount = gos.Length;

                for (int i = 0; i < m_MinssingMaxCount; i++)
                {
                    GameObject _go = gos[i];


                    if ((m_MissingOpt == 0 && !_go.scene.IsValid()) ||
                        (m_MissingOpt == 1 && _go.scene.IsValid())) continue;

                    bool _hasLost = false;
                    Component[] cos = _go.GetComponents<Component>();
                    foreach (var co in cos)
                    {
                        if (co == null)
                        {
                            _hasLost = true;
                            break;
                        }
                    }
                    if (!_hasLost) continue;

                    Transform tr = _go.transform.parent;
                    Info nfo = new Info()
                    {
                        DetailsPath = _go.name,
                        Target = _go
                    };
                    int _layoutCount = 0;
                    while (tr != null)
                    {
                        _layoutCount++;
                        nfo.DetailsPath = $"{tr.name} / {nfo.DetailsPath}";
                        nfo.ParentName = tr.name;
                        tr = tr.parent;
                    }
                    nfo.LayersCount = _layoutCount;
                    m_Entries.Add(nfo);
                }

                m_Entries.Sort((a, b) => a.DetailsPath.CompareTo(b.DetailsPath));
                m_MinssingMaxCount = m_Entries.Count;
                m_MissingTempCount = m_MinssingMaxCount;
            }
            #endregion

            GUIStyle ChangedColor(bool changed)
            {
                if (changed)
                    m_buttonStyle.normal.textColor = Color.green;
                else
                    m_buttonStyle.normal.textColor = new Color(0.898f, 0.898f, 0.898f);
                return m_buttonStyle;
            }
        }
    }
}
