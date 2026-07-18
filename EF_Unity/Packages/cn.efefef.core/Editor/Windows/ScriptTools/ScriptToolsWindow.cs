/*
 * ================================================
 * Describe:        This script is used to help user handle scripts .   Possible reference: --> plyoung and other author <-- Thanks in advance. ^_^
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2023-05-12 14:41:18
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-01 17:51:29
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EasyFramework.Windows.ScriptTools
{
    /// <summary>
    /// The script tools panel
    /// </summary>
    internal class ScriptToolsWindow : EditorWindow
    {
        private struct Info
        {
            public Object Target;
            public int LayersCount;
            public string ParentName;
            public string DetailsPath;
        }

        private int _typeIndex;
        private int _missingOpt;
        private int _missingTempOpt;
        private int _missingMaxCount;
        private int _missingTempCount;
        private int _dependenciesMaxCount;

        private bool _shouldRecurse;

        private Vector2 _missingScroll;
        private Vector2 _dependenciesScroll;

        private MonoScript _targetComponent;

        private readonly List<Info> _entries = new List<Info>();
        private readonly List<string> _results = new List<string>();

        [MenuItem(MenuItemToolkit.Tools + "🔍 Script Tools", false, MenuItemToolkit.ToolsPriority + 2)]
        private static void OpenWindow()
        {
            ScriptToolsWindow window = GetWindow<ScriptToolsWindow>(false, "Script Tools");
            window.minSize = new Vector2(360.0f, 200.0f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(LC.Combine(Lc.Script, Lc.Tool), GUIUtils.BoldCenterTitle());
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            _typeIndex = EditorGUILayout.Popup(LC.Combine(Lc.Select, Lc.Find, Lc.Type),
                _typeIndex,
                new[]
                {
                    LC.Combine(Lc.Rely, Lc.This, Lc.Script, Lc.Of, Lc.Prefab),
                    LC.Combine(Lc.Lost, Lc.Script, Lc.Of, Lc.Object),
                });
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

            //Dependencies
            if (_typeIndex == 0)
            {
                _targetComponent = (MonoScript)EditorGUILayout.ObjectField(
                    LC.Combine(Lc.Select, Lc.Target, Lc.Script), _targetComponent, typeof(MonoScript),
                    false);

                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                _shouldRecurse = EditorGUILayout.ToggleLeft(LC.Combine(Lc.Stw_RecurseDependencies), _shouldRecurse);
                if (GUILayout.Button(LC.Combine(Lc.Find, Lc.Script, Lc.Rely)))
                {
                    ActionSearchForComponent();
                }

                if (_dependenciesMaxCount != 0)
                    EditorGUILayout.LabelField(
                        $"{LC.Combine(Lc.Rely, Lc.Count)}:  [ {_dependenciesMaxCount} ] ");

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

                _missingOpt = EditorGUILayout.Popup(_missingOpt, new[]
                {
                    LC.Combine(Lc.In, Lc.All, Lc.Activity, Lc.Scene),
                    LC.Combine(Lc.In, Lc.All, Lc.Prefab)
                });
                if (_missingOpt != _missingTempOpt)
                {
                    _missingTempOpt = _missingOpt;
                    MissingFind();
                }

                EditorGUILayout.EndHorizontal();
                if (_missingMaxCount != 0)
                    EditorGUILayout.LabelField(
                        $"{LC.Combine(Lc.Lost, Lc.Count)}:  [ {_missingMaxCount} ] ");
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
                EditorGUILayout.LabelField(LC.Combine(Lc.Not, Lc.Found, Lc.Match));
            }
            else
            {
                _dependenciesScroll = EditorGUILayout.BeginScrollView(_dependenciesScroll);

                for (int i = 0; i < _dependenciesMaxCount; i++)
                {
                    string res = _results[i];
                    EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
                    if (GUILayout.Button(res, GUIUtils.LeftButtonStyle(), GUILayout.Height(25f)))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(res);
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"----- {LC.Combine(Lc.End)} -----", GUIUtils.CenteredEndLabel());
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
                string[] single = { prefab };
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
                if (Path.GetExtension(s).Equals(".prefab", StringComparison.OrdinalIgnoreCase))
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
            if (_entries == null || _entries.Count != _missingMaxCount)
                return;

            _missingScroll = EditorGUILayout.BeginScrollView(_missingScroll);
            for (int i = 0; i < _missingMaxCount; i++)
            {
                Info info = _entries[i];
                if (!info.Target)
                {
                    _missingTempCount--;
                    continue;
                }

                bool isRoot = info.LayersCount == 0;
                string prefix = isRoot
                    ? $" [ {LC.Combine(Lc.Root)} ]  >>> "
                    : $" {LC.Combine(Lc.Root, Lc.Target)} [ {info.ParentName} ]   {info.LayersCount} {LC.Combine(Lc.Layer)}  >>> ";

                EditorGUILayout.BeginHorizontal(EditorStyles.boldLabel);
                if (GUILayout.Button(prefix + info.DetailsPath,
                        ChangedColor(isRoot), GUILayout.Height(25f)))
                {
                    EditorGUIUtility.PingObject(info.Target);
                    Selection.activeObject = info.Target;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (_missingTempCount != _missingMaxCount)
            {
                MissingFind();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"----- {LC.Combine(Lc.End)} -----", GUIUtils.CenteredEndLabel());
            EditorGUILayout.Space(18f);
            EditorGUILayout.EndScrollView();
        }

        void MissingFind()
        {
            _missingMaxCount = 0;
            _entries.Clear();
            GameObject[] gos = Resources.FindObjectsOfTypeAll<GameObject>();
            _missingMaxCount = gos.Length;

            for (int i = 0; i < _missingMaxCount; i++)
            {
                GameObject go = gos[i];

                if ((_missingOpt == 0 && !go.scene.IsValid()) ||
                    (_missingOpt == 1 && go.scene.IsValid())) continue;

                bool hasLost = false;
                Component[] cos = go.GetComponents<Component>();
                foreach (var co in cos)
                {
                    if (co == null)
                    {
                        hasLost = true;
                        break;
                    }
                }

                if (!hasLost) continue;

                Transform tr = go.transform.parent;
                Info nfo = new Info
                {
                    DetailsPath = go.name,
                    Target = go
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
            _missingMaxCount = _entries.Count;
            _missingTempCount = _missingMaxCount;
        }

        #endregion

        /// <summary>
        /// 根据是否变化返回不同颜色的按钮样式（每次创建新实例，不污染共享状态）
        /// </summary>
        private GUIStyle ChangedColor(bool isRoot)
        {
            var style = GUIUtils.LeftButtonStyle();
            style.normal.textColor = isRoot
                ? Color.green
                : new Color(0.898f, 0.898f, 0.898f);
            return style;
        }
    }
}
