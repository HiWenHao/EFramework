/*
 * ================================================
 * Describe:        LocalizedText 的 Inspector：key 下拉 + 搜索弹窗
 * Author:          Alvin5100
 * CreationTime:    2026-10-10 19:22:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-15 18:58:00
 * ScriptVersion:   0.4
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
    /// <summary>
    /// LocalizedText 专属 Inspector
    /// </summary>
    [CustomEditor(typeof(LocalizedText))]
    [CanEditMultipleObjects]
    internal class LocalizedTextEditor : TMP_EditorPanelUI
    {
        /// <summary> 目标对象 </summary>
        private LocalizedText _target;

        /// <summary> 缓存的 key 列表（按文件时间戳失效，避免每帧读盘） </summary>
        private static List<LocalizationKeyEntry> _cachedKeys;

        /// <summary> 缓存对应的文件时间戳，用于变更检测 </summary>
        private static DateTime _cachedStamp;

        /// <summary>
        /// 读取 key 清单
        /// <para>以文件最后写入时间为缓存键：文件未变则复用缓存，外部手改 JSON 也能自动生效。</para>
        /// <para>Read the key manifest, cached by last-write time.</para>
        /// </summary>
        /// <returns>全部 key 条目；清单不存在或读取失败时返回空列表（不返回 null）</returns>
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
                D.Error($"[Localization] 读取 Key 清单失败: {e.Message}");
                return new List<LocalizationKeyEntry>();
            }
        }

        /// <summary>
        /// 使 key 缓存失效，强制下次 <see cref="LoadKeys"/> 重新读盘
        /// <para>面板保存 / 重新加载清单后调用，使下拉与搜索窗立刻反映最新数据。</para>
        /// <para>Called after the manifest is saved or reloaded.</para>
        /// </summary>
        public static void InvalidateCache()
        {
            _cachedKeys?.Clear();
            _cachedKeys = null;
        }

        /// <summary>
        /// 绘制 Inspector
        /// <para>结构：上方 key 选择行 + 下方 TMP 原面板（base 绘制）。</para>
        /// </summary>
        public override void OnInspectorGUI()
        {
            _target = target as LocalizedText;
            if (_target == null)
            {
                base.OnInspectorGUI();
                return;
            }

            DrawKeyRow(LoadKeys());

            EditorGUILayout.Space(4f);

            DrawPreviewRow();

            EditorGUILayout.Space(4f);

            base.OnInspectorGUI();
        }

        /// <summary>
        /// 绘制译文预览行
        /// <para>左侧语言下拉 + 中间只读译文框 + 右侧「应用」按钮（仅手动模式下显示）。
        /// 译文经 <see cref="EditorL10nPreview"/> 从导表产物读取，与运行时同一数据源，
        /// 因此所见即所得。</para>
        /// <para>两种应用模式（开关在 多语言设置 → 自动应用）：
        /// 手动模式显示「应用」按钮；自动模式选择 key 即写入，按钮隐藏。</para>
        /// <para>未选 key / 语言表不存在 / 该词未翻译，三种情况给出不同提示——
        /// 它们是三类完全不同的问题，混成一句「无数据」会让人无从下手。</para>
        /// </summary>
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

        /// <summary>
        /// 绘制「应用」按钮
        /// <para>把当前预览语言的译文写入 TMP 文本框，便于烘焙默认文案
        /// （如未接入运行时的纯静态展示场景）。未选 key 或该词无译文时按钮禁用。</para>
        /// <para>运行时若 key 命中语言表，<see cref="LocalizedText.Refresh"/> 仍会用
        /// 译文覆盖 text，烘焙内容只影响编辑器显示与序列化默认值。</para>
        /// </summary>
        private void DrawBakeTextButton()
        {
            var value = _target.HasKey ? EditorL10nPreview.Get(_target.Key) : null;

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(value)))
            {
                if (!GUILayout.Button(LC.Combine(Lc.Apply), GUILayout.Width(64f)))
                    return;
            }

            Undo.RecordObject(_target, "Apply Text With Preview");
            _target.text = value;
            EditorUtility.SetDirty(_target);
        }

        /// <summary>
        /// 绘制预览语言下拉
        /// <para>语言集合来自语言表文件名自动发现（见 <see cref="L10nTableStore"/>），
        /// 与运行时可选语言天然一致；还没导过表的全新工程回落到默认集合，
        /// 保证下拉框永远有项可选。</para>
        /// </summary>
        private void DrawLanguageDropdown()
        {
            var source = EditorL10nPreview.AvailableLanguages;
            var languages = new string[source.Count];
            for (var i = 0; i < source.Count; i++) languages[i] = source[i];

            int index = Array.IndexOf(languages, EditorL10nPreview.PreviewLanguage);
            if (index < 0) index = 0;

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup(index, languages, GUILayout.Width(50f));

            if (!EditorGUI.EndChangeCheck() || newIndex < 0) return;
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

        /// <summary>
        /// 绘制只读译文框
        /// </summary>
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

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(rect, value);
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// 绘制 key 选择行：当前 key 显示 + 下拉 + 搜索按钮
        /// <para>清单为空时改为提示条，不画选择控件。</para>
        /// </summary>
        /// <param name="keys">全量 key 条目</param>
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

        /// <summary>
        /// 绘制当前 key 显示区
        /// <para>禁用态文本框：看起来像个输入框，但不可编辑，宽度撑满剩余空间。</para>
        /// <para>未选择时显示灰色占位符「-」。</para>
        /// </summary>
        /// <param name="current">当前 key；空串表示未选择</param>
        private static void DrawCurrentKey(string current)
        {
            bool hasKey = !string.IsNullOrEmpty(current);
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true));

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(rect, hasKey ? current : "-");
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// 绘制 key 下拉框
        /// <para>首项固定为空串（表示未选择），其后为清单全部 key。</para>
        /// </summary>
        /// <param name="keys">全量 key 条目</param>
        /// <param name="current">当前 key；不在清单中时回落到首项</param>
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

        /// <summary>
        /// 绘制搜索按钮：打开搜索窗供快速定位 key
        /// <para>选中结果经回调写回组件，支持 Undo。</para>
        /// </summary>
        /// <param name="keys">全量 key 条目</param>
        private void DrawSearchButton(List<LocalizationKeyEntry> keys)
        {
            if (!GUILayout.Button(LC.Combine(Lc.Search), GUILayout.Width(50f)))
                return;

            KeySearchWindow.Show(keys, picked =>
            {
                CommonRecord(picked);
                Repaint();
            });
        }

        /// <summary>
        /// 通用操作记录
        /// <para>自动应用模式下（见 <see cref="LocalizationData.AutoApplyOnKeySelected"/>），选择 key 后立即把
        /// 预览译文写入文本框，等效于点了「替换文本」按钮；该词无译文时跳过写入，
        /// 保持原文本。手动模式下只改 key。</para>
        /// <para>Undo 记录在改值之前：key 与 text 的变更落在同一步撤销里。</para>
        /// </summary>
        /// <param name="keyName">键名</param>
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
    }
}