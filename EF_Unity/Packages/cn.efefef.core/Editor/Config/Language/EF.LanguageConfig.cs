/*
 * ================================================
 * Describe:      Editor panel language support (Optimized)
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-28 16:14:49
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-01
 * ScriptVersion: 0.4
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    public enum ELanguage
    {
        English = 0,
        中文,
    }

    /// <summary>
    /// Editor panel language core.
    /// 用于编辑器界面的多语言支持
    /// </summary>
    internal static class LC
    {
        private class LcRoot
        {
            public List<LcItem> LcList { get; set; }
        }

        private class LcItem
        {
            /// <summary>
            /// 名字
            /// </summary>
            public string name { get; set; }

            /// <summary>
            /// 描述
            /// </summary>
            public string desc { get; set; }

            public List<string> array { get; set; }
        }

        private static Dictionary<Lc, LcItem> _lcDictionary;
        private static int _currentIndex = -1;
        private static string _separator; // 当前语言的分隔符，语言切换时重置
        private static string _jsonPath; // 缓存的 JSON 文件路径

        /// <summary>
        /// 当前显示的语言（可读写，修改后自动保存到 EditorPrefs）
        /// </summary>
        public static ELanguage DisPlayLanguage
        {
            get
            {
                if (_currentIndex == -1)
                    _currentIndex = EditorPrefs.GetInt(GetPrefsKey(), 0);
                return (ELanguage)_currentIndex;
            }
            set
            {
                int newIndex = (int)value;
                if (_currentIndex == newIndex)
                    return;

                _currentIndex = newIndex;
                EditorPrefs.SetInt(GetPrefsKey(), _currentIndex);
                _separator = null;
            }
        }

        private static string GetPrefsKey() => $"{Application.productName}_EditorLanguageIndex";
        
        private static void EnsureJsonPath()
        {
            if (string.IsNullOrEmpty(_jsonPath))
                _jsonPath = Path.Combine(Utility.Path.GetEfAssetsPath(), "Description/Editorlanguages.json");
        }

        private static void LoadLanguageData()
        {
            if (_lcDictionary is { Count: > 0 })
                return;

            EnsureJsonPath();
            if (!File.Exists(_jsonPath))
            {
                D.Error($"[LC] Language file not found: {_jsonPath}");
                _lcDictionary = new Dictionary<Lc, LcItem>();
                return;
            }

            string json = File.ReadAllText(_jsonPath);
            var root = JsonConvert.DeserializeObject<LcRoot>(json);
            if (root?.LcList == null)
            {
                D.Error("[LC] Failed to parse language JSON.");
                _lcDictionary = new Dictionary<Lc, LcItem>();
                return;
            }

            _lcDictionary = new Dictionary<Lc, LcItem>();
            foreach (var item in root.LcList)
            {
                if (Enum.TryParse(item.name, out Lc key))
                    _lcDictionary[key] = item;
                else
                    D.Warning($"[LC] Enum value '{item.name}' not found in Lc, skip.");
            }
        }

        /// <summary>
        /// 获取当前语言的分隔符（从 Lc.S 翻译得到）
        /// </summary>
        private static string GetSeparator()
        {
            if (_separator != null)
                return _separator;

            LoadLanguageData();
            if (_lcDictionary != null && _lcDictionary.TryGetValue(Lc.S, out var item) &&
                item.array != null && _currentIndex >= 0 && _currentIndex < item.array.Count)
            {
                _separator = item.array[_currentIndex] ?? string.Empty;
            }
            else
            {
                _separator = string.Empty;
            }

            return _separator;
        }

        /// <summary>
        /// 获取单个键的本地化字符串。
        /// </summary>
        /// <param name="lc">语言键</param>
        /// <returns>翻译后的文本，若找不到则返回 "[键名]" 便于排查</returns>
        public static string Combine(Lc lc)
        {
            LoadLanguageData();
            if (_lcDictionary != null && _lcDictionary.TryGetValue(lc, out var item))
            {
                if (item.array != null && _currentIndex >= 0 && _currentIndex < item.array.Count)
                {
                    return item.array[_currentIndex];
                }
            }

            // 降级处理：返回带方括号的键名，避免编辑器崩溃
            return $"[{lc}]";
        }

        /// <summary>
        /// 将多个键的本地化字符串用当前语言的分隔符拼接。
        /// </summary>
        /// <param name="lc">语言键数组</param>
        /// <returns>拼接后的字符串</returns>
        public static string Combine(Lc[] lc)
        {
            if (lc == null || lc.Length == 0)
                return string.Empty;

            LoadLanguageData();
            var sb = new StringBuilder();
            string sep = GetSeparator();

            for (int i = 0; i < lc.Length; i++)
            {
                sb.Append(Combine(lc[i]));
                if (i < lc.Length - 1)
                    sb.Append(sep);
            }

            return sb.ToString();
        }

        [MenuItem("EFTools/Utility/Update Edit Language", priority = 10002)]
        private static void UpdateLanguageConfig()
        {
            EnsureJsonPath();
            if (!File.Exists(_jsonPath))
            {
                D.Error($"Cannot find language JSON: {_jsonPath}");
                EditorUtility.DisplayDialog("错误", $"未找到语言配置文件：{_jsonPath}", "确定");
                return;
            }

            var root = JsonConvert.DeserializeObject<LcRoot>(File.ReadAllText(_jsonPath));
            if (root?.LcList == null)
            {
                D.Error("Failed to parse JSON for enum generation.");
                EditorUtility.DisplayDialog("错误", "解析语言 JSON 失败，请检查文件格式。", "确定");
                return;
            }

            // 构建枚举文件内容
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// This file is auto-generated by LC.UpdateLanguageConfig().");
            sb.AppendLine("// Do not modify manually.\n");
            sb.AppendLine("namespace EasyFramework.Edit\n{");
            sb.AppendLine("\t/// <summary> Editor language keys. </summary>");
            sb.AppendLine("\tpublic enum Lc\n\t{");

            foreach (var item in root.LcList)
            {
                string desc = string.IsNullOrEmpty(item.desc) ? item.name : item.desc;
                sb.AppendLine($"\t\t/// <summary> {desc} </summary>");
                sb.AppendLine($"\t\t{item.name},");
            }

            sb.AppendLine("\t}\n}");
            
            string outputDir = $"{Utility.Path.GetEfPath()}/Editor/Config/Language";
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);
            string outputPath = Path.Combine(outputDir, "EF.LanguageEnum.cs");
            File.WriteAllText(outputPath, sb.ToString());
            AssetDatabase.Refresh();
        }
    }
}