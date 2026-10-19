/*
 * ================================================
 * Describe:        本地化数据仓库：编辑器预览与 Resources 回退通道（运行时数据经 I18NManager 按需加载）
 * Author:          Alvin5100
 * CreationTime:    2026-10-15 18:10:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 18:25:00
 * ScriptVersion:   0.2
 * ================================================
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EasyFramework.Localization
{
    /// <summary>
    /// 本地化数据仓库：编辑态预览的 Resources 回退通道（主通道为 DatableConfig.LubanDataPath 导表产物，运行时走 AssetsManager 按需加载）
    /// <para>Localization data store serving as the Resources fallback for editor preview.
    /// The primary preview channel reads the Luban export folder; runtime tables are loaded
    /// on demand by I18NManager via AssetsManager.</para>
    /// </summary>
    public static class L10nTableStore
    {
        #region Fields

        // 已加载的语言表缓存：语言代码 → key → 译文
        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        // 是否已完成语言发现（只扫一次，失效后经 Invalidate 重扫）
        private static bool _discovered;

        // 发现的全部语言代码（兜底语言 en 优先，其余字典序）
        private static List<string> _languages;

        // 是否发现了至少一张语言表
        private static bool _hasTables;

        // Resources 下的语言表目录（相对 Resources 根）
        internal const string ResourcesFolder = "L10n";

        // 语言表文件名前缀，语言代码紧跟其后（如 l10n_tbtext_zh.json）；新增语言只需多导一张表
        internal const string TableFilePrefix = "l10n_tbtext_";

        #endregion

        #region Properties

        /// <summary>
        /// 是否存在至少一张可用语言表（本工程是否接入多语言的闸门：无表则上层静默）
        /// <para>Whether at least one language table exists; gates all logging so projects
        /// without localization stay silent.</para>
        /// </summary>
        public static bool HasTables
        {
            get
            {
                EnsureDiscovered();
                return _hasTables;
            }
        }

        /// <summary>
        /// 已发现的全部语言代码（兜底语言 en 排最前，其余字典序，保证顺序稳定）
        /// <para>All discovered language codes, fallback first for stable ordering.</para>
        /// </summary>
        public static IReadOnlyList<string> Languages
        {
            get
            {
                EnsureDiscovered();
                return _languages;
            }
        }

        #endregion

        #region Methods

        // 确保已完成一次语言发现（不符合文件名前缀约定的资源会被自然过滤掉）
        private static void EnsureDiscovered()
        {
            if (_discovered) return;
            _discovered = true;

            _languages = new List<string>();
            foreach (var asset in Resources.LoadAll<TextAsset>(ResourcesFolder))
            {
                if (asset == null) continue;

                var fileName = asset.name;
                if (!fileName.StartsWith(TableFilePrefix, StringComparison.Ordinal)) continue;

                // 前缀之后即为语言代码（文件名 = l10n_tbtext_{lang}.json，Unity 的 name 不含扩展名）
                var language = fileName.Substring(TableFilePrefix.Length);
                if (string.IsNullOrEmpty(language)) continue;
                if (_languages.Contains(language)) continue;

                _languages.Add(language);
            }

            _languages.Sort(CompareLanguages);
            _hasTables = _languages.Count > 0;
        }

        // 语言排序：兜底语言（en）最前，其余按字典序
        private static int CompareLanguages(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.Ordinal)) return 0;
            if (string.Equals(a, LanguageCode.En, StringComparison.Ordinal)) return -1;
            if (string.Equals(b, LanguageCode.En, StringComparison.Ordinal)) return 1;
            return string.CompareOrdinal(a, b);
        }

        // 取指定语言表，未加载时从 Resources 懒加载（保证返回非 null：坏文件落为空表并缓存，避免反复读盘刷错误）
        private static Dictionary<string, string> GetOrLoadTable(string language)
        {
            if (Tables.TryGetValue(language, out var cached)) return cached;

            var asset = Resources.Load<TextAsset>($"{ResourcesFolder}/{TableFilePrefix}{language}");
            var table = ParseTable(language, asset != null ? asset.text : null);
            Tables[language] = table;
            return table;
        }

        // 解析一张语言表 JSON（解析失败属真故障：输出错误日志并按空表处理）
        private static Dictionary<string, string> ParseTable(string language, string text)
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(text)) return table;

            try
            {
                // 导出文件可能带 UTF-8 BOM：BOM 字符混进 JSON 首位会让反序列化直接失败，先剥掉
                var rows = JsonConvert.DeserializeObject<List<TableRow>>(text.TrimStart('\uFEFF'));
                if (rows == null) return table;

                foreach (var row in rows)
                {
                    if (row == null || string.IsNullOrEmpty(row.key)) continue;
                    table[row.key] = row.value ?? string.Empty;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[L10nTableStore] 解析语言表失败：{TableFilePrefix}{language}.json\n{e.Message}");
            }

            return table;
        }

        /// <summary>
        /// 按语言与 key 取译文（无状态查询，主要服务于编辑器预览）
        /// <para>Stateless lookup by language and key; mainly serves editor preview.</para>
        /// </summary>
        /// <param name="key">本地化 key<para>Localization key.</para></param>
        /// <param name="language">语言代码（如 en / zh）<para>Language code (e.g. en / zh).</para></param>
        /// <returns>译文；key 或语言不存在时返回 null（回落策略由调用方决定）<para>The translation, or null when the key or language is missing.</para></returns>
        public static string TryGet(string key, string language)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(language)) return null;

            var table = GetOrLoadTable(language);
            // table 理论上不会为 null（GetOrLoadTable 保证返回非 null），防御一下
            if (table == null) return null;

            return table.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// 失效全部缓存，强制下次访问重新发现语言并重新读表（导表同步后调用，使预览立即反映最新译文）
        /// <para>Drops all caches so the next access re-scans and re-reads tables.</para>
        /// </summary>
        public static void Invalidate()
        {
            _discovered = false;
            _hasTables = false;
            _languages = null;
            Tables.Clear();
        }

        #endregion

        // 语言表单行（与导表产物 JSON 结构一一对应，字段名不可改）
        [Serializable]
        private sealed class TableRow
        {
            // 本地化 key
            public string key = string.Empty;

            // 译文
            public string value = string.Empty;
        }
    }
}