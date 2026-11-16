/*
 * ================================================
 * Describe:      ArchiveManager 运行时监测面板。展示槽位列表、Key 列表、存储状态。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 17:00:00
 * ScriptVersion: 0.1.1
 * ===============================================
 */

using System;
using Cysharp.Threading.Tasks;
using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Systems.Archive.Editor
{
    [CustomEditor(typeof(ArchiveManager))]
    public class ArchiveManagerInspector : EFInspectorBase<ArchiveManager>
    {
        protected override string Title => LC.Combine("Archive.ArchiveDataMonitor");

        private bool _showSlots = true;     // 是否展开槽位折叠区
        private bool _showKeys;             // 是否展开 Key 折叠区
        private string[] _cachedKeys = Array.Empty<string>(); // 缓存的 Key 列表
        private ArchiveSlotMeta[] _cachedSlots = Array.Empty<ArchiveSlotMeta>(); // 缓存的槽位列表

        // 并发保护：避免上一个异步刷新还未完成时再次启动新的刷新
        private bool _refreshInFlight;

        protected override void OnEditorGUI()
        {
            if (Target == null) return;

            if (null == Target.Settings)
            {
                EditorGUILayout.HelpBox(
                    LC.Combine("Archive.ArchiveConfigLoadFailed")
                    + "\n"
                    + LC.Combine("Archive.PleaseCheckConfigByPath")
                    + ": Resources/Configs/ArchiveSettings.asset.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(LC.Combine("Provider"),
                string.IsNullOrEmpty(Target.Settings.providerTypeName)
                    ? LC.Combine("Archive.FileArchiveProviderDefault")
                    : Target.Settings.providerTypeName);
            EditorGUILayout.LabelField(LC.Combine("Archive.AutoSave"),
                $"{Target.Settings.autoSaveIntervalSeconds}s");
            EditorGUILayout.LabelField(LC.Combine("Archive.DataVersion"),
                Target.Settings.dataVersion.ToString());
            EditorGUILayout.LabelField(LC.Combine("Archive.ActiveSlot"),
                Target.ActiveSlot.ToString());

            EditorGUILayout.Space();

            // 槽位列表
            _showSlots = EditorGUILayout.Foldout(_showSlots, LC.Combine(ArmSlotList), true);
            if (_showSlots)
            {
                EditorGUI.indentLevel++;

                var slots = _cachedSlots;
                if (slots == null || slots.Length == 0)
                {
                    EditorGUILayout.LabelField(LC.Combine(ArmSlotEmpty));
                }
                else
                {
                    foreach (var slot in slots)
                    {
                        if (slot.slotId < 0) continue;

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField(
                            $"{LC.Combine("Slot")} {slot.slotId}: {slot.slotName}",
                            EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(LC.Combine("Progress"), slot.progressDescription);
                        EditorGUILayout.LabelField(LC.Combine("Archive.PlayTime"), slot.PlayTimeFormatted);
                        EditorGUILayout.LabelField(LC.Combine("Archive.LastModified"), slot.LastModifiedFormatted);
                        EditorGUILayout.LabelField(LC.Combine("Size"), FormatBytes(slot.totalSizeBytes));
                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUI.indentLevel--;
            }

            // Key 列表
            _showKeys = EditorGUILayout.Foldout(_showKeys, LC.Combine(ArmKeyList), true);
            if (!_showKeys) return;
            EditorGUI.indentLevel++;

            var keys = _cachedKeys;
            if (keys == null || keys.Length == 0)
            {
                EditorGUILayout.LabelField(LC.Combine("Empty"));
            }
            else
            {
                foreach (var key in keys)
                    EditorGUILayout.LabelField(key);
            }

            EditorGUI.indentLevel--;
        }

        protected override void OnEditorUpdate()
        {
            if (!Application.isPlaying || Target == null || Target.Settings == null) return;
            if (_refreshInFlight) return;
            RefreshKeysAsync().Forget();
        }

        private async UniTaskVoid RefreshKeysAsync()
        {
            if (_refreshInFlight) return;
            _refreshInFlight = true;
            try
            {
                var newKeys = await Target.ListKeysAsync();
                var newSlots = await Target.GetAllSlotsAsync();

                if (this == null || Target == null) return;

                _cachedKeys = newKeys ?? Array.Empty<string>();
                _cachedSlots = newSlots ?? Array.Empty<ArchiveSlotMeta>();
                Repaint();
            }
            catch (Exception ex)
            {
                _cachedKeys = Array.Empty<string>();
                _cachedSlots = Array.Empty<ArchiveSlotMeta>();
                Debug.LogWarning($"[ ArchiveManagerInspector ] Failed to refresh key list: {ex.Message}");
            }
            finally
            {
                _refreshInFlight = false;
            }
        }

        #region Fallback Strings

        private static readonly string[] ArmSlotList = { "Slot", "List" };
        private static readonly string[] ArmSlotEmpty = { "No", "Slot", "Found" };
        private static readonly string[] ArmKeyList = { "Active", "Slot", "In", "Key", "List" };

        #endregion

        private static string FormatBytes(long bytes)
        {
            return bytes switch
            {
                < 0 => "—",
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{bytes / 1024f:F1} KB",
                _ => $"{bytes / (1024f * 1024f):F1} MB"
            };
        }
    }
}
