/*
 * ================================================
 * Describe:      ArchiveManager 运行时调试面板。继承 EFInspectorBase，支持自动刷新。
 *                Inspector 中查看槽位列表、Key 列表，手动保存/删除操作。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-24 23:19:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Systems.Archive.Editor
{
    [CustomEditor(typeof(ArchiveManager))]
    public class ArchiveManagerInspector : EFInspectorBase<ArchiveManager>
    {
        protected override string Title => LC.Combine(Lc.Archive, Lc.Data, Lc.Manager);

        private bool _showSlots = true;
        private bool _showKeys;
        private string[] _cachedKeys;

        protected override void OnEditorEnable()
        {
            base.OnEditorEnable();
        }

        protected override void OnEditorGUI()
        {
            var am = Target;

            if (am.Settings != null)
            {
                EditorGUILayout.LabelField("Provider",
                    string.IsNullOrEmpty(am.Settings.providerTypeName)
                        ? "FileArchiveProvider (default)"
                        : am.Settings.providerTypeName);
                EditorGUILayout.LabelField("Auto Save", $"{am.Settings.autoSaveIntervalSeconds}s");
                EditorGUILayout.LabelField("Data Version", am.Settings.dataVersion.ToString());
                EditorGUILayout.LabelField("Active Slot", am.ActiveSlot.ToString());
            }

            EditorGUILayout.Space(5);

            _showSlots = EditorGUILayout.Foldout(_showSlots, "Slots", true);
            if (_showSlots)
            {
                EditorGUI.indentLevel++;

                var slots = am.GetAllSlots();
                if (slots.Length == 0)
                {
                    EditorGUILayout.LabelField("(no slots found)");
                }
                else
                {
                    foreach (var slot in slots)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"Slot {slot.slotId}: {slot.slotName}",
                            EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Progress", slot.progressDescription);
                        EditorGUILayout.LabelField("Play Time", slot.PlayTimeFormatted);
                        EditorGUILayout.LabelField("Last Modified", slot.LastModifiedFormatted);
                        EditorGUILayout.LabelField("Size", FormatBytes(slot.totalSizeBytes));

                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                            am.ActiveSlot = slot.slotId;

                        if (GUILayout.Button("Delete", GUILayout.Width(60)))
                        {
                            am.DeleteSlotAsync(slot.slotId).Forget();
                            Repaint();
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUI.indentLevel--;
            }

            _showKeys = EditorGUILayout.Foldout(_showKeys, "Keys in Active Slot", true);
            if (_showKeys)
            {
                EditorGUI.indentLevel++;

                if (GUILayout.Button("Refresh Keys"))
                    ListKeysAsync(am);

                if (_cachedKeys != null && _cachedKeys.Length > 0)
                {
                    foreach (var key in _cachedKeys)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(key);
                        if (GUILayout.Button("Delete", GUILayout.Width(60)))
                        {
                            am.DeleteKeyAsync(key).Forget();
                            ListKeysAsync(am);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else if (_cachedKeys != null)
                {
                    EditorGUILayout.LabelField("(empty)");
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Test Slot"))
            {
                am.CreateSlotAsync("Test Slot").Forget();
                Repaint();
            }

            if (GUILayout.Button("Flush All"))
                am.FlushAsync().Forget();

            EditorGUILayout.EndHorizontal();
        }

        protected override void OnEditorUpdate()
        {
        }

        protected override void OnEditorDisable()
        {
            base.OnEditorDisable();
        }

        private async void ListKeysAsync(ArchiveManager am)
        {
            _cachedKeys = await am.ListKeysAsync();
            Repaint();
        }

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
