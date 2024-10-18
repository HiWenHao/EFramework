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
            private string m_AssetsPath;
            private Vector2 m_ScrollPostionL;

            private EFSettingBase m_CurrentPanel;

            private EFSettingBase m_EFSetting;
            private EFSettingBase m_PathConfig;
            private EFSettingBase m_AssetsSwitch;
            private EFSettingBase m_UIAutoBinding;

            [MenuItem("EFTools/Settings &E", priority = 0)]
            private static void OpenWindow()
            {
                EFSettingsPanel window = GetWindow<EFSettingsPanel>(false, "EF Settings");
                window.minSize = new Vector2(650.0f, 350.0f);
                window.Show();
            }

            private void OnEnable()
            {
                if (null == m_CurrentPanel)
                {
                    m_AssetsPath = Utility.Path.GetEFAssetsPath();

                    m_PathConfig = new PathConfigPanel(LC.Combine(new Lc[] { Lc.Path, Lc.Config }));
                    m_EFSetting = new EFProjectPanel(LC.Combine(new Lc[] { Lc.Project, Lc.Settings }));
                    m_AssetsSwitch = new AssetsSwitch(LC.Combine(new Lc[] { Lc.Assets, Lc.Config, Lc.Switch }));
                    m_UIAutoBinding = new AutoBindingPanel(LC.Combine(new Lc[] { Lc.Code, Lc.Auto, Lc.Bind }));

                    m_CurrentPanel = m_EFSetting;
                    m_CurrentPanel.OnEnable(m_AssetsPath);
                }
            }

            private void OnGUI()
            {
                EditorGUILayout.BeginHorizontal();
                #region Left menu
                m_ScrollPostionL =  EditorGUILayout.BeginScrollView(m_ScrollPostionL, GUILayout.Width(180f), GUILayout.Height(position.height));
                EditorGUILayout.Space();
                DrawButton(m_EFSetting);
                DrawButton(m_PathConfig);
                DrawButton(m_AssetsSwitch);
                DrawButton(m_UIAutoBinding);
                EditorGUILayout.EndScrollView();
                #endregion

                GUILayout.Box(GUIContent.none, "hostview", GUILayout.Width(10f));

                #region Right contents
                EditorGUILayout.BeginVertical("hostview");
                #region Title
                EditorGUILayout.LabelField(m_CurrentPanel.Name, new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 24
                }, GUILayout.Height(35f));
                GUILayout.Box(GUIContent.none, GUILayout.Height(3.0f), GUILayout.ExpandWidth(true));
                EditorGUILayout.Space();
                #endregion
                m_CurrentPanel.OnGUI();
                EditorGUILayout.EndVertical();
                #endregion
                EditorGUILayout.EndHorizontal();
            }

            private void OnDestroy()
            {
                m_CurrentPanel.OnDestroy();
            }

            void DrawButton(EFSettingBase setting)
            {
                if (GUILayout.Button(setting.Name, 
                    new GUIStyle(GUI.skin.button)
                    {
                        alignment = TextAnchor.MiddleLeft
                    }))
                {
                    setting.OnEnable(m_AssetsPath);
                    m_CurrentPanel = setting;
                }
            }
        }
    }
}
