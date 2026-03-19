/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-10 14:39:19
 * ModifyAuthor:  Alvin.Wang(Wenhao)
 * ModifyTime:    2026-03-17 13:57:26
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace SettingPanel
    {
        /// <summary>
        /// Please modify the description。
        /// </summary>
        public class EFSettingsPanel : EditorWindow
        {
            private int _panelIndex = -1;
            private bool _inited;
            private string _assetsPath;
            private Vector2 _scrollPositionL;

            private EFSettingBase[] _settings;

            [MenuItem("EFTools/Settings &E", priority = 0)]
            private static void OpenWindow()
            {
                Open(0);
            }

            private void OnGUI()
            {
                EditorGUILayout.BeginHorizontal();
                #region Left menu
                _scrollPositionL =  EditorGUILayout.BeginScrollView(_scrollPositionL, GUILayout.Width(140f), GUILayout.Height(position.height));
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
                int length = _settings.Length;
                for (int i = 0; i < length; i++)
                {
                    if (null == _settings[i])
                        continue;
                    _settings[i].OnDestroy();
                }

                _settings = null;
            }

            void DrawButton(int index, EFSettingBase setting)
            {
                if (GUILayout.Button(setting.Name, 
                    new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft
                    }))
                {
                    _panelIndex = index;
                    setting.OnEnable(_assetsPath);
                }
            }

            void Init()
            {
                if (_panelIndex >= 0 || _inited) return;
                _inited = true;
                _assetsPath = Utility.Path.GetEfAssetsPath();

                _settings = new[] {
                    new EFProjectPanel(LC.Combine(new Lc[] { Lc.Project, Lc.Settings })) as EFSettingBase,
                    new PathConfigPanel(LC.Combine(new Lc[] { Lc.Path, Lc.Config })),
                    //new AssetsSwitch(LC.Combine(new Lc[] { Lc.Assets, Lc.Config, Lc.Switch })),
                    new AutoBindingPanel(LC.Combine(new Lc[] { Lc.Code, Lc.Auto, Lc.Bind }))
                };

                _panelIndex = 0;
            }

            /// <summary>
            /// 根据索引打开页面
            /// </summary>
            /// <param name="pageIndex">页面索引</param>
            public static void Open(int pageIndex)
            {
                EFSettingsPanel window = GetWindow<EFSettingsPanel>(false, "EF Settings");
                window.minSize = new Vector2(650.0f, 350.0f);
                window.Show();
                window.Init();
                window._panelIndex = pageIndex;
                window._settings[pageIndex].OnEnable(window._assetsPath);
            }
        }
    }
}
