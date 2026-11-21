/*
 * ================================================
 * Describe:      红点系统编辑器监视器 - 树状图（类型/数值右对齐，节点名可点击折叠）
 * Author:        Alvin5100, AI
 * CreationTime:  2026-05-14
 * ModifyAuthor:  Alvin5100, AI
 * ModifyTime:    2026-11-20
 * ScriptVersion: 0.1
 * ===============================================
 */

#if UNITY_EDITOR
using System.Collections.Generic;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.RedDot
{
    [CustomEditor(typeof(RedDotSystem))]
    public class RedDotSystemInspector : UnityEditor.Editor
    {
        private RedDotSystem _target;
        private double _lastRepaintTime;
        private const double RepaintInterval = 0.3f;

        private GUIStyle _bgStyle;
        private Vector2 _scrollPos;
        private readonly List<RedDotNode> _rootNodes = new();
        private readonly Dictionary<string, bool> _foldouts = new();
        private readonly HashSet<string> _nodesKeySet = new(); // 复用的有效 key 集合
        private readonly List<string> _staleKeys = new();      // 复用的待清理 key 列表

        // 缓存的 GUIStyle：按显示类型 + 数值是否为 0 预建，避免每个节点每次重绘都 new
        private GUIStyle[] _typeStyles;
        private GUIStyle _numActiveStyle;
        private GUIStyle _numIdleStyle;
        private GUIStyle _headerCenterStyle;

        private void OnEnable()
        {
            _target = (RedDotSystem)target;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying) return;
            if (!(EditorApplication.timeSinceStartup - _lastRepaintTime >= RepaintInterval)) return;
            _lastRepaintTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10);
            var titleRect = GUILayoutUtility.GetRect(GUIContent.none, GUIUtils.InspectorTitle());
            EditorGUI.DrawRect(titleRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            EditorGUI.LabelField(titleRect, LC.Combine("Reddot.RedDotMonitor"), GUIUtils.InspectorTitle());

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(LC.Combine("NoDataOutsidePlayMode"), MessageType.Info);
                return;
            }

            if (_target.Nodes == null)
            {
                EditorGUILayout.HelpBox(LC.Combine("NoDataOutsidePlayMode"), MessageType.Info);
                return;
            }

            _bgStyle ??= new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(20, 20, 10, 10)
            };
            UpdateRootNodes();
            DrawTree();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine("RefreshManually"), GUILayout.Width(80)))
                Repaint();
            EditorGUILayout.LabelField(LC.Combine("AutoRefreshInterval") + $": {RepaintInterval:F1} s",
                GUIUtils.SmallNote());
            EditorGUILayout.EndHorizontal();
        }

        private void UpdateRootNodes()
        {
            _rootNodes.Clear();
            _nodesKeySet.Clear();
            var nodes = _target.Nodes;
            if (nodes == null) return;

            foreach (var node in nodes)
            {
                if (node == null) continue;
                _nodesKeySet.Add(node.Key);
                if (node.Parent == null)
                    _rootNodes.Add(node);
            }

            _rootNodes.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            // 清理已不存在的 foldout 条目（手写收集，避免 Linq 分配）
            _staleKeys.Clear();
            foreach (var kv in _foldouts)
            {
                if (!_nodesKeySet.Contains(kv.Key))
                    _staleKeys.Add(kv.Key);
            }
            for (int i = 0; i < _staleKeys.Count; i++)
                _foldouts.Remove(_staleKeys[i]);
        }

        private void DrawTree()
        {
            if (_rootNodes.Count == 0)
            {
                EditorGUILayout.LabelField(LC.Combine("Reddot.NotRegistered"), EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EnsureStyles();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.LabelField(LC.Combine("Reddot.NodeKey"), EditorStyles.boldLabel, GUILayout.MinWidth(150));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(LC.Combine("Reddot.NodeDisplayType"), _headerCenterStyle, GUILayout.Width(120));
            EditorGUILayout.LabelField(LC.Combine("Reddot.NodeValue"), _headerCenterStyle, GUILayout.Width(100));
            GUILayout.Space(20);
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginVertical(_bgStyle);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.MaxHeight(500f));

            foreach (var root in _rootNodes)
            {
                DrawNode(root, 0);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        // 首次使用时构建样式表（只建一次）
        private void EnsureStyles()
        {
            if (_typeStyles != null)
                return;

            var colors = new[]
            {
                new Color(0.4f, 0.8f, 0.4f), // Dot
                new Color(1f, 0.6f, 0.2f),   // Number
                new Color(0.3f, 0.7f, 1f),   // Image
                new Color(0.8f, 0.4f, 0.8f)  // ImageNumber
            };
            _typeStyles = new GUIStyle[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                _typeStyles[i] = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = colors[i] }
                };
            }

            _numActiveStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = GUIUtils.LightYellow }
            };
            _numIdleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.gray }
            };
            _headerCenterStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void DrawNode(RedDotNode node, int depth)
        {
            if (node == null) return;

            const int indentStep = 20;
            int indent = depth * indentStep;
            string key = node.Key;
            bool hasChildren = node.Children.Count > 0;

            _foldouts.TryAdd(key, true);

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(indent);

            if (hasChildren)
                _foldouts[key] = EditorGUILayout.Foldout(_foldouts[key], key, true);
            else
                EditorGUILayout.LabelField(key, GUILayout.MinWidth(100), GUILayout.ExpandWidth(true));

            // 将类型和数值推到右侧
            GUILayout.FlexibleSpace();

            // 类型标签（带颜色，固定宽度）——复用缓存样式
            EnsureStyles();
            int typeIndex = (int)node.DisplayType;
            var typeStyle = typeIndex >= 0 && typeIndex < _typeStyles.Length
                ? _typeStyles[typeIndex]
                : _typeStyles[0];
            EditorGUILayout.LabelField($"{node.DisplayType}", typeStyle, GUILayout.Width(120));

            var numStyle = node.Number > 0 ? _numActiveStyle : _numIdleStyle;
            EditorGUILayout.LabelField($" {node.Number}", numStyle, GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();

            // 递归子节点
            if (!hasChildren || !_foldouts[key])
                return;

            foreach (var child in node.Children)
                DrawNode(child, depth + 1);
        }
    }
}
#endif