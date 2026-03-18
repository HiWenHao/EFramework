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

            int _typeIndex;
            int _missingOpt;
            int _missingTempOpt;
            int _minssingMaxCount;
            int _missingTempCount;
            int _dependenciesMaxCount;

            bool _shouldRecurse = false;

            Vector2 _missingScroll;
            Vector2 _dependenciesScroll;

            MonoScript _targetComponent;

            List<Info> _entries = new List<Info>();
            List<string> _results = new List<string>();

            GUIStyle _buttonStyle;

            [MenuItem("EFTools/Tools/Script Tools", priority = 200)]
            private static void OpenWindow()
            {
                ScriptToolsWindow window = GetWindow<ScriptToolsWindow>(false, "Script Tools");
                window.minSize = new Vector2(360.0f, 200.0f);
                window.Show();
            }

            private void OnEnable()
            {
                if (_typeIndex == 1)
                {
                    MissingFind();
                }
            }

            private void OnGUI()
            {
                #region Style Initialize
                _buttonStyle = new GUIStyle("button")
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
                _typeIndex = EditorGUILayout.Popup(LC.Combine(new Lc[] { Lc.Select, Lc.Find, Lc.Type }),
                    _typeIndex, 
                    new[]
                    {
                        LC.Combine(new Lc[]{ Lc.Rely, Lc.This, Lc.Script, Lc.Of, Lc.Prefab }),
                        LC.Combine(new Lc[]{ Lc.Lost, Lc.Script, Lc.Of, Lc.Object }),
                    });
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

                //Dependencies
                if (_typeIndex == 0)
                {
                    _targetComponent = (MonoScript)EditorGUILayout.ObjectField(LC.Combine(new Lc[] { Lc.Select, Lc.Target, Lc.Script }), _targetComponent, typeof(MonoScript), false);

                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                    _shouldRecurse = EditorGUILayout.ToggleLeft(LC.Combine(Lc.Stw_RecurseDependencies), _shouldRecurse);
                    if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Find, Lc.Script, Lc.Rely })))
                    {
                        ActionSearchForComponent();
                    }
                    if (_dependenciesMaxCount != 0)
                        EditorGUILayout.LabelField($"{LC.Combine(new Lc[] { Lc.Rely, Lc.Count })}:  [ {_dependenciesMaxCount} ] ");

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
                    _missingOpt = EditorGUILayout.Popup(_missingOpt, new string[]
                        {
                            LC.Combine(new Lc[]{ Lc.In, Lc.All, Lc.Activity, Lc.Scene }),
                            LC.Combine(new Lc[]{ Lc.In, Lc.All, Lc.Prefab })
                        });
                    if (_missingOpt != _missingTempOpt)
                    {
                        _missingTempOpt = _missingOpt;
                        MissingFind();
                    }
                    EditorGUILayout.EndHorizontal();
                    if (_minssingMaxCount != 0)
                        EditorGUILayout.LabelField($"{LC.Combine(new Lc[] { Lc.Lost, Lc.Count })}:  [ {_minssingMaxCount} ] ");
                    EditorGUILayout.Space();

                    MissingListInfoShow();
                }
            }

            #region Dependencies
            void DependenciesListInfoShow()
            {
                if (_results == null)
                    return;

                if (_dependenciesMaxCount == 0)
                {
                    EditorGUILayout.LabelField(LC.Combine(new Lc[] { Lc.Not, Lc.Found, Lc.Match }));
                }
                else
                {
                    _dependenciesScroll = EditorGUILayout.BeginScrollView(_dependenciesScroll);

                    for (int i = 0; i < _dependenciesMaxCount; i++)
                    {
                        string res = _results[i];
                        EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
                        if (GUILayout.Button(res, _buttonStyle, GUILayout.Height(25f)))
                        {
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(res);
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
                string targetPath = AssetDatabase.GetAssetPath(_targetComponent);
                string[] allPrefabs = GetAllPrefabs();

                _results.Clear();
                _dependenciesMaxCount = allPrefabs.Length;
                for (int i = 0; i < _dependenciesMaxCount; i++)
                {
                    string prefab = allPrefabs[i];
                    string[] single = new string[] { prefab };
                    string[] dependencies = AssetDatabase.GetDependencies(single, _shouldRecurse);
                    foreach (string dependentAsset in dependencies)
                    {
                        if (dependentAsset == targetPath)
                        {
                            _results.Add(prefab);
                        }
                    }
                }
                _dependenciesMaxCount = _results.Count;
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
                if (_entries == null || _entries.Count != _minssingMaxCount)
                    return;

                _missingScroll = EditorGUILayout.BeginScrollView(_missingScroll);
                for (int i = 0; i < _minssingMaxCount; i++)
                {
                    Info _info = _entries[i];
                    if (!_info.Target)
                    {
                        _missingTempCount--;
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
                if (_missingTempCount != _minssingMaxCount)
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
                _minssingMaxCount = 0;
                _entries.Clear();
                GameObject[] gos = Resources.FindObjectsOfTypeAll<GameObject>();
                _minssingMaxCount = gos.Length;

                for (int i = 0; i < _minssingMaxCount; i++)
                {
                    GameObject _go = gos[i];


                    if ((_missingOpt == 0 && !_go.scene.IsValid()) ||
                        (_missingOpt == 1 && _go.scene.IsValid())) continue;

                    bool hasLost = false;
                    Component[] cos = _go.GetComponents<Component>();
                    foreach (var co in cos)
                    {
                        if (co == null)
                        {
                            hasLost = true;
                            break;
                        }
                    }
                    if (!hasLost) continue;

                    Transform tr = _go.transform.parent;
                    Info nfo = new Info()
                    {
                        DetailsPath = _go.name,
                        Target = _go
                    };
                    int layoutCount = 0;
                    while (tr != null)
                    {
                        layoutCount++;
                        nfo.DetailsPath = $"{tr.name} / {nfo.DetailsPath}";
                        nfo.ParentName = tr.name;
                        tr = tr.parent;
                    }
                    nfo.LayersCount = layoutCount;
                    _entries.Add(nfo);
                }

                _entries.Sort((a, b) => a.DetailsPath.CompareTo(b.DetailsPath));
                _minssingMaxCount = _entries.Count;
                _missingTempCount = _minssingMaxCount;
            }
            #endregion

            GUIStyle ChangedColor(bool changed)
            {
                if (changed)
                    _buttonStyle.normal.textColor = Color.green;
                else
                    _buttonStyle.normal.textColor = new Color(0.898f, 0.898f, 0.898f);
                return _buttonStyle;
            }
        }
    }
}
