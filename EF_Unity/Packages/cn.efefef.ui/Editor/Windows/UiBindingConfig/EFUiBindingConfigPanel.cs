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

namespace EasyFramework.Edit.Windows.ConfigPanel
{
    /// <summary>
    /// UI自动绑定设置
    /// </summary>
    [EFConfigPanel]
    internal class EFUiBindingConfigPanel : EFConfigPanelBase
    {
        Vector2 _scrollPos;

        private SerializedProperty _namespace;
        private SerializedProperty _rulePrefixes;
        private SerializedObject _customSettings;
        
        public override string Name => LC.Combine(new Lc[] { Lc.Code, Lc.Auto, Lc.Bind });

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }

        public override void LoadWindowData()
        {
            _customSettings = new SerializedObject(UiBindingConfig.Instance);
            _namespace = _customSettings.FindProperty("_namespace");
            _rulePrefixes = _customSettings.FindProperty("_rulePrefixes");
        }

        public override void OnGUI()
        {
            _customSettings.Update();
            using var changeCheckScope = new EditorGUI.ChangeCheckScope();

            _namespace.stringValue =
                EditorGUILayout.TextField(LC.Combine(new Lc[] { Lc.Default, Lc.Script, Lc.Namespace }),
                    _namespace.stringValue);

            EditorGUILayout.Space(12f, true);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.PropertyField(_rulePrefixes);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            if (!changeCheckScope.changed) return;

            _customSettings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}
