/*
 * ================================================
 * Describe:        编辑器下按 key 预览译文的数据桥接（优先直读 Luban 导表产物，回落 Resources）
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 17:35:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-19 18:15:00
 * ScriptVersion:   0.7
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using EasyFramework.Edit;
using EasyFramework.Edit.Windows.Dataable;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// 编辑器下预览译文的数据桥接
    /// <para>Editor-side preview bridge for translations.</para>
    /// <para>取数优先级：编辑态优先直读 <see cref="DatableConfig.LubanDataPath"/> 的导表产物
    /// （导表即所见，无需等待同步）；配置资产不在场或产物缺失时回落 Resources/L10n
    /// （<see cref="L10nTableStore"/>）。Play 模式透传 <see cref="I18NManager"/>
    /// （其语言表经 AssetsManager 按需加载），保证「预览里看到的」等于「游戏里显示的」。</para>
    /// </summary>
    public static class EditorL10nPreview
    {
        #region Fields

        // 未发现语言表时的兜底语言集合（与清单默认语言一致，保证全新工程下拉框也有可选项）
        private static readonly string[] FallbackLanguages = { LanguageCode.Zh, LanguageCode.En };

        // Luban 导表产物文件名前缀（与运行时 I18NManager 的资源命名约定一致）
        private const string TableFilePrefix = "l10n_tbtext_";

        // 缓存的配置资产（Unity fake-null：资产被删除时自动视为 null 并触发重探测）
        private static DatableConfig _config;

        // 下次允许重试查找配置资产的时间（EditorApplication.timeSinceStartup 秒）
        private static double _nextConfigProbe;

        // 配置资产重探测间隔（秒）：Inspector 高频重绘下避免每次都 FindAssets
        private const double ConfigProbeInterval = 2.0;

        // 主通道发现的语言列表缓存（目录时间戳变化才重扫）
        private static List<string> _diskLanguages;

        // 语言列表缓存对应的导出目录时间戳
        private static DateTime _diskLanguagesStamp;

        // 主通道语言表缓存：语言 → 表（key → 译文）
        private static readonly Dictionary<string, Dictionary<string, string>> DiskTables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        // 语言表缓存对应的文件时间戳：语言 → LastWriteTimeUtc（导表重写后自动重读）
        private static readonly Dictionary<string, DateTime> DiskTableStamps =
            new Dictionary<string, DateTime>(StringComparer.Ordinal);

        #endregion

        #region Properties

        /// <summary>
        /// 编辑器预览当前使用的语言（仅编辑态；经 EditorPrefs 持久化，跨编译域重载保持上次选择）
        /// <para>Editor preview language; edit mode only, persisted via EditorPrefs.</para>
        /// <para>Play 模式下预览跟随运行时语言（见 <see cref="FollowsRuntimeLanguage"/>），本值不参与取词。</para>
        /// </summary>
        public static string PreviewLanguage
        {
            get => LocalizationData.PreviewLanguage;
            set => LocalizationData.PreviewLanguage = value;
        }

        /// <summary>
        /// 预览是否正跟随运行时语言
        /// <para>Whether the preview follows the runtime language.</para>
        /// <para>Play 模式且 I18NManager 已确定当前语言时为 true；编辑态恒为 false。
        /// 短路顺序保证编辑态绝不触碰 I18NManager（避免其单例在编辑态被创建）。</para>
        /// </summary>
        public static bool FollowsRuntimeLanguage
            => Application.isPlaying
               && I18NManager.Instance != null
               && !string.IsNullOrEmpty(I18NManager.Instance.CurrentLanguage);

        /// <summary>
        /// 预览数据是否可用（导表产物或 Resources 副本任一存在语言表；供 Inspector 区分「没有语言表」与「表里缺词」）
        /// <para>Whether preview data is available from either the export folder or Resources.</para>
        /// </summary>
        public static bool IsDataAvailable => UseDiskChannel() || L10nTableStore.HasTables;

        /// <summary>
        /// 可选的预览语言集合（优先导表产物发现；不可用时回落 Resources 与兜底集合）
        /// <para>Available preview languages; export folder first, Resources fallback.</para>
        /// </summary>
        public static IReadOnlyList<string> AvailableLanguages
        {
            get
            {
                var dir = GetExportDir();
                if (dir != null)
                {
                    var disk = GetDiskLanguages(dir);
                    if (disk.Count > 0) return disk;
                }

                return L10nTableStore.HasTables ? L10nTableStore.Languages : FallbackLanguages;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 切换运行时语言（Play 模式下由预览下拉调用；装载为异步过程，此处不等待，完成后自动广播刷新）
        /// <para>Switches the runtime language from the preview dropdown (play mode only).</para>
        /// </summary>
        public static void TrySwitchRuntimeLanguage(string language)
        {
            if (Application.isPlaying && I18NManager.Instance != null)
                _ = I18NManager.Instance.SwitchLanguage(language);
        }

        /// <summary>
        /// 取译文预览
        /// <para>Looks up the preview translation.</para>
        /// </summary>
        /// <param name="key">本地化 key<para>Localization key.</para></param>
        /// <returns>译文；未命中或数据不可用时返回 null（由调用方决定显示占位还是提示）<para>The translation, or null on a miss.</para></returns>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            // 运行态：直接问运行时服务（与场景文本同源）；[Missing:xxx] 占位在预览中等同于缺词，返回 null 交调用方提示
            if (Application.isPlaying && I18NManager.Instance != null)
            {
                var runtime = I18NManager.Instance.Get(key);
                return runtime != null && !runtime.StartsWith(I18NManager.MissingPrefix, StringComparison.Ordinal)
                    ? runtime
                    : null;
            }

            // 编辑态主通道：直读导表产物（导表即所见），当前语言缺词回落 en；产物存在时独占数据源，
            // 不与 Resources 混读——主通道是最新事实，缺词就是真缺词，混读旧副本反而出错
            if (UseDiskChannel())
            {
                var value = TryGetFromDisk(key, PreviewLanguage);
                if (string.IsNullOrEmpty(value) && PreviewLanguage != LanguageCode.En)
                    value = TryGetFromDisk(key, LanguageCode.En);

                return string.IsNullOrEmpty(value) ? null : value;
            }

            // 编辑态回退通道：Resources（配置不在场 / 导出目录为空时），当前语言缺词回落 en
            var fallback = L10nTableStore.TryGet(key, PreviewLanguage);

            if (string.IsNullOrEmpty(fallback) && PreviewLanguage != LanguageCode.En)
                fallback = L10nTableStore.TryGet(key, LanguageCode.En);

            return string.IsNullOrEmpty(fallback) ? null : fallback;
        }

        /// <summary>
        /// 使数据缓存失效，下次取值重新探测配置、发现语言并重读表（导表产物被覆盖后可手动调用）
        /// <para>Invalidates all caches (config probe, language list, tables) and the Resources channel.</para>
        /// </summary>
        public static void Invalidate()
        {
            // 配置资产也可能刚创建（首次导表前不存在），一并触发重探测
            _config = null;
            _diskLanguages = null;
            DiskTables.Clear();
            DiskTableStamps.Clear();
            L10nTableStore.Invalidate();
        }

        // 主通道是否可用：配置资产存在、导出目录存在且至少发现一张语言表
        private static bool UseDiskChannel()
        {
            var dir = GetExportDir();
            return dir != null && GetDiskLanguages(dir).Count > 0;
        }

        // 获取导表产物目录的绝对路径；配置资产不存在 / 路径为空 / 目录不存在时返回 null
        private static string GetExportDir()
        {
            // 节流重探测：找不到时不放弃，间隔后重试（首次创建配置资产后无需域重载即可生效）
            if (_config == null && EditorApplication.timeSinceStartup >= _nextConfigProbe)
            {
                _nextConfigProbe = EditorApplication.timeSinceStartup + ConfigProbeInterval;
                if (EditorUtils.CheckAssets<DatableConfig>(out var path))
                    _config = EditorUtils.LoadSettingAtPath<DatableConfig>(path);
            }

            // Unity fake-null：资产被删除时回到 null 分支
            if (_config == null) return null;
            if (string.IsNullOrWhiteSpace(_config.LubanDataPath)) return null;

            string dir = ResolveProjectPath(_config.LubanDataPath);
            return Directory.Exists(dir) ? dir : null;
        }

        // 归一化 LubanDataPath 为绝对路径（相对值按工程根拼接，绝对值直接使用）
        private static string ResolveProjectPath(string path)
        {
            string normalized = path.Replace('\\', '/').Trim();
            if (Path.IsPathRooted(normalized)) return normalized;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, normalized));
        }

        // 扫描导出目录，从文件名发现语言（目录时间戳变化才重扫；en 优先排序）
        private static List<string> GetDiskLanguages(string dir)
        {
            var stamp = Directory.GetLastWriteTimeUtc(dir);
            if (_diskLanguages != null && stamp == _diskLanguagesStamp) return _diskLanguages;

            var languages = new List<string>();
            foreach (string file in Directory.GetFiles(dir, TableFilePrefix + "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var language = name.StartsWith(TableFilePrefix, StringComparison.Ordinal)
                    ? name.Substring(TableFilePrefix.Length)
                    : string.Empty;

                if (string.IsNullOrEmpty(language) || languages.Contains(language)) continue;
                languages.Add(language);
            }

            languages.Sort(CompareLanguages);
            _diskLanguages = languages;
            _diskLanguagesStamp = stamp;
            return languages;
        }

        // 语言排序：兜底语言（en）最前，其余按字典序
        private static int CompareLanguages(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.Ordinal)) return 0;
            if (string.Equals(a, LanguageCode.En, StringComparison.Ordinal)) return -1;
            if (string.Equals(b, LanguageCode.En, StringComparison.Ordinal)) return 1;
            return string.CompareOrdinal(a, b);
        }

        // 主通道取词（文件时间戳缓存：导表重写后自动重读，未变直接用缓存）
        private static string TryGetFromDisk(string key, string language)
        {
            string dir = GetExportDir();
            if (dir == null) return null;

            string path = Path.Combine(dir, TableFilePrefix + language + ".json");
            if (!File.Exists(path)) return null;

            var stamp = File.GetLastWriteTimeUtc(path);
            if (!DiskTables.TryGetValue(language, out var table) || DiskTableStamps[language] != stamp)
            {
                table = ParseTable(File.ReadAllText(path));
                DiskTables[language] = table;
                DiskTableStamps[language] = stamp;
            }

            return table.TryGetValue(key, out var value) ? value : null;
        }

        // 解析语言表 JSON（剥 BOM；解析失败输出错误日志并按空表处理）
        private static Dictionary<string, string> ParseTable(string text)
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
                D.Error(LC.Tr("[EditorL10nPreview] 解析语言表失败\n{0}",
                    "[EditorL10nPreview] Failed to parse the language table\n{0}",
                    e.Message));
            }

            return table;
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
