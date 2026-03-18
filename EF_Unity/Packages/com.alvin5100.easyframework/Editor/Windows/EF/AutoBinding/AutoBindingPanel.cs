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
            Vector2 _scrllPos;

            private SerializedProperty _namespace;
            private SerializedProperty _rulePrefixes;
            private SerializedObject _customSettings;

            public AutoBindingPanel(string name) : base(name)
            {
            }

            internal override void OnEnable(string assetsPath)
            {
                if (IsInitialzed)
                    return;
                IsInitialzed = true;

                AutoBindSetting setting = EditorUtils.LoadSettingAtPath<AutoBindSetting>();
                _customSettings = new SerializedObject(setting);
                _namespace = _customSettings.FindProperty("_namespace");
                _rulePrefixes = _customSettings.FindProperty("_rulePrefixes");
            }

            internal override void OnGUI()
            {
                _customSettings.Update();
                using var changeCheckScope = new EditorGUI.ChangeCheckScope();

                _namespace.stringValue = EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Default, Lc.Script, Lc.Namespace }), _namespace.stringValue);

                EditorGUILayout.Space(12f, true);
                
                _scrllPos = EditorGUILayout.BeginScrollView(_scrllPos);
                EditorGUILayout.PropertyField(_rulePrefixes);
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                if (!changeCheckScope.changed) return;
                _customSettings.ApplyModifiedPropertiesWithoutUndo();
                _customSettings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
	}
}
