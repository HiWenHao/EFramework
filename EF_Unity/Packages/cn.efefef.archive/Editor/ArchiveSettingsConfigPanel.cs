/*
 * ================================================
 * Describe:      存档系统 Editor 配置面板。继承 EFConfigPanelBase，
 *                集成到 EF Configs 面板中，统一管理加密参数、自动保存策略。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 17:00:00
 * ScriptVersion: 0.1.1
 * ===============================================
 */

using System.IO;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows.ConfigPanel;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Archive
{
    [EFConfigPanel(Priority = 200)]
    public class ArchiveSettingsConfigPanel : EFAssetConfigPanelBase
    {
        public override string Name => LC.Combine("Archive.ArchiveDataConfig");

        private UnityEditor.Editor _cachedEditor; // 缓存的 Settings Editor 实例（避免每帧重建）
        private ArchiveSettings _settings; // 当前存档配置资产

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }

        public override void LoadWindowData()
        {
            if (_settings is not null || !EditorUtils.CheckAssets<ArchiveSettings>(out string path))
                return;
            _settings = EditorUtils.LoadSettingAtPath<ArchiveSettings>(path);
        }

        protected override void OnPanelGUI()
        {
            if (_settings == null)
            {
                _settings = DrawMissingConfigGuide<ArchiveSettings>(LC.Combine("Archive.ArchiveConfigNotExist"),
                    LC.Combine("CreateConfig"));
                return;
            }

            if (_cachedEditor == null || _cachedEditor.target != _settings)
                _cachedEditor = UnityEditor.Editor.CreateEditor(_settings);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(LC.Combine("Archive.SystemConfiguration"), EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(LC.Combine("Archive.SystemConfigHint"), MessageType.Info);

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.Space(5);
                _cachedEditor.OnInspectorGUI();

                if (scope.changed)
                {
                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();
                }
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(LC.Combine("Archive.QuickActions"), EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(LC.Combine("Archive.LocateSettingsAsset")))
                EditorGUIUtility.PingObject(_settings);

            if (GUILayout.Button(LC.Combine("Archive.OpenArchivesFolder")))
            {
                string path = Path.Combine(Application.persistentDataPath, _settings.fileStorageRoot);
                EditorCommands.OpenFolder(path);
            }

            if (GUILayout.Button(LC.Combine("Archive.ResetToDefaults")))
            {
                if (EditorUtility.DisplayDialog(LC.Combine("Archive.ResetSettingsTitle"),
                        LC.Combine("Archive.ResetSettingsHint"), LC.Combine("Reset"), LC.Combine("Cancel")))
                {
                    var defaults = ScriptableObject.CreateInstance<ArchiveSettings>();

                    // 用 CopySerialized 替代逐字段赋值，新增字段不会遗漏
                    EditorUtility.CopySerialized(defaults, _settings);

                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();
                    EditorApplication.delayCall += () => Object.DestroyImmediate(defaults);

                    // 重建 Inspector Editor，确保面板 UI 反映最新字段值
                    if (_cachedEditor != null)
                    {
                        Object.DestroyImmediate(_cachedEditor);
                        _cachedEditor = null;
                    }

                    _cachedEditor = UnityEditor.Editor.CreateEditor(_settings);

                    // 通知父容器重绘
                    EditorApplication.delayCall += () =>
                    {
                        if (_cachedEditor != null)
                            EditorUtility.SetDirty(_settings);
                    };
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        public override void OnDestroy()
        {
            if (_cachedEditor == null) return;
            Object.DestroyImmediate(_cachedEditor);
            _cachedEditor = null;
        }
    }
}