/*
 * ================================================
 * Describe:        This script is used to help user handle scripts .   Possible reference: --> plyoung and other author <-- Thanks in advance. ^_^
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2023-05-12 14:41:18
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-10-31 22:55:00
 * ScriptVersion:   0.2
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
    internal class ScriptToolsWindow : EditorWindowBase
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
        private int _dependenciesMaxCount;

        private bool _shouldRecurse;

        private Vector2 _missingScroll;
        private Vector2 _dependenciesScroll;

        private MonoScript _targetComponent;

        private readonly List<Info> _entries = new List<Info>();
        private readonly List<string> _results = new List<string>();

        /// <summary>
        /// 待执行的数据刷新类型
        /// <para>Pending data refresh kind.</para>
        /// </summary>
        private enum PendingWork
        {
            /// <summary>无<para>None.</para></summary>
            None = 0,

            /// <summary>重扫丢失脚本<para>Rescan missing scripts.</para></summary>
            MissingScan = 1,

            /// <summary>重扫依赖预制体<para>Rescan prefab dependencies.</para></summary>
            DependencyScan = 2,
        }

        // 待执行的数据刷新（点击只登记意图，真正刷新延后到下一次 Layout 事件）
        private PendingWork _pendingWork = PendingWork.None;

        [MenuItem(MenuItemToolkit.Tools + "🔍 Script Tools", false, MenuItemToolkit.ToolsPriority + 2)]
        private static void OpenWindow()
        {
            ScriptToolsWindow window = GetWindow<ScriptToolsWindow>(false, "Script Tools");
            window.minSize = new Vector2(360.0f, 200.0f);
            window.Show();
        }

        protected override void OnSmartGUI()
        {
            // 必须先于任何控件绘制执行：本次刷新会让列表行数变化，延后到 Layout 事件可保证
            // Layout 与随后的 Repaint 看到同一份数据（详见 ApplyPendingWork 注释）
            ApplyPendingWork();

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
                    _pendingWork = PendingWork.DependencyScan;
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
                    _pendingWork = PendingWork.MissingScan;
                }

                _missingOpt = EditorGUILayout.Popup(_missingOpt, new[]
                {
                    LC.Combine(Lc.In, Lc.All, Lc.Activity, Lc.Scene),
                    LC.Combine(Lc.In, Lc.All, Lc.Prefab)
                });
                if (_missingOpt != _missingTempOpt)
                {
                    _missingTempOpt = _missingOpt;
                    _pendingWork = PendingWork.MissingScan;
                }

                EditorGUILayout.EndHorizontal();
                if (_missingMaxCount != 0)
                    EditorGUILayout.LabelField(
                        $"{LC.Combine(Lc.Lost, Lc.Count)}:  [ {_missingMaxCount} ] ");
                EditorGUILayout.Space();

                MissingListInfoShow();
            }
        }

        /// <summary>
        /// 执行被延后的数据刷新。
        /// <para>Executes the deferred data refresh.</para>
        /// <para>
        /// IMGUI 中 Layout 事件负责计算并缓存控件矩形，Repaint / MouseUp 复用同一份布局结果；
        /// 若在同一次 GUI pass 内改变控件数量（例如点击按钮后立刻把列表从 0 行刷成 N 行），
        /// Unity 会判定 GUILayout 不匹配并抛 <c>ExitGUIException</c> 中止本次绘制（表现为控制台报错、界面不刷新）。
        /// 因此点击只登记意图，真正刷新等到下一次 Layout 事件执行，保证 Layout 与随后的 Repaint 数据一致。
        /// </para>
        /// </summary>
        private void ApplyPendingWork()
        {
            if (_pendingWork == PendingWork.None) return;
            if (Event.current.type != EventType.Layout)
            {
                // 主动请求一次重绘，保证后续一定会来 Layout 事件，刷新不会一直挂起
                Repaint();
                return;
            }

            PendingWork work = _pendingWork;
            _pendingWork = PendingWork.None;

            switch (work)
            {
                case PendingWork.MissingScan:
                    MissingFind();
                    break;

                case PendingWork.DependencyScan:
                    ActionSearchForComponent();
                    break;
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
            int invalidCount = 0;
            for (int i = 0; i < _missingMaxCount; i++)
            {
                Info info = _entries[i];
                if (!info.Target)
                {
                    invalidCount++;
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

            // 目标对象已被销毁（场景/预制体被改动）：登记一次重扫。
            // 注意不能在这里直接重扫——绘制过程中改变数据会让本次 GUI pass 的控件数量与 Layout 缓存不一致，触发 ExitGUIException
            if (invalidCount > 0)
            {
                _pendingWork = PendingWork.MissingScan;
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
