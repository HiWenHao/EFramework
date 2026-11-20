/*
 * ================================================
 * Describe:      This script is used to inspect all registered singletons at runtime.
 * Author:        Alvin8412
 * CreationTime:  2026-11-15 10:45:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-15 10:45:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows
{
    /// <summary>
    /// 单例检查器：列出 <see cref="EFC"/> 注册表里的全部单例，反射展开字段。
    /// <para>Singleton inspector: lists every singleton registered in EFC and reflects its fields.</para>
    /// </summary>
    /// <remarks>
    /// 原理与 ET 的组件检查器一致：<b>全局可达的对象图 + 反射</b>。
    /// EFC 的注册表本身就是那张图，所以这里不需要遍历场景。
    /// 只在 Play 模式下有数据；真机上没有 EditorWindow，要看数据得另做运行时面板。
    /// </remarks>
    public sealed class SingletonInspectorWindow : EditorWindowBase
    {
        #region 常量

        private const float AutoRefreshInterval = 0.5f;
        private const int MaxStringLength = 120;

        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        #endregion

        #region 字段

        private readonly Dictionary<Type, FieldInfo[]> _fieldCache = new Dictionary<Type, FieldInfo[]>();
        private readonly Dictionary<Type, bool> _expanded = new Dictionary<Type, bool>();

        private List<ISingleton> _snapshot = new List<ISingleton>();
        private string _filter = string.Empty;
        private bool _autoRefresh = true;
        private bool _showNonPublic = true;
        private Vector2 _scroll;
        private double _nextRefresh;

        #endregion

        #region 入口

        [MenuItem(MenuItemToolkit.Tools + "🔍 Singleton Inspector", false, MenuItemToolkit.ToolsPriority + 20)]
        private static void Open()
        {
            var window = GetWindow<SingletonInspectorWindow>();
            if (window == null)
                return;

            window.titleContent = new GUIContent(LC.Tr("单例检查器", "Singleton Inspector"));
            window.minSize = new Vector2(460f, 280f);
            window.Show();
        }

        #endregion

        #region 生命周期

        protected override void LoadWindowData()
        {
            Refresh();
        }

        protected override void OnSmartGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(LC.Tr(
                    "仅在 Play 模式下有数据：单例是运行时创建的。",
                    "Data is only available in Play mode; singletons are created at runtime."), MessageType.Info);
                return;
            }

            DrawList();
            TickAutoRefresh();
        }

        #endregion

        #region 绘制

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button(LC.Tr("刷新", "Refresh"), EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    Refresh();

                _autoRefresh = GUILayout.Toggle(_autoRefresh, LC.Tr("自动", "Auto"),
                    EditorStyles.toolbarButton, GUILayout.Width(50f));

                bool showNonPublic = GUILayout.Toggle(_showNonPublic, LC.Tr("含私有", "Non-public"),
                    EditorStyles.toolbarButton, GUILayout.Width(70f));
                if (showNonPublic != _showNonPublic)
                {
                    _showNonPublic = showNonPublic;
                    _fieldCache.Clear(); // 字段列表受该开关影响，切换时必须让缓存失效
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label(LC.Tr($"共 {_snapshot.Count} 个", $"{_snapshot.Count} total"),
                    EditorStyles.miniLabel, GUILayout.Width(80f));
            }

            _filter = EditorGUILayout.TextField(LC.Tr("过滤", "Filter"), _filter);
        }

        private void DrawList()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int shown = 0;
            for (int i = 0; i < _snapshot.Count; i++)
            {
                var item = _snapshot[i];
                if (item == null)
                    continue;

                Type type = item.GetType();
                if (!MatchFilter(type))
                    continue;

                shown++;
                DrawEntry(item, type);
            }

            if (shown == 0)
            {
                EditorGUILayout.HelpBox(LC.Tr(
                    "没有匹配的单例。若刚启动，等框架初始化完成后再刷新。",
                    "No singleton matched. If the game just started, refresh after the framework finishes initializing."),
                    MessageType.None);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(ISingleton item, Type type)
        {
            bool expanded = _expanded.TryGetValue(type, out bool state) && state;
            bool next = expanded;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    next = EditorGUILayout.Foldout(expanded, type.Name, true);
                    if (next != expanded)
                        _expanded[type] = next;

                    DrawBadges(type, item);
                }

                if (next)
                    DrawFields(item, type);
            }
        }

        private void DrawBadges(Type type, ISingleton item)
        {
            GUILayout.FlexibleSpace();

            var priority = type.GetCustomAttribute<SingletonPriorityAttribute>(true);
            if (priority != null)
            {
                GUILayout.Label($"{LC.Tr("顺序", "Order")} {priority.Order}", EditorStyles.miniLabel,
                    GUILayout.Width(72f));
            }

            if (item is MonoBehaviour)
            {
                GUILayout.Label("MonoBehaviour", EditorStyles.miniLabel, GUILayout.Width(90f));
            }

            string interfaces = CollectInterfaces(type);
            if (!string.IsNullOrEmpty(interfaces))
                GUILayout.Label(interfaces, EditorStyles.miniLabel);
        }

        private void DrawFields(ISingleton item, Type type)
        {
            FieldInfo[] fields = GetFields(type);
            if (fields.Length == 0)
            {
                EditorGUILayout.LabelField(LC.Tr("（无实例字段）", "(no instance fields)"), EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];

                string label = field.Name;
                string value;
                try
                {
                    value = FormatValue(field.GetValue(item));
                }
                catch (Exception e)
                {
                    value = $"<{LC.Tr("读取失败", "read failed")}: {e.GetType().Name}>";
                }

                EditorGUILayout.LabelField(label, value, EditorStyles.miniLabel);
            }
        }

        #endregion

        #region 数据

        private void Refresh()
        {
            _snapshot = new List<ISingleton>(EFC.GetAllSingletons());
            _nextRefresh = EditorApplication.timeSinceStartup + AutoRefreshInterval;
        }

        private void TickAutoRefresh()
        {
            if (!_autoRefresh || EditorApplication.timeSinceStartup < _nextRefresh)
                return;

            Refresh();
            Repaint();
        }

        private FieldInfo[] GetFields(Type type)
        {
            if (_fieldCache.TryGetValue(type, out var cached))
                return cached;

            var list = new List<FieldInfo>();
            FieldInfo[] all = type.GetFields(FieldFlags);

            for (int i = 0; i < all.Length; i++)
            {
                FieldInfo field = all[i];

                // 编译器生成的字段（<Property>k__BackingField 等）不展示
                if (field.Name.IndexOf('<') >= 0 || field.IsStatic)
                    continue;

                if (!_showNonPublic && !field.IsPublic)
                    continue;

                list.Add(field);
            }

            var result = list.ToArray();
            _fieldCache[type] = result;
            return result;
        }

        private bool MatchFilter(Type type)
        {
            return string.IsNullOrEmpty(_filter) ||
                   type.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string CollectInterfaces(Type type)
        {
            var names = new List<string>();

            if (typeof(IUpdate).IsAssignableFrom(type)) names.Add("IUpdate");
            if (typeof(ILateUpdate).IsAssignableFrom(type)) names.Add("ILate");
            if (typeof(IFixedUpdate).IsAssignableFrom(type)) names.Add("IFixed");
            if (typeof(IOnAppQuit).IsAssignableFrom(type)) names.Add("IQuit");

            return string.Join(" ", names);
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is string text)
                return "\"" + Truncate(text) + "\"";

            if (value is UnityEngine.Object unityObject)
                return unityObject == null
                    ? "<destroyed>"
                    : $"{unityObject.name} ({unityObject.GetType().Name})";

            if (value is ICollection collection)
                return $"{value.GetType().Name} (Count = {collection.Count})";

            if (value is IEnumerable enumerable)
                return $"{value.GetType().Name} {Truncate(enumerable.ToString())}";

            return Truncate(value.ToString());
        }

        private static string Truncate(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text.Length <= MaxStringLength ? text : text.Substring(0, MaxStringLength) + "…";
        }

        #endregion
    }
}
