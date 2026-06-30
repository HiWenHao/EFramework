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
        protected override string Title => LC.Combine(Lc.Archive, Lc.Data, Lc.Monitor);

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
                    LC.Combine(Lc.Archive, Lc.Config, Lc.No, Lc.Load, Lc.Success)
                    + "\n"
                    + LC.Combine(Lc.Please, Lc.Check, Lc.Config, Lc.By, Lc.Path)
                    + ": Resources/Configs/ArchiveSettings.asset.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(LC.Combine(Lc.Provider),
                string.IsNullOrEmpty(Target.Settings.providerTypeName)
                    ? LC.Combine(Lc.File, Lc.Archive, Lc.Provider, Lc.Default)
                    : Target.Settings.providerTypeName);
            EditorGUILayout.LabelField(LC.Combine(Lc.Auto, Lc.Save),
                $"{Target.Settings.autoSaveIntervalSeconds}s");
            EditorGUILayout.LabelField(LC.Combine(Lc.Data, Lc.Version),
                Target.Settings.dataVersion.ToString());
            EditorGUILayout.LabelField(LC.Combine(Lc.Active, Lc.Slot),
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
                            $"{LC.Combine(Lc.Slot)} {slot.slotId}: {slot.slotName}",
                            EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(LC.Combine(Lc.Progress), slot.progressDescription);
                        EditorGUILayout.LabelField(LC.Combine(Lc.Play, Lc.Time), slot.PlayTimeFormatted);
                        EditorGUILayout.LabelField(LC.Combine(Lc.Last, Lc.Modified), slot.LastModifiedFormatted);
                        EditorGUILayout.LabelField(LC.Combine(Lc.Size), FormatBytes(slot.totalSizeBytes));
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
                EditorGUILayout.LabelField(LC.Combine(Lc.Empty));
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

        private static readonly Lc[] ArmSlotList = { Lc.Slot, Lc.List };
        private static readonly Lc[] ArmSlotEmpty = { Lc.No, Lc.Slot, Lc.Found };
        private static readonly Lc[] ArmKeyList = { Lc.Active, Lc.Slot, Lc.In, Lc.Key, Lc.List };

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
