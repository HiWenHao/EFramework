/*
 * ================================================
 * Describe:        本地化数据仓库：包内置的默认数据通道，直接读取 Resources 下的导表产物
 * Author:          Alvin5100
 * CreationTime:    2026-10-15 18:10:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-15 18:10:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace EasyFramework.Localization
{
    /// <summary>
    /// 本地化数据仓库
    /// <para>包内置的<strong>唯一</strong>取数通道：直接读取 <c>Resources/L10n</c> 下的
    /// Luban 导表产物 <c>l10n_tbtext_{lang}.json</c>，语言列表按文件名自动发现，
    /// 各语言表在首次访问时才加载并缓存。使用者无需实现任何接口、无需注册任何代码。</para>
    ///
    /// <para><b>为什么直接读导表产物而不是生成类：</b>生成类要导表后才存在，
    /// 且绑定 Luban 的代码生成方案；读数据文件则与生成方案解耦——
    /// 换数据格式只需改导出端，本类与上层服务不受影响。</para>
    ///
    /// <para><b>文件从哪来：</b>Luban 导表输出在 <c>Assets/luban/Data</c>（每次导表会被
    /// 整体清空重建，不能作为常驻资源目录），由编辑器侧的 L10nTablePublisher
    /// 自动同步到 <c>Resources/L10n</c>，使用者无感知。</para>
    ///
    /// <para>Built-in data channel reading Luban-exported <c>l10n_tbtext_{lang}.json</c>
    /// files from <c>Resources/L10n</c>; languages are discovered from file names and
    /// each table is lazily loaded and cached. Zero code is required from consumers.</para>
    /// </summary>
    public static class L10nTableStore
    {
        #region 常量 Constants

        /// <summary>
        /// Resources 下的语言表目录（相对 Resources 根）
        /// <para>与编辑器侧 LocalizationData.Folder（"Resources/L10n"）指向同一目录；
        /// Runtime 看不到 Editor 程序集，故在此各自定义并互相注明。</para>
        /// </summary>
        internal const string ResourcesFolder = "L10n";

        /// <summary>
        /// 语言表文件名前缀，语言代码紧跟其后
        /// <para>完整文件名形如 <c>l10n_tbtext_en.json</c>（Luban 表名 L10n.TbText_en
        /// 导出时统一小写）。新增语言 = 多导一张表，本类零改动。</para>
        /// </summary>
        internal const string TableFilePrefix = "l10n_tbtext_";

        #endregion

        #region 数据模型 Data Models

        /// <summary>
        /// 语言表单行（与导表产物 JSON 结构一一对应）
        /// <para>字段名必须与 Luban 导出的 JSON 键完全一致（key / value），不可改名。</para>
        /// <para>Field names must match the exported JSON keys exactly.</para>
        /// </summary>
        [Serializable]
        private sealed class TableRow
        {
            /// <summary>本地化 key </summary>
            public string key = string.Empty;

            /// <summary>译文 </summary>
            public string value = string.Empty;
        }

        #endregion

        #region 缓存字段 Cached State

        /// <summary>
        /// 是否已完成语言发现
        /// <para>Resources.LoadAll 有 IO 开销，进程内只扫一次；导表同步后经
        /// <see cref="Invalidate"/> 失效重扫。</para>
        /// </summary>
        private static bool _discovered;

        /// <summary>发现的全部语言代码（已按 兜底语言优先 + 字典序 排序） </summary>
        private static List<string> _languages;

        /// <summary>是否发现了至少一张语言表 </summary>
        private static bool _hasTables;

        /// <summary>已加载的语言表缓存：语言代码 → key → 译文 </summary>
        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        #endregion

        #region 公开属性 Public Properties

        /// <summary>
        /// 磁盘上是否存在至少一张语言表
        /// <para>这是「本工程是否使用多语言」的判定闸门：一张表都没有时
        /// 上层服务保持静默，不向未接入多语言的工程刷日志。</para>
        /// <para>Whether at least one language table exists; gates all logging so
        /// projects without localization stay silent.</para>
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
        /// 已发现的全部语言代码（只读）
        /// <para>兜底语言（en）排最前，其余按字典序，保证 UI 下拉与日志顺序稳定。</para>
        /// <para>All discovered language codes, fallback language first.</para>
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

        #region 公开方法 Public Methods

        /// <summary>
        /// 按语言与 key 取译文
        /// <para>刻意做成<strong>无状态</strong>查询：语言由参数传入，
        /// 多语言回落策略集中在 <see cref="I18NManager.Get"/> 一处实现。</para>
        /// <para>Stateless lookup: the language comes in as a parameter.</para>
        /// </summary>
        /// <param name="key">本地化 key</param>
        /// <param name="language">语言代码（如 en / zn）</param>
        /// <returns>译文；key 或语言不存在时返回 null（由调用方决定回落策略）</returns>
        public static string TryGet(string key, string language)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(language)) return null;

            var table = GetOrLoadTable(language);
            // table 理论上不会为 null（GetOrLoadTable 保证返回非 null），防御一下
            if (table == null) return null;

            return table.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// 失效全部缓存，强制下次访问重新发现语言并重新读表
        /// <para>编辑器完成导表同步后调用，使预览与运行时立刻反映最新译文；
        /// 运行期通常不需要调用（语言表视为随包只读数据）。</para>
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

        #region 私有方法 Private Methods

        /// <summary>
        /// 确保已完成一次语言发现
        /// <para>只扫 Resources 目录一次；文件名不符合前缀约定的资源
        /// （如同目录的 LocalizationKeys.json）会被自然过滤掉。</para>
        /// </summary>
        private static void EnsureDiscovered()
        {
            if (_discovered) return;
            _discovered = true;

            _languages = new List<string>();
            var assets = Resources.LoadAll<TextAsset>(ResourcesFolder);
            foreach (var asset in assets)
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

        /// <summary>
        /// 语言排序：兜底语言（en）最前，其余按字典序
        /// </summary>
        private static int CompareLanguages(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.Ordinal)) return 0;
            if (string.Equals(a, LanguageCode.En, StringComparison.Ordinal)) return -1;
            if (string.Equals(b, LanguageCode.En, StringComparison.Ordinal)) return 1;
            return string.CompareOrdinal(a, b);
        }

        /// <summary>
        /// 取指定语言表，未加载时从 Resources 懒加载
        /// <para>保证返回非 null（含空表）：查不到文件 / 解析失败都落为空表并缓存，
        /// 避免同一坏文件反复读盘、反复刷错误日志。</para>
        /// </summary>
        /// <param name="language">语言代码</param>
        /// <returns>该语言的 key → 译文映射（可能为空表，不为 null）</returns>
        private static Dictionary<string, string> GetOrLoadTable(string language)
        {
            if (Tables.TryGetValue(language, out var cached)) return cached;

            var table = LoadTableFromResources(language);
            Tables[language] = table;
            return table;
        }

        /// <summary>
        /// 从 Resources 加载并解析一张语言表
        /// <para>解析失败属于真故障（有表但读不出），会输出错误日志——
        /// 与「一张表都没有 = 正常静默」的闸门约定相区分。</para>
        /// </summary>
        /// <param name="language">语言代码</param>
        /// <returns>解析结果；文件不存在或解析失败返回空表（不为 null）</returns>
        private static Dictionary<string, string> LoadTableFromResources(string language)
        {
            var table = new Dictionary<string, string>(StringComparer.Ordinal);

            var asset = Resources.Load<TextAsset>($"{ResourcesFolder}/{TableFilePrefix}{language}");
            if (asset == null) return table;

            try
            {
                // 导出文件可能带 UTF-8 BOM：BOM 字符混进 JSON 首位会让反序列化直接失败，先剥掉
                var text = asset.text.TrimStart('\uFEFF');
                var rows = JsonConvert.DeserializeObject<List<TableRow>>(text);
                if (rows == null) return table;

                foreach (var row in rows)
                {
                    if (row == null || string.IsNullOrEmpty(row.key)) continue;
                    table[row.key] = row.value ?? string.Empty;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[L10nTableStore] 解析语言表失败：{ResourcesFolder}/{TableFilePrefix}{language}.json\n{e.Message}");
            }

            return table;
        }

        #endregion
    }
}
