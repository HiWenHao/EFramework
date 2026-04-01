/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-10-17 11:31:01
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 16:47:08
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.Windows.SettingPanel
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

        public AutoBindingPanel(string name, AutoBindSetting target) : base(name, target)
        {
        }

        internal override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }

        internal override void LoadWindowData()
        {
            _customSettings = new SerializedObject(TargetScriptable);
            _namespace = _customSettings.FindProperty("_namespace");
            _rulePrefixes = _customSettings.FindProperty("_rulePrefixes");
        }

        internal override void OnGUI()
        {
            _customSettings.Update();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();

            _namespace.stringValue =
                EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Default, Lc.Script, Lc.Namespace }),
                    _namespace.stringValue);

            EditorGUILayout.Space(12f, true);

            _scrllPos = EditorGUILayout.BeginScrollView(_scrllPos);
            EditorGUILayout.PropertyField(_rulePrefixes);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            if (!changeCheckScope.changed) return;

            _customSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}
