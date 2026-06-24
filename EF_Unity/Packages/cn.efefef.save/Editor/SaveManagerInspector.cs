/*
 * ================================================
 * Describe:      SaveManager 运行时调试面板。继承 EFInspectorBase，支持自动刷新。
 *                Inspector 中查看槽位列表、Key 列表，手动保存/删除操作。
 * Author:        Alvin8412
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-06-24 22:33:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Systems.Save.Editor
{
    [CustomEditor(typeof(SaveManager))]
    public class SaveManagerInspector : EFInspectorBase<SaveManager>
    {
        protected override string Title => "Save Manager";

        private bool _showSlots = true;
        private bool _showKeys;
        private string[] _cachedKeys;

        protected override void OnEditorEnable()
        {
            base.OnEditorEnable();
        }

        protected override void OnEditorGUI()
        {
            var sm = Target;

            // Settings Info
            if (sm.Settings != null)
            {
                EditorGUILayout.LabelField("Provider",
                    string.IsNullOrEmpty(sm.Settings.providerTypeName)
                        ? "FileSaveProvider (default)"
                        : sm.Settings.providerTypeName);
                EditorGUILayout.LabelField("Auto Save", $"{sm.Settings.autoSaveIntervalSeconds}s");
                EditorGUILayout.LabelField("Data Version", sm.Settings.dataVersion.ToString());
                EditorGUILayout.LabelField("Active Slot", sm.ActiveSlot.ToString());
            }

            EditorGUILayout.Space(5);

            // Slots
            _showSlots = EditorGUILayout.Foldout(_showSlots, "Slots", true);
            if (_showSlots)
            {
                EditorGUI.indentLevel++;

                var slots = sm.GetAllSlots();
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
                            sm.ActiveSlot = slot.slotId;

                        if (GUILayout.Button("Delete", GUILayout.Width(60)))
                        {
                            sm.DeleteSlotAsync(slot.slotId).Forget();
                            Repaint();
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();
                    }
                }

                EditorGUI.indentLevel--;
            }

            // Keys
            _showKeys = EditorGUILayout.Foldout(_showKeys, "Keys in Active Slot", true);
            if (_showKeys)
            {
                EditorGUI.indentLevel++;

                if (GUILayout.Button("Refresh Keys"))
                    ListKeysAsync(sm);

                if (_cachedKeys != null && _cachedKeys.Length > 0)
                {
                    foreach (var key in _cachedKeys)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(key);
                        if (GUILayout.Button("Delete", GUILayout.Width(60)))
                        {
                            sm.DeleteKeyAsync(key).Forget();
                            ListKeysAsync(sm);
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

            // Actions
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Test Slot"))
            {
                sm.CreateSlotAsync("Test Slot").Forget();
                Repaint();
            }

            if (GUILayout.Button("Flush All"))
                sm.FlushAsync().Forget();

            EditorGUILayout.EndHorizontal();
        }

        protected override void OnEditorUpdate()
        {
            // 自动刷新由基类 AutoRefresh 控制，无需额外逻辑
        }

        protected override void OnEditorDisable()
        {
            base.OnEditorDisable();
        }

        private async void ListKeysAsync(SaveManager sm)
        {
            _cachedKeys = await sm.ListKeysAsync();
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
