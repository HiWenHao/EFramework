/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-10 14:39:19
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 16:47:08
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// Please modify the description。
    /// </summary>
    public class EFConfigsPanel : EditorWindowBase
    {
        private int _panelIndex = -1;
        private string _assetsPath;
        private Vector2 _scrollPositionL;

        private static EFConfigsPanel _window;
        private List<EFConfigPanelBase> _settings;

        [MenuItem("EFTools/Settings &E", priority = 0)]
        private static void OpenWindow()
        {
            Open(0);
        }

        protected override void LoadWindowData()
        {
            if (null != _settings)
                return;
            
            _settings = new List<EFConfigPanelBase>();

            Type attribute = typeof(EFConfigAttribute);
            Type[] types = EditorUtils.GetAssembly("EF.Editor").GetTypes();

            foreach (Type oneType in types)
            {
                if (!oneType.IsDefined(attribute, false))
                    continue;

                if (Activator.CreateInstance(oneType) is not EFConfigPanelBase configPanel)
                    continue;
                AddConfigPanel(configPanel);
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

            EditorGUILayout.LabelField(_settings[_panelIndex].Name, new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 24
            }, GUILayout.Height(35f));
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

            _settings.Clear();
            _settings = null;
        }

        private void AddConfigPanel(EFConfigPanelBase config)
        {
            if (config.Priority == -1)
            {
                _settings.Add(config);
                return;
            }
            
            int count = _settings.Count;
            int insertIndex = -1;
            for (int i = 0; i < count; i++)
            {
                int priority = _settings[i].Priority;
                
                if (priority == -1)
                {
                    insertIndex = i;
                    break;
                }

                if (priority <= config.Priority)
                    continue;

                insertIndex = i;
                break;
            }
            
            _settings.Insert(insertIndex, config);
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

        /// <summary>
        /// 根据索引打开页面
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        public static void Open(int pageIndex)
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

            _window._panelIndex = pageIndex;
            _window._settings[pageIndex].OnEnable(_window._assetsPath);
        }
    }
}