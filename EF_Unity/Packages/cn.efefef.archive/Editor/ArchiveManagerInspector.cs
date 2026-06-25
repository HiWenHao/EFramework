/*
 * ================================================
 * Describe:      ArchiveManager 运行时监测面板。展示槽位列表、Key 列表、存储状态。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 17:00:00
 * ScriptVersion: 0.1.1
 * Changelog:
 *   0.1.1  移除失效的 60 帧累加器（基类已经按 RefreshInterval 节流），
 *          让基类节流机制直接生效；OnEditorGUI 边界检查；
 *          异步刷新加并发保护。
 *   0.1.0  首版
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

        // 并发保护：避免上一个异步刷新还未完成时再次启动新的刷新
        private bool _refreshInFlight;

        protected override void OnEditorGUI()
        {
            // 防御：基类在非 Play 模式下仍会调用 OnInspectorGUI（用于显示元数据），
            // 但 Target 可能为 null（编辑器跨域重载后尚未绑定）
            if (Target == null) return;

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
            else
            {
                EditorGUILayout.HelpBox(
                    "ArchiveSettings not loaded. Check Resources/Configs/ArchiveSettings.asset.",
                    MessageType.Warning);
            }

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
                        // 防御：JsonUtility 反序列化可能产生默认结构体（slotId=0 但其他字段都为空），
                        // 简单过滤掉完全空的 meta
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

        // 基类 EFInspectorBase 已经按 RefreshInterval（默认 0.3s）节流调用 OnEditorUpdate，
        // 子类不需要再叠加帧计数器（之前的 60 帧累加器永远到不了，是死代码）。
        // 这里直接发起异步刷新，基类负责节流。
        protected override void OnEditorUpdate()
        {
            if (!Application.isPlaying || Target == null || Target.Settings == null) return;
            if (_refreshInFlight) return; // 并发保护
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

                // 防御：异步过程中 Target 可能被销毁（场景切换 / 重载）
                if (this == null || Target == null) return;

                _cachedKeys = newKeys ?? Array.Empty<string>();
                _cachedSlots = newSlots ?? Array.Empty<ArchiveSlotMeta>();
                Repaint();
            }
            catch (Exception ex)
            {
                _cachedKeys = Array.Empty<string>();
                _cachedSlots = Array.Empty<ArchiveSlotMeta>();
                Debug.LogWarning($"[ArchiveManagerInspector] Failed to refresh key list: {ex.Message}");
            }
            finally
            {
                _refreshInFlight = false;
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
            if (bytes < 0) return "—";
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F1} KB";
            return $"{bytes / (1024f * 1024f):F1} MB";
        }
    }
}
