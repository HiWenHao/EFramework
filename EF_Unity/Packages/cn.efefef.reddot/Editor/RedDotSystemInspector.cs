/*
 * ================================================
 * Describe:      红点系统编辑器监视器 - 树状图（类型/数值右对齐，节点名可点击折叠）
 * Author:        AI Assistant
 * CreationTime:  2026-05-14
 * ScriptVersion: 0.1
 * ===============================================
 */

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Systems.RedDot.Editor
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
            EditorGUI.LabelField(titleRect, LC.Combine(Lc.Red, Lc.Dot, Lc.Monitor), GUIUtils.InspectorTitle());

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Non, Lc.Running, Lc.Not, Lc.Data), MessageType.Info);
                return;
            }

            if (_target.Nodes == null)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Non, Lc.Running, Lc.Not, Lc.Data), MessageType.Info);
                return;
            }

            _bgStyle ??= new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(20, 20, 10, 10)
            };
            UpdateRootNodes();
            DrawTree();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine(Lc.Manually, Lc.Refresh), GUILayout.Width(80)))
                Repaint();
            EditorGUILayout.LabelField(LC.Combine(Lc.Auto, Lc.Refresh, Lc.Interval) + $": {RepaintInterval:F1} s",
                GUIUtils.SmallNote());
            EditorGUILayout.EndHorizontal();
        }

        private void UpdateRootNodes()
        {
            _rootNodes.Clear();
            var nodes = _target.Nodes;
            if (nodes == null) return;

            foreach (var node in nodes)
            {
                if (node is { Parent: null })
                    _rootNodes.Add(node);
            }

            _rootNodes.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            // 清理已不存在的 foldout 条目
            var validKeys = new HashSet<string>();
            foreach (var node in nodes)
                validKeys.Add(node.Key);
            var toRemove = _foldouts.Keys.Where(k => !validKeys.Contains(k)).ToList();
            foreach (var k in toRemove) _foldouts.Remove(k);
        }

        private void DrawTree()
        {
            if (_rootNodes.Count == 0)
            {
                EditorGUILayout.LabelField(LC.Combine(Lc.No, Lc.Register), EditorStyles.centeredGreyMiniLabel);
                return;
            }

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.LabelField(LC.Combine(Lc.Node, Lc.Key), EditorStyles.boldLabel, GUILayout.MinWidth(150));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(LC.Combine(Lc.Node, Lc.Display, Lc.Type), labelStyle, GUILayout.Width(120));
            EditorGUILayout.LabelField(LC.Combine(Lc.Node, Lc.Value), labelStyle, GUILayout.Width(100));
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

            // 类型标签（带颜色，固定宽度）
            Color typeColor = node.DisplayType switch
            {
                RedDotDisplayType.Dot => new Color(0.4f, 0.8f, 0.4f),
                RedDotDisplayType.Number => new Color(1f, 0.6f, 0.2f),
                RedDotDisplayType.Image => new Color(0.3f, 0.7f, 1f),
                RedDotDisplayType.ImageNumber => new Color(0.8f, 0.4f, 0.8f),
                _ => Color.gray
            };
            var typeStyle = new GUIStyle(EditorStyles.label)
            {
                normal =
                {
                    textColor = typeColor
                },
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField($"{node.DisplayType}", typeStyle, GUILayout.Width(120));

            var numStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal =
                {
                    textColor = node.Number > 0 ? GUIUtils.LightYellow : Color.gray
                },
                alignment = TextAnchor.MiddleCenter
            };
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