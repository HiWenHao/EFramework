/*
 * ================================================
 * Describe:      EF配置面板 - 可通过快捷键 Alt + E 快速打开
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-10 14:39:19
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 16:47:08
 * ScriptVersion: 0.1
 * ===============================================
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// EF配置面板 - 可通过快捷键 Alt + E 快速打开  
    /// </summary>
    public class EFConfigsPanel : EditorWindowBase
    {
        private int _panelIndex = -1;
        private string _assetsPath;
        private Vector2 _scrollPositionL;

        private static EFConfigsPanel _window;
        private List<EFConfigPanelBase> _settings;
        private Dictionary<Type, int> _priorityCache;

        [MenuItem(MenuItemToolkit.Settings, priority = MenuItemToolkit.SettingPriority)]
        private static void OpenWindow()
        {
            Open<EFProjectConfigPanel>();
        }

        protected override void LoadWindowData()
        {
            if (null != _settings)
                return;

            _settings = new List<EFConfigPanelBase>();
            _priorityCache = new Dictionary<Type, int>();
            var allTypesWithAttr = TypeCache.GetTypesWithAttribute<EFConfigPanelAttribute>();

            foreach (Type oneType in allTypesWithAttr)
            {
                if (oneType.IsAbstract || !typeof(EFConfigPanelBase).IsAssignableFrom(oneType))
                    continue;

                if (oneType.GetConstructor(Type.EmptyTypes) == null)
                {
                    D.Warning($"{oneType.Name} 缺少无参构造函数，跳过实例化");
                    continue;
                }

                Insert(_settings, Activator.CreateInstance(oneType) as EFConfigPanelBase);
            }

            _panelIndex = _panelIndex == -1 ? 0 : _panelIndex;
            _settings[_panelIndex].LoadWindowData();
        }

        protected override void OnSmartGUI()
        {
            if (IsRefreshing)
                return;

            EditorGUILayout.BeginHorizontal();

            #region Left menu

            _scrollPositionL = EditorGUILayout.BeginScrollView(_scrollPositionL, GUILayout.Width(140f),
                GUILayout.Height(position.height));
            EditorGUILayout.Space();
            int length = _settings.Count;
            for (int i = 0; i < length; i++)
            {
                DrawButton(i, _settings[i]);
            }

            EditorGUILayout.EndScrollView();

            #endregion

            GUILayout.Box(GUIContent.none, "hostview", GUILayout.Width(10f));

            #region Right contents

            EditorGUILayout.BeginVertical("hostview");

            #region Title

            EditorGUILayout.LabelField(_settings[_panelIndex].Name, GUIUtils.Title(), GUILayout.Height(35f));
            GUILayout.Box(GUIContent.none, GUILayout.Height(3.0f), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();

            #endregion

            _settings[_panelIndex].OnGUI();
            EditorGUILayout.EndVertical();

            #endregion

            EditorGUILayout.EndHorizontal();
        }

        private void OnDestroy()
        {
            if (null == _settings)
                return;
            int length = _settings.Count;
            for (int i = 0; i < length; i++)
            {
                if (null == _settings[i])
                    continue;
                _settings[i].OnDestroy();
            }

            _priorityCache.Clear();
            _priorityCache = null;

            _settings.Clear();
            _settings = null;
        }

        private void DrawButton(int index, EFConfigPanelBase configPanel)
        {
            if (!GUILayout.Button(configPanel.Name,
                    new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft
                    }))
                return;

            _panelIndex = index;
            configPanel.OnEnable(_assetsPath);
        }

        private int GetOrder(EFConfigPanelBase configPanel)
        {
            Type type = configPanel.GetType();

            if (_priorityCache.TryGetValue(type, out int cached))
                return cached;
            var attr = type.GetCustomAttribute<EFConfigPanelAttribute>(true);
            int order = attr?.Priority ?? -1;
            _priorityCache[type] = order;
            return order;
        }

        // 按 Priority 插入到列表中（小的在前）
        private void Insert<T>(List<T> list, T item)
        {
            int priority = GetOrder(item as EFConfigPanelBase);
            int index = 0;
            for (; index < list.Count; index++)
            {
                if (priority < GetOrder(list[index] as EFConfigPanelBase))
                    break;
            }

            list.Insert(index, item);
        }

        /// <summary>
        /// 打开页面
        /// </summary>
        public static void Open<T>() where T : EFConfigPanelBase
        {
            if (!ConfigManager.Project)
            {
                D.Log("ProjectConfig has been created successfully. Please reopen this panel.");
                return;
            }

            if (null == _window)
            {
                _window = CreateInstance<EFConfigsPanel>(); // GetWindow<EFConfigsPanel>(false, "EF Settings");
                _window.titleContent = new GUIContent("EF " + LC.Combine(Lc.Config));
                _window.minSize = new Vector2(650.0f, 350.0f);
                _window.Show();
                _window.Focus();
            }

            Type type = typeof(T);
            foreach (var configPanel in _window._settings)
            {
                if (configPanel.GetType() != type) continue;
                _window._panelIndex = _window._settings.IndexOf(configPanel);
                configPanel.OnEnable(_window._assetsPath);
                break;
            }
        }
    }
}

#endif