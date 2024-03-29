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

            readonly GUIContent[] m_TypeContent = new[]
            {
                new GUIContent("Script Dependencies", "脚本依赖项"),
                new GUIContent("Script Missing", "丢失脚本的对象")
            };
            readonly GUIContent[] m_OptContents = new[]
            {
                new GUIContent("In Active Scenes", "在活动场景中"),
                new GUIContent("On Prefabs", "为预制件")
            };

            [MenuItem("EFTools/Assets/Script Tools &F", priority = 10)]
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

                EditorGUILayout.LabelField(new GUIContent("Script Tools", "脚本工具"), new GUIStyle("label")
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    fixedHeight = 30
                });
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                m_typeIndex = EditorGUILayout.Popup(new GUIContent("Selection Find GunsType", "选择查找类型"), m_typeIndex, m_TypeContent);
                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

                //Dependencies
                if (m_typeIndex == 0)
                {
                    targetComponent = (MonoScript)EditorGUILayout.ObjectField(new GUIContent("Select Target Script", "选择查询脚本"), targetComponent, typeof(MonoScript), false);

                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                    m_ShouldRecurse = EditorGUILayout.ToggleLeft(new GUIContent("Recurse Dependencies    (Warning: Very Slow)", "递归查找, 非常慢"), m_ShouldRecurse);
                    if (GUILayout.Button(new GUIContent("Find Dependencies", "查找脚本依赖项")))
                    {
                        ActionSearchForComponent();
                    }
                    if (m_DependenciesMaxCount != 0)
                        EditorGUILayout.LabelField(new GUIContent($"Dependencies Count:  [ {m_DependenciesMaxCount} ] ", "依赖数量"));

                    DependenciesListInfoShow();
                }
                //Missing
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent("Find", "查找")))
                    {
                        MissingFind();
                    }
                    m_MissingOpt = EditorGUILayout.Popup(m_MissingOpt, m_OptContents);
                    if (m_MissingOpt != m_MissingTempOpt)
                    {
                        m_MissingTempOpt = m_MissingOpt;
                        MissingFind();
                    }
                    EditorGUILayout.EndHorizontal();
                    if (m_MinssingMaxCount != 0)
                        EditorGUILayout.LabelField(new GUIContent($"Missing Count:  [ {m_MinssingMaxCount} ] ", "丢失数量"));
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
                    EditorGUILayout.LabelField(new GUIContent("No matches found.", "未找到匹配项"));
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
                    EditorGUILayout.LabelField("----- The End -----", new GUIStyle(EditorStyles.whiteLabel) { alignment = TextAnchor.MiddleCenter });
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
                for (int i = 0; i < m_MinssingMaxCount - 1; i++)
                {
                    Info _info = m_Entries[i];
                    if (!_info.Target)
                    {
                        m_MissingTempCount--;
                        continue;
                    }
                    EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
                    if (GUILayout.Button((_info.LayersCount == 0 ? " [ Root Object ]  >>> " : $" Root [ {_info.ParentName} ]   {_info.LayersCount} Layers  >>> ")
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
                EditorGUILayout.LabelField("----- The End -----", new GUIStyle(EditorStyles.whiteLabel) { alignment = TextAnchor.MiddleCenter });
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
