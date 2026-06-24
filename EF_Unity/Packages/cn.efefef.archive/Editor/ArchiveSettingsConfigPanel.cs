/*
 * ================================================
 * Describe:      存档系统 Editor 配置面板。继承 EFConfigPanelBase，
 *                集成到 EF Configs 面板中，统一管理加密参数、自动保存策略。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 00:56:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Diagnostics;
using System.IO;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows.ConfigPanel;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Systems.Archive.Editor
{
    [EFConfigPanel(Priority = 200)]
    public class ArchiveSettingsConfigPanel : EFConfigPanelBase
    {
        public override string Name => LC.Combine(Lc.Archive, Lc.Data, Lc.Config);

        private UnityEditor.Editor _cachedEditor; // 缓存的 Settings Editor 实例（避免每帧重建）
        private ArchiveSettings _settings;         // 当前存档配置资产

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }

        public override void LoadWindowData()
        {
            _settings = AssetDatabase.LoadAssetAtPath<ArchiveSettings>(
                "Assets/Resources/Configs/ArchiveSettings.asset");

            if (_settings != null)
                _cachedEditor = UnityEditor.Editor.CreateEditor(_settings);
        }

        public override void OnGUI()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox(
                    "ArchiveSettings.asset not found.\n" +
                    "Create one: Right-click in Project → Create → EF → Archive Settings\n" +
                    "Then place it at: Assets/Resources/Configs/ArchiveSettings.asset",
                    MessageType.Warning);

                if (GUILayout.Button("Create & Place ArchiveSettings"))
                    CreateDefaultSettings();

                return;
            }

            if (_cachedEditor == null || _cachedEditor.target != _settings)
                _cachedEditor = UnityEditor.Editor.CreateEditor(_settings);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Archive System Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These settings control encryption strength, auto-save behavior, " +
                "backup strategy, and the storage backend. " +
                "Changes take effect on next Play / Build.",
                MessageType.Info);

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

            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Locate Settings Asset"))
                EditorGUIUtility.PingObject(_settings);

            if (GUILayout.Button("Open Archives Folder"))
            {
                string path = Path.Combine(Application.persistentDataPath, _settings.fileStorageRoot);
                Process.Start("explorer.exe", path);
            }

            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset Archive Settings",
                    "Restore all settings to default values?", "Reset", "Cancel"))
                {
                    var defaults = ScriptableObject.CreateInstance<ArchiveSettings>();
                    _settings.maxSlots = defaults.maxSlots;
                    _settings.autoSaveIntervalSeconds = defaults.autoSaveIntervalSeconds;
                    _settings.autoSaveOnlyDirty = defaults.autoSaveOnlyDirty;
                    _settings.encryptionSalt = defaults.encryptionSalt;
                    _settings.aesKeySize = defaults.aesKeySize;
                    _settings.pbkdf2Iterations = defaults.pbkdf2Iterations;
                    _settings.dataVersion = defaults.dataVersion;
                    _settings.warnOnUnknownFields = defaults.warnOnUnknownFields;
                    _settings.enableAutoBackup = defaults.enableAutoBackup;
                    _settings.maxBackupCount = defaults.maxBackupCount;
                    _settings.providerTypeName = defaults.providerTypeName;
                    _settings.fileStorageRoot = defaults.fileStorageRoot;

                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();
                    UnityEngine.Object.DestroyImmediate(defaults);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public override void OnDestroy()
        {
            if (_cachedEditor != null)
            {
                UnityEngine.Object.DestroyImmediate(_cachedEditor);
                _cachedEditor = null;
            }
        }

        // 在 Resources/Configs 下创建默认 ArchiveSettings.asset
        private void CreateDefaultSettings()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder("Assets/Resources/Configs"))
                AssetDatabase.CreateFolder("Assets/Resources", "Configs");

            var settings = ScriptableObject.CreateInstance<ArchiveSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Resources/Configs/ArchiveSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _settings = settings;
            _cachedEditor = UnityEditor.Editor.CreateEditor(_settings);
            EditorGUIUtility.PingObject(settings);

            D.Log("[Archive] ArchiveSettings.asset created at Assets/Resources/Configs/");
        }
    }

    internal static class ArchiveMenuItems
    {
        // 打开 EF Configs 面板中的 Archive Settings 页面
        [MenuItem("EFTools/Archive/Open Config Panel")]
        private static void OpenConfigPanel()
        {
            EFConfigsPanel.Open<ArchiveSettingsConfigPanel>();
        }

        // 在文件资源管理器中打开持久化存档目录
        [MenuItem("EFTools/Archive/Open Persistent Archives Folder")]
        private static void OpenArchivesFolder()
        {
            string path = Path.Combine(Application.persistentDataPath, "Archives");
            Directory.CreateDirectory(path);
            Application.OpenURL("file://" + path);
        }

        // 危险操作：清空磁盘上所有存档数据
        [MenuItem("EFTools/Archive/Clear All Archives (DANGER)")]
        private static void ClearAllArchives()
        {
            if (!EditorUtility.DisplayDialog("Clear All Archives",
                "This will DELETE all archive data from disk!\n\n" +
                "This action CANNOT be undone.\n\n" +
                "Are you sure?",
                "Yes, Delete Everything", "Cancel"))
                return;

            string path = Path.Combine(Application.persistentDataPath, "Archives");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                D.Log("[Archive] All archive data cleared.");
            }
        }
    }
}
