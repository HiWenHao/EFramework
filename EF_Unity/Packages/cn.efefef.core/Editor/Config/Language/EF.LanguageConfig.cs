/*
 * ================================================
 * Describe:      编辑器多语言系统（一语言一文件 + 组合 + 复合语句）
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-28 16:14:49
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-10-29
 * ScriptVersion: 1.0
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 编辑器多语言辅助类（各包键清单即唯一数据源 + 作用域键）
    /// <para>Editor localization helper where each package's bilingual key list
    /// "Editor Resources/Description/Editorlanguages.json" is the single data source: entries
    /// (array[0]=English, array[1]=Chinese) are merged into the runtime registry by language
    /// code — no standalone translation files and no registration inside core are required.
    /// Package entries use "{PackageShortName}.{EntryName}" scoped keys queried via
    /// Combine(string)/Format(string); Extensible languages live in
    /// "{projectRoot}/EFTools/EditorLanguage/{code}.json". Supports word combining and
    /// composite sentences with "{0}" placeholders.</para>
    /// </summary>
    public static class LC
    {
        #region Fields

        private const string PackagesRoot = "Packages";                                     // 包扫描根目录名（拼接到工程绝对路径使用）
        private const string KeyListDir = "Editor Resources/Description";                   // 各包双语键清单目录约定（包内相对路径）
        private const string KeyListFileName = "Editorlanguages.json";                      // 各包双语键清单文件名（唯一语言数据源 + 维护入口）
        private const string ExternalLanguageDirName = "EFTools/EditorLanguage";            // 工程扩展语言目录（相对工程根）

        private const string AutoCode = "Auto";              // 自动跟随系统语言的选择值

        private static readonly Regex ScopedKeyPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$", RegexOptions.Compiled); // 语言键词法校验（允许 "包名.键"；枚举生成前键尚不存在，不能依赖 TryParse）
        private static readonly object _lock = new object();   // 加载线程锁

        private static List<LanguageRuntime> _languages;       // 语言注册表：内置 zh/en 在前 + 外部按 code 排序
        private static bool _loaded;                            // 注册表是否已加载
        private static bool _loading;                           // 加载中标志（防 Tr→IsEnglish→EnsureLoaded 同线程重入递归）

        /// <summary>语言文件 JSON 模型<para>JSON model of a language file.</para></summary>
        private sealed class LanguageFileModel
        {
            // 语言 code（缺失回落文件名）
            public string code;
            // 下拉显示名（缺失回落 code）
            public string displayName;
            // 可选 SystemLanguage 枚举名（Auto 推导用；null 时输出省略）
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string systemLanguage;
            // 词条集合
            public List<EntryModel> entries;
            // 未知字段兜底（同步器回写时防丢失）
            [JsonExtensionData] public Dictionary<string, JToken> ExtraData;
        }

        /// <summary>语言词条 JSON 模型<para>JSON model of a language entry.</para></summary>
        private sealed class EntryModel
        {
            // 枚举键名
            public string key;
            // 描述（仅供翻译参考；null 时输出省略）
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string desc;
            // 文本；null = 未翻译回落 English（模板中显式输出），空串 = 有意空文本（采用）
            public string text;
            // 占位符说明（仅供翻译参考，加载时忽略；null 时输出省略）
            [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)] public List<string> ParamHints;
            // 未知字段兜底（同步器回写时防丢失）
            [JsonExtensionData] public Dictionary<string, JToken> ExtraData;
        }

        /// <summary>旧版双语键清单 JSON 模型（Editorlanguages.json，维护入口）<para>Legacy bilingual key list model (Editorlanguages.json).</para></summary>
        private sealed class LegacyLanguageModel
        {
            // 键条目集合
            public List<LegacyItemModel> LcList;
        }

        /// <summary>旧版键清单条目（双语并排维护）<para>Legacy key list item (bilingual, side by side).</para></summary>
        private sealed class LegacyItemModel
        {
            // 枚举键名
            public string name;
            // 描述（用作枚举注释）
            public string desc;
            // 多语言文本：[0]=English, [1]=中文
            public List<string> array;
            // 可选占位符说明（复合语句用）
            [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)] public List<string> ParamHints;
        }

        /// <summary>单个包的键清单整合结果<para>Merged key list of one package.</para></summary>
        private sealed class PackageKeyList
        {
            // 包目录（"Packages/cn.efefef.launcher" 形式的工程相对路径）
            public string PackageDir;
            // 键条目（name 需通过作用域键校验）
            public List<LegacyItemModel> Items;
        }

        /// <summary>加载后的语言运行时数据<para>Loaded runtime language data.</para></summary>
        private sealed class LanguageRuntime
        {
            // 语言 code（持久化与查询标识）
            public string Code;
            // 下拉显示名
            public string DisplayName;
            // 可选 SystemLanguage 枚举名
            public string SystemLanguage;
            // 键 -> 文本（字符串作用域键；未翻译条目不入典；忽略大小写以兼容旧枚举名解析）
            public readonly Dictionary<string, string> Texts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Properties

        /// <summary>
        /// 编辑器 UI 语言（"Auto" / 语言 code），持久化于 ProjectConfig 资产（随工程 git 同步，全队生效）
        /// <para>Editor UI language persisted in the ProjectConfig asset: "Auto" lets every
        /// machine follow its own system language, while an explicit choice applies to the
        /// whole team via version control.</para>
        /// </summary>
        public static string EditorLanguageCode
        {
            get
            {
                var project = ConfigManager.Project;
                if (project == null)
                    return AutoCode;
                if (project.AutoLanguage)
                    return AutoCode;
                return project.DisplayLanguage.ToString();
            }
            set
            {
                var project = ConfigManager.Project;
                if (project == null)
                {
                    D.Warning(Tr("[ LC ] ProjectConfig 未就绪，语言选择未保存。",
                        "[ LC ] ProjectConfig is not ready; the language choice is not saved."));
                    return;
                }
                string next = string.IsNullOrEmpty(value) ? AutoCode : value;
                if (EditorLanguageCode == next)
                    return;
                if (next == AutoCode)
                {
                    project.AutoLanguage = true;
                }
                else if (Enum.TryParse(next, out SystemLanguage language))
                {
                    project.AutoLanguage = false;
                    project.DisplayLanguage = language;
                }
                else
                {
                    // 非枚举名 code（旧手工格式如 "ja"）无法入 SO，回落自动
                    D.Warning(Tr($"[ LC ] 未知语言代码 '{next}'，已回落自动。",
                        $"[ LC ] Unknown language code '{next}', fallback to Auto."));
                    project.AutoLanguage = true;
                }
                EditorUtility.SetDirty(project);
            }
        }

        /// <summary>
        /// 当前编辑器语言是否为英文回落语言
        /// <para>Whether the resolved editor language is the English fallback; non-English
        /// (including external languages) renders built-in bilingual text as Chinese.</para>
        /// </summary>
        public static bool IsEnglish => ResolveCode() == nameof(SystemLanguage.English);

        /// <summary>
        /// 当前编辑器语言是否为中文
        /// <para>Whether the resolved editor language is Chinese.</para>
        /// </summary>
        public static bool IsChinese => ResolveCode() == nameof(SystemLanguage.Chinese);

        #endregion

        #region Methods

        /// <summary>
        /// 加载与扫描语言数据（线程安全，坏文件跳过不炸面板）
        /// <para>Load and scan language data (thread-safe; broken files are skipped).</para>
        /// </summary>
        private static void EnsureLoaded()
        {
            if (_loaded || _loading)
                return;
            lock (_lock)
            {
                if (_loaded || _loading)
                    return;
                _loading = true;
                try
                {
                    _languages = new List<LanguageRuntime>();
                    LoadPackageLanguages();
                    LoadExternal();
                    SortLanguages();
                    EnsureFallbackLanguages();
                    _loaded = true;
                }
                finally
                {
                    _loading = false;
                }
            }
        }

        /// <summary>
        /// 从全部包的键清单构建中文/英文运行时语言数据（键清单即唯一数据源，无独立翻译文件）
        /// <para>Build the Chinese/English runtime language data from every package's key list
        /// (the key list is the single data source; no standalone translation files).</para>
        /// </summary>
        /// <remarks>
        /// 每条键清单条目的 array[0]=English、array[1]=中文，分别入对应语言的词条表；
        /// 某一侧缺失时该侧不入典（查询链回落另一侧或占位符）。
        /// <para>Each key list entry provides array[0]=English and array[1]=Chinese into the
        /// matching language table; a missing side is not added (the query chain falls back).</para>
        /// </remarks>
        private static void LoadPackageLanguages()
        {
            var packageLists = CollectPackageKeyLists(new List<string>());
            foreach (var package in packageLists)
            {
                foreach (var item in package.Items)
                {
                    if (TryGetListItemText(item, 0, out string english))
                        AddPackageText(nameof(SystemLanguage.English), item.name, english);
                    if (TryGetListItemText(item, 1, out string chinese))
                        AddPackageText(nameof(SystemLanguage.Chinese), item.name, chinese);
                }
            }
        }

        /// <summary>
        /// 读取键清单条目的指定侧文本（null 或数组缺失视为未提供；空串为有意空文本，正常采用）
        /// <para>Read one side of a key list entry's bilingual text (null or a missing array means
        /// not provided; an empty string is intentional empty text and is adopted as-is).</para>
        /// </summary>
        /// <param name="item">键清单条目<para>Key list item.</para></param>
        /// <param name="textIndex">文本数组下标（0=English, 1=中文）<para>Text array index.</para></param>
        /// <param name="text">命中文本<para>Matched text.</para></param>
        /// <returns>存在非 null 文本返回 true<para>True when a non-null text exists.</para></returns>
        private static bool TryGetListItemText(LegacyItemModel item, int textIndex, out string text)
        {
            text = null;
            // 注意：空串与 null 语义不同（空串=有意空文本，如中文分隔符；null=该侧未提供，查询时回落）
            if (item?.array == null || item.array.Count <= textIndex || item.array[textIndex] == null)
                return false;
            text = item.array[textIndex];
            return true;
        }

        /// <summary>
        /// 将词条文本写入指定语言的运行时表（语言不存在时按兜底元数据创建）
        /// <para>Add one entry text into the runtime table of the given language (the language is
        /// created with builtin metadata when absent).</para>
        /// </summary>
        /// <param name="code">语言 code（English/Chinese）<para>Language code (English/Chinese).</para></param>
        /// <param name="key">词条键<para>Entry key.</para></param>
        /// <param name="text">文本<para>Text.</para></param>
        private static void AddPackageText(string code, string key, string text)
        {
            var language = FindLanguage(code);
            if (language == null)
            {
                // 显示名与系统语言标识与原内置语言文件一致（下拉显示与 Auto 推导依赖）
                string displayName = code;
                string systemLanguage = code;
                if (string.Equals(code, nameof(SystemLanguage.Chinese), StringComparison.OrdinalIgnoreCase))
                {
                    displayName = "中文";
                    systemLanguage = nameof(SystemLanguage.ChineseSimplified);
                }
                language = new LanguageRuntime { Code = code, DisplayName = displayName, SystemLanguage = systemLanguage };
                _languages.Add(language);
            }
            if (language.Texts.ContainsKey(key))
                return; // CollectPackageKeyLists 已过滤跨包同名键，此处为防御
            language.Texts.Add(key, text);
        }

        /// <summary>
        /// 注册表排序：中文、英文置前（兜底语言），其余按 code 字母序
        /// <para>Sort the registry: Chinese and English first (fallback languages), the rest by code ordinal.</para>
        /// </summary>
        private static void SortLanguages()
        {
            _languages.Sort((a, b) =>
            {
                int weightA = GetSortWeight(a.Code);
                int weightB = GetSortWeight(b.Code);
                if (weightA != weightB)
                    return weightA - weightB;
                return string.Compare(a.Code, b.Code, StringComparison.Ordinal);
            });
        }

        /// <summary>
        /// 语言排序权重（0=中文, 1=英文, 2=其余）
        /// <para>Sort weight (0=Chinese, 1=English, 2=others).</para>
        /// </summary>
        /// <param name="code">语言 code<para>Language code.</para></param>
        /// <returns>排序权重<para>The sort weight.</para></returns>
        private static int GetSortWeight(string code)
        {
            if (string.Equals(code, nameof(SystemLanguage.Chinese), StringComparison.OrdinalIgnoreCase))
                return 0;
            if (string.Equals(code, nameof(SystemLanguage.English), StringComparison.OrdinalIgnoreCase))
                return 1;
            return 2;
        }

        /// <summary>
        /// 校验兜底语言（中文/英文）存在，缺失时报错并给出修复指引（查询链会退化为占位符）
        /// <para>Verify fallback languages (Chinese/English) exist; log an error with a repair
        /// hint when missing (the query chain degrades to placeholders).</para>
        /// </summary>
        /// <remarks>
        /// 键清单即唯一数据源：兜底语言可由任一包的 Editorlanguages.json 提供；缺失时在任一包键清单补回
        /// 词条并跑同步菜单（刷新运行时缓存）即可恢复。
        /// <para>The key list is the single data source: fallback languages may come from any
        /// package's Editorlanguages.json; restore by adding entries back to any package key list
        /// and running the sync menu (which reloads the runtime cache).</para>
        /// </remarks>
        private static void EnsureFallbackLanguages()
        {
            string repairHint = Tr("请在任一包的 Editorlanguages.json 补回双语词条，并跑菜单 \"✍️ Update Edit Language\" 刷新。",
                "Restore bilingual entries in any package's Editorlanguages.json, then run the menu \"✍️ Update Edit Language\" to refresh.");
            if (FindLanguage(nameof(SystemLanguage.Chinese)) == null)
                D.Error(Tr($"兜底语言 '{nameof(SystemLanguage.Chinese)}' 缺失：中文文案不可用。",
                    $"Fallback language '{nameof(SystemLanguage.Chinese)}' is missing: Chinese texts are unavailable.") + " " + repairHint);
            if (FindLanguage(nameof(SystemLanguage.English)) == null)
                D.Error(Tr($"兜底语言 '{nameof(SystemLanguage.English)}' 缺失：未翻译词条无法回落英文。",
                    $"Fallback language '{nameof(SystemLanguage.English)}' is missing: untranslated entries cannot fall back to English.") + " " + repairHint);
        }

        /// <summary>
        /// 扫描并加载工程扩展语言目录（EFTools/EditorLanguage）
        /// <para>Scan and load external language files from the project folder.</para>
        /// </summary>
        private static void LoadExternal()
        {
            string dir = GetExternalLanguageDirectory();
            if (!Directory.Exists(dir))
                return;

            var externals = new List<LanguageRuntime>();
            foreach (string file in Directory.GetFiles(dir, "*.json"))
            {
                string fallbackCode = Path.GetFileNameWithoutExtension(file);
                var runtime = ParseLanguageFile(ReadFileSafe(file), fallbackCode);
                if (runtime == null)
                    continue;
                if (FindLanguage(runtime.Code) != null)
                {
                    D.Warning(Tr($"语言 code '{runtime.Code}' 重复，已跳过（文件：{file}）",
                        $"Duplicate language code '{runtime.Code}' skipped (file: {file})"));
                    continue;
                }
                externals.Add(runtime);
            }
            externals.Sort((a, b) => string.Compare(a.Code, b.Code, StringComparison.Ordinal));
            _languages.AddRange(externals);
        }

        /// <summary>
        /// 解析单个语言 JSON 文件为运行时数据
        /// <para>Parse one language JSON file into runtime data.</para>
        /// </summary>
        /// <param name="json">文件内容<para>File content.</para></param>
        /// <param name="fallbackCode">code 缺失时的回落值<para>Fallback when code is absent.</para></param>
        /// <returns>解析失败返回 null（已告警）<para>Null on failure (warning logged).</para></returns>
        private static LanguageRuntime ParseLanguageFile(string json, string fallbackCode)
        {
            if (json == null)
                return null;
            try
            {
                var model = JsonConvert.DeserializeObject<LanguageFileModel>(json);
                if (model?.entries == null)
                {
                    D.Warning(Tr($"无效的语言文件（缺少 entries），已跳过：{fallbackCode}",
                        $"Invalid language file (entries missing), skipped: {fallbackCode}"));
                    return null;
                }

                var runtime = new LanguageRuntime
                {
                    Code = string.IsNullOrEmpty(model.code) ? fallbackCode : model.code,
                    DisplayName = string.IsNullOrEmpty(model.displayName) ? (string.IsNullOrEmpty(model.code) ? fallbackCode : model.code) : model.displayName,
                    SystemLanguage = model.systemLanguage,
                };

                int skipped = 0;
                foreach (var entry in model.entries)
                {
                    if (entry == null || entry.text == null)
                        continue; // text=null 表示未翻译，回落 English
                    if (string.IsNullOrEmpty(entry.key))
                    {
                        skipped++;
                        continue;
                    }
                    runtime.Texts[entry.key] = entry.text;
                }
                if (skipped > 0)
                    D.Warning(Tr($"语言 '{runtime.Code}' 中有 {skipped} 个空键词条已跳过",
                        $"{skipped} entries with empty keys skipped in language '{runtime.Code}'"));
                return runtime;
            }
            catch (Exception ex)
            {
                D.Warning(Tr($"解析语言文件 '{fallbackCode}' 失败：{ex.Message}",
                    $"Failed to parse language file '{fallbackCode}': {ex.Message}"));
                return null;
            }
        }

        /// <summary>
        /// 解析当前生效的编辑器语言 code（Auto 跟随系统）
        /// <para>Resolve the effective editor language code (Auto follows system).</para>
        /// </summary>
        /// <returns>生效 code；解析结果不在注册表时查询链自然回落 English<para>Effective code; unknown codes fall back to English in the query chain.</para></returns>
        /// <remarks>不缓存解析结果：Auto 模式依赖系统语言这一外部状态，缓存会使其失效时机难以对齐；
        /// 解析本身只有枚举比较与一次线性查找，开销可忽略。<para>No result cache: Auto depends on the external
        /// system language; resolving is cheap enumeration comparison.</para></remarks>
        private static string ResolveCode()
        {
            string raw = EditorLanguageCode;
            return AutoCode.Equals(raw, StringComparison.Ordinal) ? ResolveAutoCode() : raw;
        }

        /// <summary>
        /// Auto 模式下按系统语言推导编辑器语言
        /// <para>Derive the editor language from the system language in Auto mode.</para>
        /// </summary>
        /// <returns>推导 code；无法匹配时回落 English（繁体系统回落简体）<para>Derived code; English when unmatched (Traditional Chinese falls back to Simplified).</para></returns>
        private static string ResolveAutoCode()
        {
            EnsureLoaded();
            var system = Application.systemLanguage;
            if (system == SystemLanguage.Chinese || system == SystemLanguage.ChineseSimplified)
                return nameof(SystemLanguage.Chinese);
            if (system == SystemLanguage.English)
                return nameof(SystemLanguage.English);
            // 其余系统语言（含繁体中文）：优先命中外部语言声明的 systemLanguage，其次按 code 同名匹配
            foreach (var language in _languages)
            {
                string declared = string.IsNullOrEmpty(language.SystemLanguage) ? language.Code : language.SystemLanguage;
                if (declared == system.ToString())
                    return language.Code;
            }
            if (system == SystemLanguage.ChineseTraditional)
                return nameof(SystemLanguage.Chinese); // 繁体系统未建繁体语言时回落简体
            return nameof(SystemLanguage.English);
        }

        /// <summary>
        /// 按 code 查找注册表中的语言
        /// <para>Find a registered language by code.</para>
        /// </summary>
        /// <param name="code">语言 code<para>Language code.</para></param>
        /// <returns>未命中返回 null<para>Null when not found.</para></returns>
        private static LanguageRuntime FindLanguage(string code)
        {
            foreach (var language in _languages)
            {
                if (string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase))
                    return language;
            }
            return null;
        }

        /// <summary>
        /// 查询链：当前语言 -> 回落 English（键为字符串作用域键或枚举名）
        /// <para>Query chain: current language -> English fallback (key is a scoped string key or enum name).</para>
        /// </summary>
        /// <param name="key">语言键<para>Language key.</para></param>
        /// <param name="text">命中文本<para>Matched text.</para></param>
        /// <returns>命中返回 true<para>True when found.</para></returns>
        private static bool TryGetText(string key, out string text)
        {
            EnsureLoaded();
            var language = FindLanguage(ResolveCode());
            if (language != null && language.Texts.TryGetValue(key, out text))
                return true;
            var fallback = FindLanguage(nameof(SystemLanguage.English));
            if (fallback != null && fallback.Texts.TryGetValue(key, out text))
                return true;
            text = null;
            return false;
        }

        /// <summary>
        /// 获取单个作用域键的本地化文本（"{包短名}.{词条名}"，各包词条入口）
        /// <para>Get localized text for a single scoped key ("{PackageShortName}.{EntryName}"),
        /// the entry point for package-owned entries.</para>
        /// </summary>
        /// <param name="key">作用域键<para>Scoped key.</para></param>
        /// <returns>对应文本；未命中返回 "[键名]" 占位符便于排查<para>The text; "[key]" placeholder when missing.</para></returns>
        public static string Combine(string key)
        {
            return TryGetText(key, out var text) ? text : $"[{key}]";
        }

        /// <summary>
        /// 使用当前语言的分隔符（键 "S"）拼接多个作用域键
        /// <para>Combine multiple scoped keys with the current separator (key "S").</para>
        /// </summary>
        /// <param name="keys">要拼接的作用域键数组<para>Scoped keys to combine.</para></param>
        /// <returns>拼接后的字符串；空数组返回空串<para>Combined string; empty for no keys.</para></returns>
        public static string Combine(params string[] keys)
        {
            if (keys == null || keys.Length == 0)
                return string.Empty;
            if (keys.Length == 1)
                return Combine(keys[0]);
            return JoinTexts(keys.Length, i => Combine(keys[i]));
        }

        /// <summary>
        /// 以当前语言分隔符拼接已解析文本的核心实现
        /// <para>Core helper joining resolved texts with the current separator.</para>
        /// </summary>
        /// <param name="count">文本数量<para>Number of texts.</para></param>
        /// <param name="resolve">按下标取文本的委托<para>Resolver from index to text.</para></param>
        /// <returns>拼接后的字符串<para>The combined string.</para></returns>
        private static string JoinTexts(int count, Func<int, string> resolve)
        {
            string separator = Combine("S");
            var builder = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                builder.Append(resolve(i));
                if (i < count - 1)
                    builder.Append(separator);
            }
            return builder.ToString();
        }

        /// <summary>
        /// 复合语句（作用域键）：取当前语言模板并填充 "{0}" 占位符
        /// <para>Composite sentence by scoped key: format the current language template with
        /// "{0}" placeholders.</para>
        /// </summary>
        /// <param name="key">模板作用域键<para>Template scoped key.</para></param>
        /// <param name="args">占位符参数<para>Placeholder args.</para></param>
        /// <returns>填充后的句子；占位符与参数不匹配时降级返回模板原文并告警<para>The formatted sentence; degrades to the raw template with a warning on mismatch.</para></returns>
        public static string Format(string key, params object[] args)
        {
            string template = Combine(key);
            if (args == null || args.Length == 0)
                return template;
            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                D.Warning(Tr("[ LC ] 格式化失败（占位符与参数不匹配），返回模板原文：",
                    "[ LC ] Format failed (placeholders do not match args), returning template: ") + template);
                return template;
            }
        }

        /// <summary>
        /// 按当前编辑器语言选中文案（二值：非英文即中文）；args 非空时执行 string.Format
        /// <para>Select bilingual text by the current editor language (non-English renders Chinese); formats when args provided.</para>
        /// </summary>
        /// <param name="zh">中文文案<para>Chinese text.</para></param>
        /// <param name="en">英文文案<para>English text.</para></param>
        /// <param name="args">占位符参数；不传时原文返回<para>Placeholder args; raw text when omitted.</para></param>
        /// <returns>选中（并按需格式化）的文案<para>The selected and optionally formatted text.</para></returns>
        public static string Tr(string zh, string en, params object[] args)
        {
            string text = IsEnglish ? en : zh;
            if (args == null || args.Length == 0)
                return text;
            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                // Tr 自身的降级告警不可再调 Tr（自指递归），故用 IsEnglish 三元
                D.Warning(IsEnglish
                    ? $"[ LC ] Tr format failed (placeholders do not match args), returning raw text: {text}"
                    : $"[ LC ] Tr 格式化失败（占位符与参数不匹配），返回原文：{text}");
                return text;
            }
        }

        /// <summary>
        /// 语言选项快照：[Auto, 中文, English] + 扩展语言（按 code 字母序）
        /// <para>Language option snapshot: [Auto, Chinese, English] + external languages (by code).</para>
        /// </summary>
        /// <returns>只读选项列表<para>Read-only option list.</para></returns>
        public static IReadOnlyList<LanguageOption> GetLanguageOptions()
        {
            EnsureLoaded();
            var options = new List<LanguageOption> { new LanguageOption(AutoCode, IsChinese ? "自动" : "Auto") };
            foreach (var language in _languages)
                options.Add(new LanguageOption(language.Code, language.DisplayName));
            return options;
        }

        /// <summary>
        /// 当前是否为自动跟随系统语言模式
        /// <para>Whether the Auto (follow system language) mode is active.</para>
        /// </summary>
        public static bool IsAutoLanguage => EditorLanguageCode == AutoCode;

        /// <summary>
        /// 当前选择在选项列表中的下标（按原始选择匹配：Auto 模式选中「自动」项而非推导结果）；未命中回落 0
        /// <para>Index of the raw selection (Auto mode selects the "Auto" item, not the derived
        /// result); 0 when unmatched.</para>
        /// </summary>
        /// <returns>选项下标<para>Option index.</para></returns>
        public static int GetSelectedIndex()
        {
            var options = GetLanguageOptions();
            string code = EditorLanguageCode;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].Code == code)
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// 当前生效语言的显示名（Auto 显示推导结果）
        /// <para>Display name of the effective language (Auto shows the derived one).</para>
        /// </summary>
        /// <returns>显示名；未命中回落 English<para>The display name; English when unmatched.</para></returns>
        public static string GetCurrentDisplayName()
        {
            EnsureLoaded();
            return FindLanguage(ResolveCode())?.DisplayName ?? nameof(SystemLanguage.English);
        }

        /// <summary>
        /// 重扫内置与扩展语言数据（面板打开、新增语言、外部手工放文件后调用）
        /// <para>Rescan builtin and external languages; call on panel enable, after creating
        /// a language, or after manually dropping files.</para>
        /// </summary>
        public static void ReloadLanguages()
        {
            lock (_lock)
            {
                _loaded = false;
                _languages = null;
            }
            EnsureLoaded();
        }

        /// <summary>
        /// 整合全部包键清单的词条检视列表（按包分组、键排序）
        /// <para>Merged entry list from every package's key list for inspection (grouped by
        /// package, ordered by key).</para>
        /// </summary>
        /// <returns>词条信息平铺列表（含包名字段，供窗口分组）<para>Flat entry list with a package-name field for window grouping.</para></returns>
        public static List<LanguageEntryInfo> CollectEntriesForInspection()
        {
            var result = new List<LanguageEntryInfo>();
            foreach (var package in CollectPackageKeyLists(new List<string>()))
            {
                string packageName = Path.GetFileName(package.PackageDir);
                foreach (var item in package.Items)
                {
                    TryGetListItemText(item, 1, out string chinese);
                    TryGetListItemText(item, 0, out string english);
                    result.Add(new LanguageEntryInfo
                    {
                        PackageName = packageName,
                        Key = item.name,
                        Desc = item.desc,
                        Chinese = chinese,
                        English = english,
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// 获取扩展语言文件完整路径
        /// <para>Get the full path of an external language file.</para>
        /// </summary>
        /// <param name="code">语言 code<para>Language code.</para></param>
        /// <returns>{projectRoot}/EFTools/EditorLanguage/{code}.json<para>The file path.</para></returns>
        public static string GetExternalLanguagePath(string code)
        {
            return Path.Combine(GetExternalLanguageDirectory(), code + ".json");
        }

        /// <summary>
        /// 判断系统语言是否已被内置语言表覆盖（不允许再建同名外部语言）
        /// <para>Whether the system language is already covered by a builtin language table
        /// (creating an external language with it is rejected).</para>
        /// </summary>
        /// <param name="systemLanguage">系统语言<para>The system language.</para></param>
        /// <returns>已被覆盖返回 true<para>True when covered.</para></returns>
        public static bool IsBuiltinCoveredLanguage(SystemLanguage systemLanguage)
        {
            return systemLanguage == SystemLanguage.English ||
                   systemLanguage == SystemLanguage.Chinese ||
                   systemLanguage == SystemLanguage.ChineseSimplified;
        }

        /// <summary>
        /// 校验并生成扩展语言模板（以系统语言为标识，全键 text=null 待翻译、保留 desc 与占位符说明）
        /// <para>Validate and create an external language template keyed by a SystemLanguage
        /// (all keys, text=null for translation, keeping desc and placeholder hints).</para>
        /// </summary>
        /// <param name="systemLanguage">系统语言——作为语言标识（code/文件名）与 Auto 推导依据<para>System language used as the code, file name and Auto key.</para></param>
        /// <param name="displayName">下拉显示名（空缺省用枚举名）<para>Display name (defaults to the enum name).</para></param>
        /// <param name="error">校验失败原因<para>Failure reason.</para></param>
        /// <returns>成功返回 true<para>True on success.</para></returns>
        public static bool CreateLanguageTemplate(SystemLanguage systemLanguage, string displayName, out string error)
        {
            error = null;
            EnsureLoaded();
            string code = systemLanguage.ToString();
            displayName = displayName?.Trim();

            // 与内置语言语义重复的标识不允许再建（English 与简中已有完整内置数据）
            if (IsBuiltinCoveredLanguage(systemLanguage))
            {
                error = Tr($"'{code}' 已拥有内置语言表，请直接使用内置语言。",
                    $"'{code}' already has a builtin language table; use it directly.");
                return false;
            }
            if (FindLanguage(code) != null)
            {
                error = Tr($"语言 '{code}' 已存在。", $"Language '{code}' already exists.");
                return false;
            }
            string path = GetExternalLanguagePath(code);
            if (File.Exists(path))
            {
                error = Tr($"文件已存在：{path}", $"File already exists: {path}");
                return false;
            }

            try
            {
                // 键源 = 全部包键清单的整合（扩展包词条一并导出，非法键已在整合时剔除）
                var keySource = CollectPackageKeyLists(new List<string>());
                if (keySource.Count == 0)
                {
                    error = Tr("没有找到任何包的键清单文件，无法生成语言模板。",
                        "No package key list file was found; cannot create the language template.");
                    return false;
                }

                var template = new LanguageFileModel
                {
                    code = code,
                    displayName = string.IsNullOrEmpty(displayName) ? code : displayName,
                    systemLanguage = code, // 标识即系统语言，Auto 推导天然生效
                    entries = new List<EntryModel>(),
                };
                foreach (var package in keySource)
                {
                    foreach (var item in package.Items)
                    {
                        template.entries.Add(new EntryModel
                        {
                            key = item.name,
                            desc = item.desc,
                            text = null, // 待翻译，运行时回落 English
                            ParamHints = item.ParamHints,
                        });
                    }
                }
                Directory.CreateDirectory(GetExternalLanguageDirectory());
                WriteLanguageFile(path, template);
                return true;
            }
            catch (Exception ex)
            {
                error = Tr("生成模板失败：", "Failed to create template: ") + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 读取文本文件（文件缺失或读取失败返回 null 并告警）
        /// <para>Read a text file; null with a warning when missing or unreadable.</para>
        /// </summary>
        /// <param name="path">文件路径<para>File path.</para></param>
        /// <returns>文件内容或 null<para>Content or null.</para></returns>
        private static string ReadFileSafe(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                D.Warning(Tr($"读取文件 '{path}' 失败：{ex.Message}", $"Failed to read '{path}': {ex.Message}"));
                return null;
            }
        }

        /// <summary>
        /// 获取工程扩展语言目录
        /// <para>Get the project external language directory.</para>
        /// </summary>
        /// <returns>{projectRoot}/EFTools/EditorLanguage<para>The directory path.</para></returns>
        private static string GetExternalLanguageDirectory()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            return projectRoot == null
                ? ExternalLanguageDirName
                : Path.Combine(projectRoot.FullName, ExternalLanguageDirName);
        }

        /// <summary>
        /// 编辑器菜单：整合全部包键清单（补齐外部语言 + 刷新运行时缓存）
        /// <para>Editor menu: merge all package key lists (fill external languages + reload runtime cache).</para>
        /// </summary>
        [MenuItem(MenuItemToolkit.Utility + "✍️ Update Edit Language", false, MenuItemToolkit.UtilityPriority + 3)]
        private static void UpdateLanguageConfig()
        {
            EditorUtility.DisplayDialog(SyncAndRegenerate(out string report) ? Combine("Done") : Combine("Error"),
                report, Combine("Confirm"));
        }

        /// <summary>
        /// 整合全部包键清单：补齐 EFTools 外部语言词条并刷新运行时缓存（键清单即唯一数据源，无枚举生成）
        /// <para>Merge every package's key list: fill external language entries and reload the
        /// runtime cache (the key list is the single data source; no enum generation).</para>
        /// </summary>
        /// <remarks>
        /// 键清单由各包手工维护且直接作为运行时数据，本菜单不再同步任何包内文件，只做两件事：
        /// EFTools 外部语言以键清单整合全集为源补 text=null 词条；最后刷新运行时缓存
        /// （改键清单后不重跑菜单则运行时仍用旧数据）。
        /// <para>Key lists are hand-maintained and directly serve as runtime data; this menu no
        /// longer syncs any in-package files. It fills external language files from the merged
        /// key set and reloads the runtime cache (stale runtime data until the menu runs).</para>
        /// </remarks>
        /// <param name="report">整合统计报告（已按当前语言本地化）<para>Localized merge report.</para></param>
        /// <returns>成功返回 true<para>True on success.</para></returns>
        public static bool SyncAndRegenerate(out string report)
        {
            EnsureLoaded();
            var warnings = new List<string>();

            // 1) 整合全部包的键清单（core 与扩展包同一入口）
            var packageLists = CollectPackageKeyLists(warnings);
            if (packageLists.Count == 0)
            {
                report = Tr("没有找到任何包的键清单文件。", "No package key list file was found.");
                return false;
            }

            var builder = new StringBuilder();
            builder.AppendLine(Tr("整合完成。", "Merge completed."));
            foreach (var package in packageLists)
                builder.AppendLine($"{package.PackageDir}: {package.Items.Count}");

            // 2) EFTools 外部语言：以键清单整合全集为源补 text=null（覆盖所有包词条）
            var mergedEntries = new List<EntryModel>();
            foreach (var package in packageLists)
            {
                foreach (var item in package.Items)
                {
                    mergedEntries.Add(new EntryModel { key = item.name, desc = item.desc, text = null, ParamHints = item.ParamHints });
                }
            }
            var externalLines = new List<string>();
            int externalAdded = SyncExternalFiles(mergedEntries, externalLines, out string externalError);
            if (externalError != null)
            {
                report = externalError;
                return false;
            }

            // 3) 刷新运行中缓存（键清单改动在重跑菜单后生效）
            ReloadLanguages();

            foreach (string warning in warnings)
                builder.AppendLine(Tr("[ 警告 ] ", "[ Warning ] ") + warning);
            foreach (string line in externalLines)
                builder.AppendLine(line);
            if (externalLines.Count > 0)
                builder.AppendLine(Tr($"外部语言共 +{externalAdded}。", $"External languages +{externalAdded} in total."));
            report = builder.ToString();
            return true;
        }

        /// <summary>
        /// 读取语言文件模型（缺失 / 格式无效返回 null 并给出错误）
        /// <para>Read a language file model; null with an error when missing or invalid.</para>
        /// </summary>
        /// <param name="path">文件路径<para>File path.</para></param>
        /// <param name="error">失败原因<para>Failure reason.</para></param>
        /// <returns>语言文件模型<para>The model.</para></returns>
        private static LanguageFileModel ReadLanguageFile(string path, out string error)
        {
            error = null;
            if (!File.Exists(path))
            {
                error = Tr($"语言文件缺失：\n{path}", $"Language file is missing:\n{path}");
                return null;
            }
            try
            {
                var model = JsonConvert.DeserializeObject<LanguageFileModel>(File.ReadAllText(path));
                if (model?.entries == null)
                {
                    error = Tr($"{Path.GetFileName(path)} 格式无效（entries 缺失）。", $"Invalid {Path.GetFileName(path)} (entries missing).");
                    return null;
                }
                return model;
            }
            catch (Exception ex)
            {
                error = Tr($"解析 {Path.GetFileName(path)} 失败：{ex.Message}", $"Failed to parse {Path.GetFileName(path)}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取工程 Packages 目录绝对路径（基于 Application.dataPath 推导，规避 Editor 工作目录漂移）
        /// <para>Get the absolute path of the project's Packages folder (derived from
        /// Application.dataPath to avoid Editor working-directory drift).</para>
        /// </summary>
        /// <returns>{projectRoot}/Packages<para>The packages root path.</para></returns>
        private static string GetPackagesRoot()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            return projectRoot == null
                ? PackagesRoot
                : Path.Combine(projectRoot.FullName, PackagesRoot);
        }

        /// <summary>
        /// 扫描并整合全部包的双语键清单（Packages/*/Editor Resources/Description/Editorlanguages.json）
        /// <para>Collect and merge every package's bilingual key list
        /// (Packages/*/Editor Resources/Description/Editorlanguages.json).</para>
        /// </summary>
        /// <param name="warnings">整合过程中的告警（坏文件 / 非法键 / 跨包同名键）<para>Warnings (broken files, invalid keys, cross-package duplicates).</para></param>
        /// <returns>按包分组的键清单（包路径字母序）<para>Key lists grouped by package (ordered by package path).</para></returns>
        /// <remarks>
        /// 各扩展包把键清单放于约定目录即完成接入；跨包同名键保留先扫描（字母序靠前）的包并告警。
        /// <para>Packages opt in by shipping the key list in the conventional folder; cross-package
        /// duplicate keys keep the first-scanned (ordinal-first) package with a warning.</para>
        /// </remarks>
        private static List<PackageKeyList> CollectPackageKeyLists(List<string> warnings)
        {
            var results = new List<PackageKeyList>();
            var seenKeys = new Dictionary<string, LegacyItemModel>(StringComparer.Ordinal);   // 键 -> 首个条目（跨包去重/内容对比）
            string packagesRoot = GetPackagesRoot();
            if (!Directory.Exists(packagesRoot))
            {
                warnings.Add(Tr($"Packages 目录不存在：{packagesRoot}", $"Packages directory not found: {packagesRoot}"));
                return results;
            }

            var packageDirs = new List<string>(Directory.GetDirectories(packagesRoot));
            packageDirs.Sort((a, b) => string.Compare(a, b, StringComparison.Ordinal));
            int keyListCount = 0;
            foreach (string packageDir in packageDirs)
            {
                string listPath = Path.Combine(packageDir, KeyListDir, KeyListFileName);
                if (!File.Exists(listPath))
                    continue;

                LegacyLanguageModel model;
                try
                {
                    model = JsonConvert.DeserializeObject<LegacyLanguageModel>(ReadFileSafe(listPath));
                }
                catch (Exception ex)
                {
                    warnings.Add(Tr($"解析键清单 '{listPath}' 失败：{ex.Message}",
                        $"Failed to parse key list '{listPath}': {ex.Message}"));
                    continue;
                }
                if (model?.LcList == null || model.LcList.Count == 0)
                    continue;

                // 逐条校验作用域键并检测跨包同名键（非法键剔除，保证同步产物键法正确）
                var validItems = new List<LegacyItemModel>();
                foreach (var item in model.LcList)
                {
                    if (item == null || string.IsNullOrEmpty(item.name))
                        continue;
                    if (!ScopedKeyPattern.IsMatch(item.name))
                    {
                        warnings.Add(Tr($"键清单 '{listPath}' 含非法键 '{item.name}'，已跳过。",
                            $"Key list '{listPath}' contains invalid key '{item.name}', skipped."));
                        continue;
                    }
                    if (seenKeys.TryGetValue(item.name, out var firstItem))
                    {
                        // 公共词按归属复制到多个包属预期行为：内容一致时静默去重，仅内容冲突才告警
                        if (!HasSameContent(firstItem, item))
                            warnings.Add(Tr($"键 '{item.name}' 在多个包的键清单中内容不同，保留先扫描包的条目（当前：{packageDir}）。",
                                $"Key '{item.name}' differs between packages; the first-scanned entry wins (current: {packageDir})."));
                        continue;
                    }
                    seenKeys.Add(item.name, item);
                    validItems.Add(item);
                }
                if (validItems.Count > 0)
                {
                    keyListCount++;
                    results.Add(new PackageKeyList { PackageDir = packageDir, Items = validItems });
                }
            }
            if (keyListCount == 0)
                warnings.Add(Tr($"没有在任何包中找到键清单（{KeyListFileName}）——扫描根：{packagesRoot}。",
                    $"No key list ({KeyListFileName}) found in any package — scan root: {packagesRoot}."));
            return results;
        }

        /// <summary>
        /// 比较两个键清单条目的内容是否一致（描述 + 双语文本 + 占位符说明；用于跨包公共词静默去重）
        /// <para>Whether two key-list entries carry identical content (description, bilingual texts
        /// and placeholder hints); used to silently dedupe shared vocabulary across packages.</para>
        /// </summary>
        /// <param name="a">条目 A<para>Entry A.</para></param>
        /// <param name="b">条目 B<para>Entry B.</para></param>
        /// <returns>内容一致返回 true<para>True when contents match.</para></returns>
        private static bool HasSameContent(LegacyItemModel a, LegacyItemModel b)
        {
            if (a == null || b == null)
                return false;
            if (!string.Equals(a.desc, b.desc, StringComparison.Ordinal))
                return false;
            if (!HasSameStringList(a.array, b.array))
                return false;
            return HasSameStringList(a.ParamHints, b.ParamHints);
        }

        /// <summary>
        /// 比较两个字符串列表是否逐项一致（顺序敏感；null 与空列表视为等价）
        /// <para>Whether two string lists are item-wise identical (order-sensitive; null and empty
        /// are treated as equivalent).</para>
        /// </summary>
        /// <param name="a">列表 A<para>List A.</para></param>
        /// <param name="b">列表 B<para>List B.</para></param>
        /// <returns>一致返回 true<para>True when identical.</para></returns>
        private static bool HasSameStringList(List<string> a, List<string> b)
        {
            int countA = a == null ? 0 : a.Count;
            int countB = b == null ? 0 : b.Count;
            if (countA != countB)
                return false;
            for (int i = 0; i < countA; i++)
            {
                if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }


        /// <summary>
        /// 以键清单整合全集为源，将缺失键以 text=null 追加到全部 EFTools 外部语言文件（不碰已有条目）
        /// <para>Append missing keys as text=null to every external language file, keyed by the
        /// merged key-list set; existing entries untouched.</para>
        /// </summary>
        /// <param name="keySource">键清单整合全集（键真相）<para>The merged key-list set (key truth).</para></param>
        /// <param name="lines">逐文件统计行（用于报告）<para>Per-file stat lines for the report.</para></param>
        /// <param name="error">失败原因（解析失败仅跳过并记录，不中断）<para>Failure reason (parse failures are logged and skipped).</para></param>
        /// <returns>追加的键总数<para>Total appended keys.</para></returns>
        private static int SyncExternalFiles(List<EntryModel> keySource, List<string> lines, out string error)
        {
            error = null;
            int totalAdded = 0;
            string directory = GetExternalLanguageDirectory();
            if (!Directory.Exists(directory))
                return 0;

            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                string fileName = Path.GetFileName(file);
                var model = ReadLanguageFile(file, out var readError);
                if (model == null)
                {
                    lines.Add($"{fileName}: {readError}");
                    continue;
                }

                var existing = CollectEntryKeys(model.entries);
                int added = 0;
                foreach (var source in keySource)
                {
                    if (source == null || string.IsNullOrEmpty(source.key) || existing.Contains(source.key))
                        continue;
                    model.entries.Add(new EntryModel { key = source.key, desc = source.desc, text = null, ParamHints = source.ParamHints });
                    existing.Add(source.key);
                    added++;
                }
                if (added > 0)
                {
                    WriteLanguageFile(file, model);
                    totalAdded += added;
                    lines.Add($"{fileName} +{added}");
                }
            }
            return totalAdded;
        }

        /// <summary>
        /// 收集词条文件中已存在的键（忽略大小写）
        /// <para>Collect existing entry keys (case-insensitive).</para>
        /// </summary>
        /// <param name="entries">词条集合<para>Entries.</para></param>
        /// <returns>键集合<para>Key set.</para></returns>
        private static HashSet<string> CollectEntryKeys(List<EntryModel> entries)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry?.key))
                    keys.Add(entry.key);
            }
            return keys;
        }

        /// <summary>
        /// 序列化语言文件（缩进 2、UTF-8 无 BOM）
        /// <para>Serialize a language file (indent 2, UTF-8 without BOM).</para>
        /// </summary>
        /// <param name="path">目标路径<para>Target path.</para></param>
        /// <param name="model">语言文件模型<para>Language file model.</para></param>
        private static void WriteLanguageFile(string path, LanguageFileModel model)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(model, Formatting.Indented), new UTF8Encoding(false));
        }

        #endregion
    }

    /// <summary>
    /// 词条检视信息（管理窗口展示用：来源包 + 键 + 描述 + 中英文本）
    /// <para>Entry info for the management window: source package, key, description and zh/en texts.</para>
    /// </summary>
    public sealed class LanguageEntryInfo
    {
        /// <summary>来源包名<para>Source package name.</para></summary>
        public string PackageName;
        /// <summary>词条键（公共词枚举名或作用域键）<para>Entry key (shared enum name or scoped key).</para></summary>
        public string Key;
        /// <summary>描述（翻译参考）<para>Description (translation hint).</para></summary>
        public string Desc;
        /// <summary>中文文本<para>Chinese text.</para></summary>
        public string Chinese;
        /// <summary>英文文本<para>English text.</para></summary>
        public string English;
    }

    /// <summary>
    /// 编辑器语言选项（下拉框条目）
    /// <para>Editor language option (dropdown item).</para>
    /// </summary>
    public readonly struct LanguageOption
    {
        /// <summary>语言 code<para>Language code.</para></summary>
        public string Code { get; }
        /// <summary>下拉显示名<para>Dropdown display name.</para></summary>
        public string DisplayName { get; }

        /// <summary>
        /// 构造语言选项
        /// <para>Create a language option.</para>
        /// </summary>
        /// <param name="code">语言 code<para>Language code.</para></param>
        /// <param name="displayName">显示名<para>Display name.</para></param>
        public LanguageOption(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }
    }
}
