/*
 * ================================================
 * Describe:      编辑器面板语言支持
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-28 16:14:49
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-01
 * ScriptVersion: 0.8
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
    /// <summary>
    /// 编辑器支持的语言类型
    /// </summary>
    public enum ELanguage
    {
        English = 0,
        中文,
    }

    /// <summary>
    /// 编辑器本地化辅助类
    /// 提供多语言文本获取、拼接以及枚举自动生成功能
    /// </summary>
    public static class LC
    {
        private const string EnumOutputFile = "EF.LanguageEnum.cs";             // 枚举输出文件名
        private const string EnumOutputDir = "Editor/Config/Language";          // 枚举输出目录
        private const string RelativePath = "Description/Editorlanguages.json"; // 相对路径

        private class LcRoot
        {
            public List<LcItem> LcList { get; set; }
        }

        private class LcItem
        {
            public string name { get; set; } // 枚举名称
            public string desc { get; set; } // 描述（用作注释）
            public List<string> array { get; set; } // 多语言文本数组：[0]=English, [1]=中文
        }

        // ==================== 私有状态 ====================
        private static Dictionary<Lc, LcItem> _lcDictionary;    // 枚举 -> 语言项的映射
        private static int _currentIndex = -1;                  // 当前语言索引（-1表示未加载）
        private static string _separatorCache;                  // 分隔符缓存（Lc.S 的值）
        private static string _jsonPath;                        // 语言文件的完整路径
        private static bool _isLoaded;                          // 是否已加载完成
        private static readonly object _lock = new object();    // 线程锁

        /// <summary>
        /// 获取或设置当前显示的语言
        /// 该值保存在 EditorPrefs 中，可跨编辑器会话持久化
        /// </summary>
        public static ELanguage DisplayLanguage
        {
            get
            {
                EnsureCurrentIndex();
                return (ELanguage)_currentIndex;
            }
            set
            {
                EnsureCurrentIndex();
                if (_currentIndex == (int)value)
                    return;

                _currentIndex = (int)value;
                EditorPrefs.SetInt(GetPrefsKey(), _currentIndex);
                _separatorCache = null;

                // 重置加载状态，切换语言后强制重新加载字典
                _isLoaded = false;
                _lcDictionary = null;
            }
        }

        /// <summary>
        /// 获取 EditorPrefs 中保存语言索引的键名
        /// </summary>
        private static string GetPrefsKey() => $"{Application.productName}_EditorLanguageIndex";

        /// <summary>
        /// 确保当前语言索引已经从 EditorPrefs 加载
        /// 如果 _currentIndex 仍为 -1，则从存储中读取（默认值为 0）
        /// </summary>
        private static void EnsureCurrentIndex()
        {
            if (_currentIndex == -1)
                _currentIndex = EditorPrefs.GetInt(GetPrefsKey(), 0);
        }

        /// <summary>
        /// 校验并设置语言 JSON 文件的完整路径
        /// 依赖 Utility.Path.GetEfAssetsPath() 获取 EF 资源根目录
        /// </summary>
        private static void EnsureJsonPath()
        {
            if (!string.IsNullOrEmpty(_jsonPath))
                return;

            string efAssetsPath = Utility.Path.GetEfAssetsPath();
            if (string.IsNullOrEmpty(efAssetsPath))
            {
                D.Error("[LC] 无法获取 EF Assets 路径");
                _jsonPath = string.Empty;
                return;
            }

            _jsonPath = Path.Combine(efAssetsPath, RelativePath);
        }

        /// <summary>
        /// 加载所有语言数据（线程安全，仅加载一次）
        /// 读取 JSON 文件并构建枚举到语言项的映射字典
        /// </summary>
        private static void LoadLanguageData()
        {
            if (_isLoaded)
                return;

            lock (_lock)
            {
                if (_isLoaded)
                    return;

                _lcDictionary = new Dictionary<Lc, LcItem>();
                EnsureJsonPath();
                EnsureCurrentIndex();

                if (string.IsNullOrEmpty(_jsonPath) || !File.Exists(_jsonPath))
                {
                    D.Error($"[LC] 语言文件不存在: {_jsonPath}");
                    _isLoaded = true;
                    return;
                }

                try
                {
                    string json = File.ReadAllText(_jsonPath);
                    var root = JsonConvert.DeserializeObject<LcRoot>(json);

                    if (root?.LcList == null)
                    {
                        D.Error("[LC] 解析语言 JSON 失败");
                        _isLoaded = true;
                        return;
                    }

                    foreach (var item in root.LcList)
                    {
                        if (Enum.TryParse(item.name, true, out Lc key))
                            _lcDictionary[key] = item;
                        else
                            D.Warning($"[LC] 枚举值 '{item.name}' 在 Lc 中不存在，已跳过");
                    }
                }
                catch (Exception ex)
                {
                    D.Error($"[LC] 加载语言数据时发生异常: {ex.Message}");
                }
                finally
                {
                    _isLoaded = true;
                }
            }
        }

        /// <summary>
        /// 获取当前语言的分隔符（对应 Lc.S 项的翻译）
        /// 分隔符用于 Join 方法拼接多个文本
        /// </summary>
        private static string GetSeparator()
        {
            if (_separatorCache != null)
                return _separatorCache;

            LoadLanguageData();

            if (_lcDictionary != null && _lcDictionary.TryGetValue(Lc.S, out var item) && item.array != null)
            {
                int index = GetSafeLanguageIndex(item.array.Count);
                _separatorCache = item.array[index] ?? string.Empty;
            }
            else
            {
                _separatorCache = string.Empty;
            }

            return _separatorCache;
        }

        /// <summary>
        /// 获取安全的语言索引（防止越界）
        /// </summary>
        /// <param name="maxCount">该语言项支持的语言种类数量</param>
        /// <returns>有效的索引值，如果 maxCount 小于等于 0 则返回 0</returns>
        private static int GetSafeLanguageIndex(int maxCount)
        {
            EnsureCurrentIndex();

            if (maxCount <= 0)
                return 0;

            int index = _currentIndex >= 0 ? _currentIndex : 0;
            return index < maxCount ? index : 0;
        }

        /// <summary>
        /// 获取单个键的本地化文本
        /// </summary>
        /// <param name="lc">语言枚举键</param>
        /// <returns>对应语言的文本；若获取失败返回 "[键名]" 便于调试</returns>
        public static string Combine(Lc lc)
        {
            LoadLanguageData();

            if (_lcDictionary != null && _lcDictionary.TryGetValue(lc, out var item) && item.array != null)
            {
                int index = GetSafeLanguageIndex(item.array.Count);
                if (index >= 0 && index < item.array.Count)
                    return item.array[index];
            }

            return $"[{lc}]"; // 回退值，便于排查缺失的键
        }

        /// <summary>
        /// 使用当前语言的分隔符拼接多个本地化文本
        /// </summary>
        /// <param name="keys">要拼接的语言键数组</param>
        /// <returns>拼接后的字符串；如果数组为空则返回空字符串</returns>
        public static string Combine(params Lc[] keys)
        {
            if (keys == null || keys.Length == 0)
                return string.Empty;

            LoadLanguageData();
            string separator = GetSeparator();

            if (keys.Length == 1)
                return Combine(keys[0]);

            var sb = new StringBuilder();
            for (int i = 0; i < keys.Length; i++)
            {
                sb.Append(Combine(keys[i]));
                if (i < keys.Length - 1)
                    sb.Append(separator);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 根据当前设定的语言类型返回设定好的对应语言
        /// <para>Return the corresponding language based on the currently set language type.</para>
        /// </summary>
        /// <summary>
        /// 按当前显示语言选中文案；args 非空时对选中文案执行 string.Format 填充占位符
        /// <para>Selects text by the current display language; formats the selected text with args when provided.</para>
        /// <para>占位符与参数不匹配时降级返回原文并告警，不抛出异常打断调用链。</para>
        /// </summary>
        /// <param name="zh">中文文案，含 {0} 等占位符时与 args 按序对应<para>Chinese text; placeholders like {0} map to args in order.</para></param>
        /// <param name="en">英文文案，占位符规则与 zh 相同<para>English text; same placeholder rules as zh.</para></param>
        /// <param name="args">占位符参数；不传或传空时原文返回，不做格式化<para>Placeholder args; omitted or empty returns the selected text as-is.</para></param>
        /// <returns>选中（并按需格式化）的文案<para>The selected and optionally formatted text.</para></returns>
        public static string Tr(string zh, string en, params object[] args)
        {
            bool isEn = DisplayLanguage == ELanguage.English;
            var text = isEn ? en : zh;

            if (args == null || args.Length == 0)
                return text;

            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                D.Warning(isEn
                    ? $"[ LC ] TrFormatting failed (placeholders do not match parameters), returning original text:  {text}"
                    : $"[ LC ] Tr 格式化失败（占位符与参数不匹配），返回原文： {text}");
                return text;
            }
        }

        /// <summary>
        /// 编辑器菜单：从 JSON 文件重新生成 Lc 枚举文件
        /// 通常在添加新语言键后执行
        /// </summary>
        [MenuItem(MenuItemToolkit.Utility + "✍️ Update Edit Language", false, MenuItemToolkit.UtilityPriority + 3)]
        private static void UpdateLanguageConfig()
        {
            EnsureJsonPath();

            if (string.IsNullOrEmpty(_jsonPath) || !File.Exists(_jsonPath))
            {
                EditorUtility.DisplayDialog(Combine(Lc.Error),
                    Tr("未找到语言配置文件:\n{0}", "Language config file not found:\n{0}", _jsonPath), Combine(Lc.Confirm));
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(_jsonPath);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(Combine(Lc.Error),
                    Tr("读取语言文件失败:\n{0}", "Failed to read language file:\n{0}", ex.Message), Combine(Lc.Confirm));
                return;
            }

            LcRoot root;
            try
            {
                root = JsonConvert.DeserializeObject<LcRoot>(json);
            }
            catch (Exception ex)
            {
                D.Error(ex);
                EditorUtility.DisplayDialog(Combine(Lc.Error),
                    Tr("解析语言 JSON 失败，请检查文件格式。", "Failed to parse language JSON. Please check the file format."),
                    Combine(Lc.Confirm));
                return;
            }

            if (root?.LcList == null || root.LcList.Count == 0)
            {
                EditorUtility.DisplayDialog(Combine(Lc.Error),
                    Tr("语言 JSON 中没有包含任何条目。", "The language JSON contains no entries."), Combine(Lc.Confirm));
                return;
            }

            string enumContent = GenerateEnumContent(root.LcList);
            string outputDir = Path.Combine(Utility.Path.GetEfPath(), EnumOutputDir);
            string outputPath = Path.Combine(outputDir, EnumOutputFile);

            try
            {
                Directory.CreateDirectory(outputDir);
                File.WriteAllText(outputPath, enumContent);
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(Combine(Lc.Error),
                    Tr("写入枚举文件失败:\n{0}", "Failed to write enum file:\n{0}", ex.Message), Combine(Lc.Confirm));
            }
        }

        /// <summary>
        /// 根据 JSON 数据生成 Lc 枚举的 C# 代码内容
        /// </summary>
        /// <param name="items">语言项列表</param>
        /// <returns>完整的枚举代码字符串</returns>
        private static string GenerateEnumContent(List<LcItem> items)
        {
            if (items == null || items.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// 此文件由 LC.UpdateLanguageConfig() 自动生成");
            sb.AppendLine("// 请勿手动修改");
            sb.AppendLine();
            sb.AppendLine("namespace EasyFramework.Edit");
            sb.AppendLine("{");
            sb.AppendLine("\t/// <summary> 编辑器语言键 </summary>");
            sb.AppendLine("\tpublic enum Lc");
            sb.AppendLine("\t{");

            foreach (var item in items)
            {
                string desc = string.IsNullOrEmpty(item.desc) ? item.name : item.desc;
                sb.AppendLine($"\t\t/// <summary> {desc} </summary>");
                sb.AppendLine($"\t\t{item.name},");
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}