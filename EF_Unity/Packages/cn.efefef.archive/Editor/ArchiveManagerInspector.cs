/*
 * ================================================
 * Describe:      ArchiveManager 运行时监测面板。展示槽位列表、Key 列表、存储状态。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 01:34:00
 * ScriptVersion: 0.1
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

        private bool _showSlots = true; // 是否展开槽位折叠区
        private bool _showKeys;          // 是否展开 Key 折叠区
        private string[] _cachedKeys = Array.Empty<string>(); // 缓存的 Key 列表
        private ArchiveSlotMeta[] _cachedSlots = Array.Empty<ArchiveSlotMeta>(); // 缓存的槽位列表
        private int _refreshTimer;       // 降频计数器（每 N 帧刷新一次 Key 列表）

        protected override void OnEditorGUI()
        {
            // 基础信息
            if (Target.Settings != null)
            {
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
            }

            EditorGUILayout.Space();

            // 槽位列表
            _showSlots = EditorGUILayout.Foldout(_showSlots, LC.Combine(ArmSlotList), true);
            if (_showSlots)
            {
                EditorGUI.indentLevel++;

                var slots = _cachedSlots;
                if (slots.Length == 0)
                {
                    EditorGUILayout.LabelField(LC.Combine(ArmSlotEmpty));
                }
                else
                {
                    foreach (var slot in slots)
                    {
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

        // 降频刷新 Key 列表（每 60 帧异步拉取一次，避免每帧同步阻塞文件 I/O）
        protected override void OnEditorUpdate()
        {
            if (++_refreshTimer < 60) return;
            _refreshTimer = 0;

            if (!Application.isPlaying || Target == null || Target.Settings == null) return;
            RefreshKeysAsync().Forget();
        }

        private async UniTask RefreshKeysAsync()
        {
            try
            {
                _cachedKeys = await Target.ListKeysAsync();
                _cachedSlots = await Target.GetAllSlotsAsync();
            }
            catch (Exception ex)
            {
                _cachedKeys = Array.Empty<string>();
                _cachedSlots = Array.Empty<ArchiveSlotMeta>();
                Debug.LogWarning($"[ArchiveManagerInspector] Failed to refresh key list: {ex.Message}");
            }
        }

        #region Fallback Strings

        // LC 组合键（不在 JSON 中单独定义的多词短语）
        private static readonly Lc[] ArmSlotList = { Lc.Slot, Lc.List };
        private static readonly Lc[] ArmSlotEmpty = { Lc.No, Lc.Slot, Lc.Found };
        private static readonly Lc[] ArmKeyList = { Lc.Active, Lc.Slot, Lc.In, Lc.Key, Lc.List };

        #endregion

        // 格式化字节数为可读字符串（B → KB → MB）
        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F1} MB";
        }
    }
}
