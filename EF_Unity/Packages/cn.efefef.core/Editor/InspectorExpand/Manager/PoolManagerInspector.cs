/*
 * ================================================
 * Describe:      用来显示池系统的当前使用情况
 * Author:        Alvin5100
 * CreationTime:  2026-05-11 10:40:50
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-11 10:40:50
 * ScriptVersion: 0.1
 * ===============================================
 */

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Managers.Pool.Editor
{
    [CustomEditor(typeof(PoolManager))]
    public class PoolManagerInspector : UnityEditor.Editor
    {
        private PoolManager _target;
        private Vector2 _goScrollPos;      // GameObject 池区域滚动位置
        private Vector2 _objScrollPos;     // 通用对象池区域滚动位置

        private double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.5;

        private FieldInfo _objectPoolsField;
        private FieldInfo _gameObjectPoolsField;

        private readonly List<ObjectPoolInfo> _objectPoolInfos = new();
        private readonly List<GameObjectPoolInfo> _gameObjectPoolInfos = new();

        private bool _openDebug;
        private bool _showObjectPools = true;
        private bool _showGameObjectPools = true;

        private GUIStyle _cardStyle;
        private GUIStyle _sectionHeaderStyle;

        private void OnEnable()
        {
            _target = (PoolManager)target;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
            InitReflection();
            RefreshData();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void InitReflection()
        {
            _gameObjectPoolsField = typeof(PoolManager).GetField("_gameObjectPools", BindingFlags.NonPublic | BindingFlags.Instance);
            _objectPoolsField = typeof(PoolManager).GetField("_objectPools", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            RefreshData();
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (!EditorApplication.isPlaying) return;
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastRefreshTime >= REFRESH_INTERVAL)
            {
                _lastRefreshTime = now;
                RefreshData();
                Repaint();
            }
        }

        private void RefreshData()
        {
            var oldGoFoldouts = new Dictionary<string, bool>();
            foreach (var info in _gameObjectPoolInfos)
                oldGoFoldouts[info.PrefabName] = info.Foldout;
            var oldObjFoldouts = new Dictionary<string, bool>();
            foreach (var info in _objectPoolInfos)
                oldObjFoldouts[info.TypeName] = info.Foldout;

            _gameObjectPoolInfos.Clear();
            _objectPoolInfos.Clear();

            if (_target == null || !EditorApplication.isPlaying)
                return;

            try
            {
                if (_gameObjectPoolsField?.GetValue(_target) is IDictionary goDict)
                {
                    foreach (DictionaryEntry entry in goDict)
                    {
                        var prefab = entry.Key as GameObject;
                        var pool = entry.Value as GameObjectPool;
                        if (prefab == null || pool == null) continue;

                        _gameObjectPoolInfos.Add(new GameObjectPoolInfo
                        {
                            PrefabName = prefab.name,
                            Count = pool.Count,
                            ActiveCount = pool.ActiveCount,
                            TotalCount = pool.TotalCount,
                            MaxSize = GetGameObjectPoolMaxSize(pool),
                            IdleTimeout = GetGameObjectPoolIdleTimeout(pool),
                            OpenDebug = pool.OpenDebug,
                            IsAlive = pool.IsAlive,
                            Foldout = oldGoFoldouts.GetValueOrDefault(prefab.name, false)
                        });
                    }
                }

                var objDict = _objectPoolsField?.GetValue(_target) as IDictionary;
                if (objDict != null)
                {
                    foreach (DictionaryEntry entry in objDict)
                    {
                        var type = entry.Key as Type;
                        var poolObj = entry.Value;
                        if (type == null || poolObj == null) continue;

                        _objectPoolInfos.Add(new ObjectPoolInfo
                        {
                            TypeName = GetCleanTypeName(type),
                            Count = GetObjectPoolCount(poolObj),
                            MaxSize = GetObjectPoolMaxSize(poolObj),
                            Foldout = oldObjFoldouts.GetValueOrDefault(GetCleanTypeName(type), false)
                        });
                    }
                }
            }
            catch (Exception) { /* ignore */ }
        }

        // ---- 反射辅助 ----
        private int GetGameObjectPoolMaxSize(GameObjectPool pool)
        {
            var f = typeof(GameObjectPool).GetField("_maxSize", BindingFlags.NonPublic | BindingFlags.Instance);
            int v = f != null ? (int)f.GetValue(pool) : int.MaxValue;
            return v == int.MaxValue ? -1 : v;
        }
        private float GetGameObjectPoolIdleTimeout(GameObjectPool pool)
        {
            var f = typeof(GameObjectPool).GetField("_idleTimeout", BindingFlags.NonPublic | BindingFlags.Instance);
            return f != null ? (float)f.GetValue(pool) : -1f;
        }
        private int GetObjectPoolCount(object pool)
        {
            var stackField = pool.GetType().GetField("_stack", BindingFlags.NonPublic | BindingFlags.Instance);
            if (stackField == null) return 0;
            var stack = stackField.GetValue(pool);
            if (stack == null) return 0;
            var countProp = stack.GetType().GetProperty("Count");
            return countProp != null ? (int)countProp.GetValue(stack) : 0;
        }
        private int GetObjectPoolMaxSize(object pool)
        {
            var f = pool.GetType().GetField("_maxSize", BindingFlags.NonPublic | BindingFlags.Instance);
            int v = f != null ? (int)f.GetValue(pool) : int.MaxValue;
            return v == int.MaxValue ? -1 : v;
        }
        private string GetCleanTypeName(Type type)
        {
            if (!type.IsGenericType) return type.Name;
            string name = type.Name.Substring(0, type.Name.IndexOf('`'));
            var args = type.GetGenericArguments();
            return $"{name}&lt;{string.Join(",", (object[])args)}&gt;";
        }

        // ---- GUI ----
        public override void OnInspectorGUI()
        {
            InitStyles();
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            var titleRect = GUILayoutUtility.GetRect(GUIContent.none, GUIUtils.InspectorTitle());
            EditorGUI.DrawRect(titleRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            EditorGUI.LabelField(titleRect, LC.Combine(Lc.Pool, Lc.Data, Lc.Monitor ), GUIUtils.InspectorTitle());

            
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox( LC.Combine(Lc.Non, Lc.Running, Lc.Not, Lc.Data), MessageType.Info);
                return;
            }

            if (_gameObjectPoolsField == null || _objectPoolsField == null)
            {
                EditorGUILayout.HelpBox(LC.Combine(Lc.Reflect, Lc.Error, Lc.Please, Lc.Check, Lc.Code), MessageType.Error);
                return;
            }

            _openDebug = EditorGUILayout.ToggleLeft(LC.Combine(Lc.Open, Lc.Debug), _openDebug);
            _target.SetOpenDebug(_openDebug);
            if (_openDebug && GUILayout.Button("Dump Leaks"))
            {
                _target.DumpAllLeaks();
            }
            
            // GameObject 对象池折叠区域（独立滚动条 + 高度限制）
            _showGameObjectPools = EditorGUILayout.Foldout(_showGameObjectPools, GUIUtils.IconText($"GameObjectPool - ({_gameObjectPoolInfos.Count})", "d_GameObject Icon"), true, _sectionHeaderStyle);
            if (_showGameObjectPools && _gameObjectPoolInfos.Count > 0)
            {
                EditorGUILayout.Space();
                float goContentHeight = _gameObjectPoolInfos.Count * 85f; // 每个卡片85px，无需额外加间距（卡片自带5px底部）
                float goRegionHeight = Mathf.Clamp(goContentHeight, 150f, 850f);
                _goScrollPos = EditorGUILayout.BeginScrollView(_goScrollPos, GUILayout.Height(goRegionHeight));
                foreach (var info in _gameObjectPoolInfos)
                    DrawGameObjectPoolCard(info);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(12);

            // 通用对象池折叠区域（独立滚动条 + 高度限制）
            _showObjectPools = EditorGUILayout.Foldout(_showObjectPools, GUIUtils.IconText($"ObjectPool - ({_objectPoolInfos.Count})", "cs Script Icon"), true, _sectionHeaderStyle);
            if (_showObjectPools && _objectPoolInfos.Count > 0)
            {
                EditorGUILayout.Space();
                float objContentHeight = _objectPoolInfos.Count * 70f;
                float objRegionHeight = Mathf.Clamp(objContentHeight, 150f, 680f);
                _objScrollPos = EditorGUILayout.BeginScrollView(_objScrollPos, GUILayout.Height(objRegionHeight));
                foreach (var info in _objectPoolInfos)
                    DrawObjectPoolCard(info);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(LC.Combine(Lc.Auto, Lc.Refresh, Lc.Interval) + $": {REFRESH_INTERVAL} s", GUIUtils.SmallNote());
        }

        private void DrawGameObjectPoolCard(GameObjectPoolInfo info)
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            
            // 标题行：预制体名称 + 状态点
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(info.PrefabName, GUIUtils.Text(13, FontStyle.Bold));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // 核心数据行
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15);
            EditorGUILayout.LabelField($"{LC.Combine(Lc.Idle)}: {info.Count}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"{LC.Combine(Lc.Active)}: {info.ActiveCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"{LC.Combine(Lc.Total)}: {info.TotalCount}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            float activePercent = info.TotalCount > 0 ? (float)info.ActiveCount / info.TotalCount : 0f;
            DrawProgressBar(activePercent, new Color(0.4f, 0.7f, 1f), LC.Combine(Lc.Activity, Lc.Rate));
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15);
            EditorGUILayout.LabelField(LC.Combine(Lc.Max, Lc.Idle, Lc.Count) + $": {(info.MaxSize == -1 ? "∞" : info.MaxSize.ToString())}", GUILayout.Width(120));
            EditorGUILayout.LabelField(LC.Combine(Lc.Idle, Lc.Timeout) + $": {(info.IdleTimeout > 0 ? info.IdleTimeout + "s" : LC.Combine(Lc.Disable))}", GUILayout.Width(120));
            EditorGUILayout.LabelField(LC.Combine(Lc.Debug) + $": {(info.OpenDebug ? "√" : "X")}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawObjectPoolCard(ObjectPoolInfo info)
        {
            EditorGUILayout.BeginVertical(_cardStyle);
            
            EditorGUILayout.LabelField(info.TypeName, GUIUtils.Text(13, FontStyle.Bold));
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(15);
            EditorGUILayout.LabelField(LC.Combine(Lc.Idle) + $": {info.Count}", GUILayout.Width(100));
            EditorGUILayout.LabelField(LC.Combine(Lc.Max, Lc.Capacity) + (info.MaxSize == -1 ? "∞" : $"{info.MaxSize}"), GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();
            
            float idlePercent = info.MaxSize > 0 ? (float)info.Count / info.MaxSize : 0f;
            DrawProgressBar(idlePercent, new Color(0.6f, 0.9f, 0.4f), LC.Combine(Lc.Idle, Lc.Occupancy, Lc.Rate));
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        private void DrawProgressBar(float percent, Color barColor, string label)
        {
            Rect rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
            rect.height = 16;
            
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(percent), rect.height);
            EditorGUI.DrawRect(fillRect, barColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), Color.gray);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + rect.height - 1, rect.width, 1), Color.gray);
            GUI.Label(rect, $"{label}: {percent * 100:F1}%", new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });
        }

        private void InitStyles()
        {
            _sectionHeaderStyle ??= new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 20,
                padding = new RectOffset(16, 0, 2, 2)
            };
            _cardStyle ??= new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(5, 5, 3, 3),
                normal = { background = MakeTex(1, 1, new Color(0.25f, 0.25f, 0.25f, 0.4f)) }
            };
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = col;
            }

            var tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private class GameObjectPoolInfo
        {
            public string PrefabName;
            public int Count, ActiveCount, TotalCount, MaxSize;
            public float IdleTimeout;
            public bool OpenDebug, IsAlive;
            public bool Foldout;
        }

        private class ObjectPoolInfo
        {
            public string TypeName;
            public int Count, MaxSize;
            public bool Foldout;
        }
    }
}
#endif