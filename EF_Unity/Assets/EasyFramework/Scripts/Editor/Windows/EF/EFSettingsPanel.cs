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
            private EFSettingBase m_CurrentPanel;
            private EFSettingBase m_ManagerSwitch;

            private Vector2 m_ScrollPostionL;
            private Vector2 m_ScrollPostionR;

            [MenuItem("EFTools/Settings/Main Panel &E", priority = 0)]
            private static void OpenWindow()
            {
                EFSettingsPanel window = GetWindow<EFSettingsPanel>(false, "EF Settings");
                window.minSize = new Vector2(600.0f, 350.0f);
                window.Show();
            }

            private void OnEnable()
            {
                if (null == m_CurrentPanel)
                {
                    m_ManagerSwitch = new AssetsSwitch();
                    m_ManagerSwitch.OnEnable();
                    m_CurrentPanel = m_ManagerSwitch;
                }
            }

            private void OnGUI()
            {
                EditorGUILayout.BeginHorizontal();
                #region Left menu
                m_ScrollPostionL =  EditorGUILayout.BeginScrollView(m_ScrollPostionL, GUILayout.Width(180f), GUILayout.Height(position.height));
                //GUILayout.ExpandHeight(true);
                EditorGUILayout.Space();
                if (GUILayout.Button(LC.Combine(new Lc[] { Lc.Assets, Lc.Switch })))
                {
                    m_CurrentPanel = m_ManagerSwitch;
                }
                EditorGUILayout.EndScrollView();
                #endregion

                GUILayout.Box(GUIContent.none, "hostview", GUILayout.Width(10f));

                #region Right contents
                EditorGUILayout.BeginVertical("hostview");
                m_CurrentPanel.OnGUI();
                EditorGUILayout.EndVertical();
                #endregion
                EditorGUILayout.EndHorizontal();
            }

            private void OnDestroy()
            {
                m_CurrentPanel.OnDestroy();
            }
        }
    }
}
