/*
 * ================================================
 * Describe:        LocalizedText 的 Inspector：key 下拉 + 搜索弹窗
 * Author:          Alvin5100
 * CreationTime:    2026-10-10 19:22:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 18:45:00
 * ScriptVersion:   0.8
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using EasyFramework.Edit;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Localization.Edit
{
    // LocalizedText 专属 Inspector（结构：上方 key 选择行 + 预览行 + 动态文案区 + 下方 TMP 原面板）
    [CustomEditor(typeof(LocalizedText))]
    [CanEditMultipleObjects]
    internal class LocalizedTextEditor : TMP_EditorPanelUI
    {
        #region Fields

        // 目标对象
        private LocalizedText _target;

        // 缓存的 key 列表（按文件时间戳失效，避免每帧读盘）
        private static List<LocalizationKeyEntry> _cachedKeys;

        // 缓存对应的文件时间戳，用于变更检测
        private static DateTime _cachedStamp;

        #endregion

        #region Methods

        // 读取 key 清单（以文件最后写入时间为缓存键：文件未变则复用缓存，外部手改 JSON 也能自动生效）
        public static List<LocalizationKeyEntry> LoadKeys()
        {
            try
            {
                if (!File.Exists(LocalizationData.JsonPath)) return new List<LocalizationKeyEntry>();

                var stamp = File.GetLastWriteTimeUtc(LocalizationData.JsonPath);
                if (_cachedKeys != null && _cachedStamp == stamp) return _cachedKeys;

                var file = LocalizationKeyFile.Load(LocalizationData.JsonPath);
                _cachedKeys = file?.keys ?? new List<LocalizationKeyEntry>();
                _cachedStamp = stamp;
                return _cachedKeys;
            }
            catch (Exception e)
            {
                D.Error(LC.Tr("[Localization] 读取 Key 清单失败：{0}",
                    "[Localization] Failed to read the Key manifest: {0}",
                    e.Message));
                return new List<LocalizationKeyEntry>();
            }
        }

        // 使 key 缓存失效，强制下次 LoadKeys 重新读盘（面板保存 / 重新加载清单后调用）
        public static void InvalidateCache()
        {
            _cachedKeys?.Clear();
            _cachedKeys = null;
        }

        public override void OnInspectorGUI()
        {
            _target = target as LocalizedText;
            if (_target == null)
            {
                base.OnInspectorGUI();
                return;
            }

            EditorGUILayout.Space(4f);

            DrawKeyRow(LoadKeys());

            EditorGUILayout.Space(4f);

            DrawPreviewRow();

            EditorGUILayout.Space(4f);

            DrawDynamicSection();

            EditorGUILayout.Space(4f);

            base.OnInspectorGUI();
        }

        // 绘制译文预览行（左侧语言下拉 + 中间只读译文框 + 右侧「应用」按钮（仅手动模式下显示）。
        // 译文经 EditorL10nPreview 从导表产物读取，与运行时同一数据源，因此所见即所得。
        // 未选 key / 语言表不存在 / 该词未翻译，三种情况给出不同提示——它们是三类完全不同的问题）
        private void DrawPreviewRow()
        {
            EditorGUILayout.BeginHorizontal();

            DrawLanguageDropdown();
            DrawPreviewValue();

            // 自动应用模式下不显示手动按钮（选择 key 时已自动写入，见 CommonRecord）
            if (!LocalizationData.AutoApplyOnKeySelected)
                DrawBakeTextButton();

            EditorGUILayout.EndHorizontal();
        }

        // 绘制「应用」按钮（把当前预览语言的译文写入 TMP 文本框，便于烘焙默认文案，如未接入运行时的
        // 纯静态展示场景；未选 key 或该词无译文时按钮禁用。运行时若 key 命中语言表，Refresh 仍会用
        // 译文覆盖 text，烘焙内容只影响编辑器显示与序列化默认值）
        private void DrawBakeTextButton()
        {
            var value = _target.HasKey ? EditorL10nPreview.Get(_target.Key) : null;

            // Dynamic 文案：按组件参数格式化后烘焙，与运行时 Refresh 的结果一致
            value = ApplyPreviewFormat(value);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(value)))
            {
                if (!GUILayout.Button(LC.Combine(Lc.Apply), GUILayout.Width(64f)))
                    return;
            }

            Undo.RecordObject(_target, "Apply Text With Preview");
            _target.text = value;
            EditorUtility.SetDirty(_target);
        }

        // Dynamic 文案辅助区（key 的 kind 为 Dynamic 时显示：清单中的占位符说明（note）+
        // 格式化参数编辑（序列化在组件上，支持 Undo 与预制体覆盖）。Static key 或 key 未登记时不绘制任何内容）
        private void DrawDynamicSection()
        {
            if (!_target.HasKey) return;

            var entry = FindEntry(_target.Key);
            if (entry == null || entry.kind != LocalizationKeyKind.Dynamic) return;

            var useEn = LC.DisplayLanguage == ELanguage.English;

            var tip = useEn
                ? $"Dynamic text: {entry.note}\nDuring runtime, parameters can be passed through \nI18NManager.Instance.GetFormat(key, args) or SetFormatArgs(args), " +
                  $"\nor parameters can be directly configured below."
                : $"动态文案：{entry.note}\n运行时通过 I18NManager.Instance.GetFormat(key, args) 或者 SetFormatArgs(args) 传参，或在下方直接配置参数";
            EditorGUILayout.HelpBox(tip, MessageType.Info);

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("formatArgs"),
                new GUIContent(LC.Combine(Lc.Param)), true);
            serializedObject.ApplyModifiedProperties();
        }

        // 从 key 清单缓存中查找条目（未登记的 key 返回 null）
        private static LocalizationKeyEntry FindEntry(string keyName)
        {
            foreach (var entry in LoadKeys())
            {
                if (string.Equals(entry.name, keyName, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        // 按组件的格式化参数套用占位符（与运行时 LocalizedText.Refresh 行为一致；无参数 / 值不含占位符时原样返回，格式化失败返回原文）
        private string ApplyPreviewFormat(string value)
        {
            if (string.IsNullOrEmpty(value)
                || _target.FormatArgs.Count == 0
                || value.IndexOf('{') < 0)
            {
                return value;
            }

            try
            {
                var args = new object[_target.FormatArgs.Count];
                for (var i = 0; i < args.Length; i++)
                {
                    args[i] = _target.FormatArgs[i];
                }

                return string.Format(value, args);
            }
            catch (FormatException)
            {
                return value;
            }
        }

        // 绘制预览语言下拉（语言集合来自语言表文件名自动发现，与运行时可选语言天然一致；还没导过表的全新工程回落到默认集合。
        // 双模式：编辑态显示/写入预览语言（EditorPrefs 持久化）；Play 模式显示运行时当前语言，
        // 变更经 SwitchLanguage 直接切换——全场景 LocalizedText 即时刷新，预览与场景文本永远同源。
        // 自动应用模式下（编辑态），切换预览语言后立即把新语言的译文写入文本框）
        private void DrawLanguageDropdown()
        {
            var source = EditorL10nPreview.AvailableLanguages;
            var languages = new string[source.Count];
            for (var i = 0; i < source.Count; i++) languages[i] = source[i];

            var followsRuntime = EditorL10nPreview.FollowsRuntimeLanguage;
            var current = followsRuntime
                ? I18NManager.Instance.CurrentLanguage
                : EditorL10nPreview.PreviewLanguage;

            int index = Array.IndexOf(languages, current);
            if (index < 0) index = 0;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(index, languages, GUILayout.Width(50f));

            if (!EditorGUI.EndChangeCheck() || newIndex < 0) return;

            if (followsRuntime)
            {
                EditorL10nPreview.TrySwitchRuntimeLanguage(languages[newIndex]);
                return;
            }

            EditorL10nPreview.PreviewLanguage = languages[newIndex];
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

            if (LocalizationData.AutoApplyOnKeySelected)
            {
                Undo.RecordObject(_target, "Apply Text With Preview");
                var value = EditorL10nPreview.Get(_target.Key);
                _target.text = value;
                EditorUtility.SetDirty(_target);
            }
        }

        // 绘制只读译文框（无 key 显示占位；无语言表提示导表流程；缺词提示补译）
        private void DrawPreviewValue()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true));

            if (!_target.HasKey)
            {
                EditorGUI.LabelField(rect, $"({LC.Combine(Lc.Empty)})", EditorStyles.miniLabel);
                return;
            }

            if (!EditorL10nPreview.IsDataAvailable)
            {
                EditorGUI.HelpBox(rect,
                    "未找到语言表（l10n_tbtext_*.json）\n" +
                    "请在 多语言设置 导出 value 表并填好译文，再到 数据表配置 执行 Luban 导表；产物会自动同步进 Resources/L10n",
                    MessageType.Warning);
                return;
            }

            var value = EditorL10nPreview.Get(_target.Key);
            if (string.IsNullOrEmpty(value))
            {
                EditorGUI.HelpBox(rect,
                    $"[{LC.Combine(Lc.Missing)}] {_target.Key} 请在 多语言设置 中补译该条目",
                    MessageType.Info);
                return;
            }

            value = ApplyPreviewFormat(value);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(rect, value);
            EditorGUI.EndDisabledGroup();
        }

        // 绘制 key 选择行：当前 key 显示 + 下拉 + 搜索按钮（清单为空时改为提示条，不画选择控件）
        private void DrawKeyRow(List<LocalizationKeyEntry> keys)
        {
            if (keys.Count == 0)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(
                    $"LocalizationKeys.json {LC.Combine(Lc.File, Lc.Missing)}" +
                    $"\n{LC.Combine(Lc.Please, Lc.Create, Lc.In, Lc.Panel)}: EFTools/Settings/{LC.Combine(Lc.Multilingual, Lc.Config)}",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            var current = _target.Key;
            DrawCurrentKey(current);
            DrawKeyDropdown(keys, current);
            DrawSearchButton(keys);

            EditorGUILayout.EndHorizontal();
        }

        // 绘制当前 key 显示区（禁用态文本框：看起来像个输入框，但不可编辑，宽度撑满剩余空间；未选择时显示灰色占位符「-」）
        private static void DrawCurrentKey(string current)
        {
            bool hasKey = !string.IsNullOrEmpty(current);
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true));

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(rect, hasKey ? current : "-");
            EditorGUI.EndDisabledGroup();
        }

        // 绘制 key 下拉框（首项固定为空串（表示未选择），其后为清单全部 key）
        private void DrawKeyDropdown(List<LocalizationKeyEntry> keys, string current)
        {
            var options = new List<string> { string.Empty };
            foreach (var entry in keys)
            {
                options.Add(entry.name);
            }

            int index = string.IsNullOrEmpty(current) ? 0 : options.IndexOf(current);
            if (index < 0) index = 0;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(index, options.ToArray(), GUILayout.Width(100f));

            if (!EditorGUI.EndChangeCheck() || newIndex < 0) return;
            CommonRecord(options[newIndex]);
        }

        // 绘制搜索按钮：打开搜索窗供快速定位 key（选中结果经回调写回组件，支持 Undo）
        private void DrawSearchButton(List<LocalizationKeyEntry> keys)
        {
            if (!GUILayout.Button(LC.Combine(Lc.Search), GUILayout.Width(50f)))
                return;

            KeySearchWindow.Show(keys, _target.Key, picked =>
            {
                CommonRecord(picked);
                Repaint();
            });
        }

        // 通用操作记录（自动应用模式下，选择 key 后立即把预览译文写入文本框，等效于点了「应用」按钮；
        // 该词无译文时跳过写入，保持原文本。手动模式下只改 key。
        // Undo 记录在改值之前：key 与 text 的变更落在同一步撤销里）
        private void CommonRecord(string keyName)
        {
            Undo.RecordObject(_target, "Set Localization Key");
            _target.Key = keyName;
            _target.Refresh();

            if (LocalizationData.AutoApplyOnKeySelected)
            {
                var value = EditorL10nPreview.Get(keyName);
                if (!string.IsNullOrEmpty(value)) _target.text = value;
            }

            EditorUtility.SetDirty(_target);
        }

        #endregion
    }
}