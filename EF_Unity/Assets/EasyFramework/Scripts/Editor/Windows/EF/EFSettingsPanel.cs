/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-10 14:39:19
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-10 14:39:19
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
            private string _assetsPath;
            private Vector2 _scrollPostionL;

            private EFSettingBase _currentPanel;

            private EFSettingBase _efSetting;
            private EFSettingBase _pathConfig;
            private EFSettingBase _assetsSwitch;
            private EFSettingBase _uiAutoBinding;

            [MenuItem("EFTools/Settings &E", priority = 0)]
            private static void OpenWindow()
            {
                EFSettingsPanel window = GetWindow<EFSettingsPanel>(false, "EF Settings");
                window.minSize = new Vector2(650.0f, 350.0f);
                window.Show();
            }

            private void OnEnable()
            {
                if (null == _currentPanel)
                {
                    _assetsPath = Utility.Path.GetEFAssetsPath();

                    _pathConfig = new PathConfigPanel(LC.Combine(new Lc[] { Lc.Path, Lc.Config }));
                    _efSetting = new EFProjectPanel(LC.Combine(new Lc[] { Lc.Project, Lc.Settings }));
                    _assetsSwitch = new AssetsSwitch(LC.Combine(new Lc[] { Lc.Assets, Lc.Config, Lc.Switch }));
                    _uiAutoBinding = new AutoBindingPanel(LC.Combine(new Lc[] { Lc.Code, Lc.Auto, Lc.Bind }));

                    _currentPanel = _efSetting;
                    _currentPanel.OnEnable(_assetsPath);
                }
            }

            private void OnGUI()
            {
                EditorGUILayout.BeginHorizontal();
                #region Left menu
                _scrollPostionL =  EditorGUILayout.BeginScrollView(_scrollPostionL, GUILayout.Width(140f), GUILayout.Height(position.height));
                EditorGUILayout.Space();
                DrawButton(_efSetting);
                DrawButton(_pathConfig);
                DrawButton(_assetsSwitch);
                DrawButton(_uiAutoBinding);
                EditorGUILayout.EndScrollView();
                #endregion

                GUILayout.Box(GUIContent.none, "hostview", GUILayout.Width(10f));

                #region Right contents
                EditorGUILayout.BeginVertical("hostview");
                #region Title
                EditorGUILayout.LabelField(_currentPanel.Name, new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 24
                }, GUILayout.Height(35f));
                GUILayout.Box(GUIContent.none, GUILayout.Height(3.0f), GUILayout.ExpandWidth(true));
                EditorGUILayout.Space();
                #endregion
                _currentPanel.OnGUI();
                EditorGUILayout.EndVertical();
                #endregion
                EditorGUILayout.EndHorizontal();
            }

            private void OnDestroy()
            {
                _currentPanel.OnDestroy();
            }

            void DrawButton(EFSettingBase setting)
            {
                if (GUILayout.Button(setting.Name, 
                    new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft
                    }))
                {
                    setting.OnEnable(_assetsPath);
                    _currentPanel = setting;
                }
            }
        }
    }
}
