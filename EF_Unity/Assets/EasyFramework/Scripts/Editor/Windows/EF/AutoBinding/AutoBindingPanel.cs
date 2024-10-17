/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-17 11:31:01
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-10-17 11:31:01
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.Edit;
using EasyFramework.Edit.AutoBind;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Windows
{
    namespace SettingPanel
    {
        /// <summary>
        /// Please modify the description。
        /// </summary>
        internal class AutoBindingPanel : EFSettingBase
        {
            Vector2 m_ScrllPos;

            private SerializedProperty m_Namespace;
            private SerializedProperty m_RulePrefixes;
            private SerializedObject m_CustomSettings;

            public AutoBindingPanel(string name) : base(name)
            {
            }

            internal override void OnEnable(string assetsPath)
            {
                if (m_IsInitialzed)
                    return;
                m_IsInitialzed = true;

                AutoBindSetting m_Setting = EditorUtils.LoadSettingAtPath<AutoBindSetting>();
                m_CustomSettings = new SerializedObject(m_Setting);
                m_Namespace = m_CustomSettings.FindProperty("m_Namespace");
                m_RulePrefixes = m_CustomSettings.FindProperty("m_RulePrefixes");
            }

            internal override void OnGUI()
            {
                m_CustomSettings.Update();
                using var changeCheckScope = new EditorGUI.ChangeCheckScope();

                m_Namespace.stringValue = EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Default, Lc.Script, Lc.Namespace }), m_Namespace.stringValue);

                EditorGUILayout.Space(12f, true);
                
                m_ScrllPos = EditorGUILayout.BeginScrollView(m_ScrllPos);
                EditorGUILayout.PropertyField(m_RulePrefixes);
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                if (!changeCheckScope.changed) return;
                m_CustomSettings.ApplyModifiedPropertiesWithoutUndo();
                m_CustomSettings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            internal override void OnDestroy()
            {

            }
        }
	}
}
