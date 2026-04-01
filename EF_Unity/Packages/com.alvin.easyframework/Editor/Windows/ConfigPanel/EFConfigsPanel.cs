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
        private EFConfigPanelBase[] _settings;

        [MenuItem("EFTools/Settings &E", priority = 0)]
        private static void OpenWindow()
        {
            Open(0);
        }

        protected override void LoadWindowData()
        {
            _assetsPath = Utility.Path.GetEfAssetsPath();
            ;
            _settings ??= new[]
            {
                new ProjectConfigPanel(LC.Combine(new Lc[] { Lc.Project, Lc.Settings }), ProjectUtility.Project) as
                    EFConfigPanelBase,
                new PathConfigPanel(LC.Combine(new Lc[] { Lc.Path, Lc.Config }), ProjectUtility.Path),
                //new AssetsSwitch(LC.Combine(new Lc[] { Lc.Assets, Lc.Config, Lc.Switch })),
                new AutoBindingPanel(LC.Combine(new Lc[] { Lc.Code, Lc.Auto, Lc.Bind }),
                    EditorUtils.LoadSettingAtPath<AutoBindingConfig>())
            };

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
            int length = _settings.Length;
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
            int length = _settings.Length;
            for (int i = 0; i < length; i++)
            {
                if (null == _settings[i])
                    continue;
                _settings[i].OnDestroy();
            }

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

            if (!configPanel.TargetScriptable)
            {
                D.Warning("Please create assets of the corresponding type, and reopen the window.");
                return;
            }

            _panelIndex = index;
            configPanel.OnEnable(_assetsPath);
        }

        /// <summary>
        /// 根据索引打开页面
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        public static void Open(int pageIndex)
        {
            if (!ProjectUtility.Project)
            {
                D.Log("ProjectConfig has been created successfully. Please reopen this panel.");
                return;
            }

            if (null == _window)
            {
                _window = CreateInstance<EFConfigsPanel>(); // GetWindow<EFConfigsPanel>(false, "EF Settings");
                _window.titleContent = new GUIContent("EF Settings");
                _window.minSize = new Vector2(650.0f, 350.0f);
                _window.Show();
                _window.Focus();
            }

            _window._panelIndex = pageIndex;
            _window._settings[pageIndex].OnEnable(_window._assetsPath);
        }
    }
}
