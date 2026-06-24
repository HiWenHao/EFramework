/*
 * ================================================
 * Describe:      存档系统 Editor 配置面板。继承 EFConfigPanelBase，
 *                集成到 EF Configs 面板中，统一管理加密参数、自动保存策略。
 * Author:        Alvin8412
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-06-24 22:33:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Diagnostics;
using System.IO;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows.ConfigPanel;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Systems.Save.Editor
{
    /// <summary>
    /// 存档系统配置面板。路径：Tools → EF → Configs → Save Settings。
    /// </summary>
    [EFConfigPanel(Priority = 200)]
    public class SaveSettingsConfigPanel : EFConfigPanelBase
    {
        public override string Name => LC.Combine(Lc.Data, Lc.Save, Lc.Config);

        private UnityEditor.Editor _cachedEditor;
        private SaveSettings _settings;

        public override void OnEnable(string assetsPath)
        {
            LoadWindowData();
        }

        public override void LoadWindowData()
        {
            _settings = AssetDatabase.LoadAssetAtPath<SaveSettings>(
                "Assets/Resources/Configs/SaveSettings.asset");

            if (_settings != null)
                _cachedEditor = UnityEditor.Editor.CreateEditor(_settings);
        }

        public override void OnGUI()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox(
                    "SaveSettings.asset not found.\n" +
                    "Create one: Right-click in Project → Create → EF → Save Settings\n" +
                    "Then place it at: Assets/Resources/Configs/SaveSettings.asset",
                    MessageType.Warning);

                if (GUILayout.Button("Create & Place SaveSettings"))
                    CreateDefaultSettings();

                return;
            }

            if (_cachedEditor == null || _cachedEditor.target != _settings)
                _cachedEditor = UnityEditor.Editor.CreateEditor(_settings);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Save System Configuration", EditorStyles.boldLabel);
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

            // Quick actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Locate Settings Asset"))
                EditorGUIUtility.PingObject(_settings);

            if (GUILayout.Button("Open Saves Folder"))
            {
                string path = Path.Combine(Application.persistentDataPath, _settings.fileStorageRoot);
                Process.Start("explorer.exe", path);
            }

            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset Save Settings",
                    "Restore all settings to default values?", "Reset", "Cancel"))
                {
                    var defaults = ScriptableObject.CreateInstance<SaveSettings>();
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

        private void CreateDefaultSettings()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            if (!AssetDatabase.IsValidFolder("Assets/Resources/Configs"))
                AssetDatabase.CreateFolder("Assets/Resources", "Configs");

            var settings = ScriptableObject.CreateInstance<SaveSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Resources/Configs/SaveSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _settings = settings;
            _cachedEditor = UnityEditor.Editor.CreateEditor(_settings);
            EditorGUIUtility.PingObject(settings);

            D.Log("[Save] SaveSettings.asset created at Assets/Resources/Configs/");
        }
    }

    /// <summary>
    /// 存档系统快捷菜单（Tools → EF → Save）。
    /// </summary>
    internal static class SaveMenuItems
    {
        [MenuItem("Tools/EF/Save/Open Config Panel")]
        private static void OpenConfigPanel()
        {
            EFConfigsPanel.Open<SaveSettingsConfigPanel>();
        }

        [MenuItem("Tools/EF/Save/Open Persistent Saves Folder")]
        private static void OpenSavesFolder()
        {
            string path = Path.Combine(Application.persistentDataPath, "Saves");
            Directory.CreateDirectory(path);
            Process.Start("explorer.exe", path);
        }

        [MenuItem("Tools/EF/Save/Clear All Saves (DANGER)")]
        private static void ClearAllSaves()
        {
            if (!EditorUtility.DisplayDialog("Clear All Saves",
                "This will DELETE all save data from disk!\n\n" +
                "This action CANNOT be undone.\n\n" +
                "Are you sure?",
                "Yes, Delete Everything", "Cancel"))
                return;

            string path = Path.Combine(Application.persistentDataPath, "Saves");
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                D.Log("[Save] All save data cleared.");
            }
        }
    }
}
