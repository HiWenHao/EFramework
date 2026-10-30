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
    /// 编辑器多语言辅助类
    /// <para>Editor localization helper. One language one JSON file: builtin zh/en live in
    /// "Editor Resources/Language" inside the package, extensible languages live in
    /// "{projectRoot}/EFTools/EditorLanguage/{code}.json". Supports word combining and
    /// composite sentences with "{0}" placeholders.</para>
    /// </summary>
    public static class LC
    {
        #region Fields

        private const string EnumOutputFile = "EF.LanguageEnum.cs";                        // 枚举输出文件名
        private const string EnumOutputDir = "Editor/Config/Language";                      // 枚举输出目录（包内相对路径）
        private const string BuiltinLanguageDir = "Editor Resources/Language";              // 内置语言数据目录（包内相对路径）
        private const string ExternalLanguageDirName = "EFTools/EditorLanguage";            // 工程扩展语言目录（相对工程根）
        private const string LegacyListPath = "Editor Resources/Description/Editorlanguages.json";      // 旧版双语键清单（维护入口）

        private const string AutoCode = "Auto";              // 自动跟随系统语言的选择值

        private static readonly Regex IdentifierPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled); // 语言键词法校验（枚举生成前键尚不存在，不能依赖 TryParse）
        private static readonly object _lock = new object();   // 加载线程锁

        private static List<LanguageRuntime> _languages;       // 语言注册表：内置 zh/en 在前 + 外部按 code 排序
        private static bool _loaded;                            // 注册表是否已加载

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

        /// <summary>加载后的语言运行时数据<para>Loaded runtime language data.</para></summary>
        private sealed class LanguageRuntime
        {
            // 语言 code（持久化与查询标识）
            public string Code;
            // 下拉显示名
            public string DisplayName;
            // 可选 SystemLanguage 枚举名
            public string SystemLanguage;
            // 键 -> 文本（未翻译条目不入典）
            public readonly Dictionary<Lc, string> Texts = new Dictionary<Lc, string>();
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
                    D.Warning("[ LC ] ProjectConfig is not ready; the language choice is not saved.");
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
                    D.Warning($"[ LC ] Unknown language code '{next}', fallback to Auto.");
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
            if (_loaded)
                return;
            lock (_lock)
            {
                if (_loaded)
                    return;
                _languages = new List<LanguageRuntime>();
                LoadBuiltin(nameof(SystemLanguage.Chinese), false);
                LoadBuiltin(nameof(SystemLanguage.English), true);
                LoadExternal();
                _loaded = true;
            }
        }

        /// <summary>
        /// 加载包内内置语言文件
        /// <para>Load a builtin language file inside the package.</para>
        /// </summary>
        /// <param name="code">语言 code<para>Language code.</para></param>
        /// <param name="required">是否必需（缺失时报错）<para>Whether a missing file is an error.</para></param>
        private static void LoadBuiltin(string code, bool required)
        {
            string path = Path.Combine(Utility.Path.GetEfPath(), BuiltinLanguageDir, code + ".json");
            if (!File.Exists(path))
            {
                if (required)
                    D.Error($"[ LC ] Builtin language file is missing: {path}");
                return;
            }
            var runtime = ParseLanguageFile(ReadFileSafe(path), code);
            if (runtime != null)
                _languages.Add(runtime);
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
                    D.Warning($"[ LC ] Duplicate language code '{runtime.Code}' skipped (file: {file})");
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
                    D.Warning($"[ LC ] Invalid language file (entries missing), skipped: {fallbackCode}");
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
                    if (!Enum.TryParse(entry.key, true, out Lc key))
                    {
                        skipped++;
                        continue;
                    }
                    runtime.Texts[key] = entry.text;
                }
                if (skipped > 0)
                    D.Warning($"[ LC ] {skipped} unknown keys skipped in language '{runtime.Code}'");
                return runtime;
            }
            catch (Exception ex)
            {
                D.Warning($"[ LC ] Failed to parse language file '{fallbackCode}': {ex.Message}");
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
        /// 查询链：当前语言 -> 回落 English
        /// <para>Query chain: current language -> English fallback.</para>
        /// </summary>
        /// <param name="key">语言键<para>Language key.</para></param>
        /// <param name="text">命中文本<para>Matched text.</para></param>
        /// <returns>命中返回 true<para>True when found.</para></returns>
        private static bool TryGetText(Lc key, out string text)
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
        /// 获取单个键的本地化文本
        /// <para>Get localized text for a single key.</para>
        /// </summary>
        /// <param name="key">语言键<para>Language key.</para></param>
        /// <returns>对应文本；缺失时返回 "[键名]" 便于排查<para>The text; "[key]" on missing.</para></returns>
        public static string Combine(Lc key)
        {
            return TryGetText(key, out var text) ? text : $"[{key}]";
        }

        /// <summary>
        /// 使用当前语言的分隔符（Lc.S）拼接多个本地化文本
        /// <para>Combine multiple localized texts with the current separator (Lc.S).</para>
        /// </summary>
        /// <param name="keys">要拼接的语言键数组<para>Keys to combine.</para></param>
        /// <returns>拼接后的字符串；空数组返回空串<para>Combined string; empty for no keys.</para></returns>
        public static string Combine(params Lc[] keys)
        {
            if (keys == null || keys.Length == 0)
                return string.Empty;
            if (keys.Length == 1)
                return Combine(keys[0]);

            string separator = Combine(Lc.S);
            var builder = new StringBuilder();
            for (int i = 0; i < keys.Length; i++)
            {
                builder.Append(Combine(keys[i]));
                if (i < keys.Length - 1)
                    builder.Append(separator);
            }
            return builder.ToString();
        }

        /// <summary>
        /// 复合语句：取当前语言模板并填充 "{0}" 占位符
        /// <para>Composite sentence: format the current language template with "{0}" placeholders.</para>
        /// </summary>
        /// <param name="key">模板键<para>Template key.</para></param>
        /// <param name="args">占位符参数<para>Placeholder args.</para></param>
        /// <returns>填充后的句子；占位符与参数不匹配时降级返回模板原文并告警<para>The formatted sentence; degrades to the raw template with a warning on mismatch.</para></returns>
        public static string Format(Lc key, params object[] args)
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
                D.Warning(IsEnglish
                    ? $"[ LC ] Format failed (placeholders do not match args), returning template: {template}"
                    : $"[ LC ] 格式化失败（占位符与参数不匹配），返回模板原文：{template}");
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
                string englishSourcePath = Path.Combine(Utility.Path.GetEfPath(), BuiltinLanguageDir, nameof(SystemLanguage.English) + ".json");
                var source = ReadLanguageFile(englishSourcePath, out var readError);
                if (source == null)
                {
                    error = readError;
                    return false;
                }

                var template = new LanguageFileModel
                {
                    code = code,
                    displayName = string.IsNullOrEmpty(displayName) ? code : displayName,
                    systemLanguage = code, // 标识即系统语言，Auto 推导天然生效
                    entries = new List<EntryModel>(),
                };
                foreach (var entry in source.entries)
                {
                    template.entries.Add(new EntryModel
                    {
                        key = entry.key,
                        desc = entry.desc,
                        text = null, // 待翻译，运行时回落 English
                        ParamHints = entry.ParamHints,
                    });
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
                D.Warning($"[ LC ] Failed to read '{path}': {ex.Message}");
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
        /// 编辑器菜单：从双语键清单同步全部语言文件并重新生成 Lc 枚举
        /// <para>Editor menu: sync all language files from the bilingual key list and regenerate the Lc enum.</para>
        /// </summary>
        [MenuItem(MenuItemToolkit.Utility + "✍️ Update Edit Language", false, MenuItemToolkit.UtilityPriority + 3)]
        private static void UpdateLanguageConfig()
        {
            if (SyncAndRegenerate(out string report))
                EditorUtility.DisplayDialog(Combine(Lc.Done), report, Combine(Lc.Confirm));
            else
                EditorUtility.DisplayDialog(Combine(Lc.Error), report, Combine(Lc.Confirm));
        }

        /// <summary>
        /// 从双语键清单（Editorlanguages.json）增量同步全部语言文件，并重新生成 Lc 枚举
        /// <para>Incrementally sync all language files from the bilingual key list
        /// (Editorlanguages.json), then regenerate the Lc enum.</para>
        /// </summary>
        /// <remarks>
        /// 同步规则：键清单是唯一键入口；en/zh 只追加缺失键（text 取 array 对位值），EFTools 外部语言只追加
        /// text=null 的待翻译键；**任何已存在条目一律不碰**（已填译文绝不覆盖，多余条目绝不删除）。
        /// <para>Sync rules: the key list is the single key entry; en/zh get missing keys appended
        /// (text from the paired array), external files get text=null placeholders; **existing entries
        /// are never touched** (translations never overwritten, extra entries never deleted).</para>
        /// </remarks>
        /// <param name="report">同步统计报告（已按当前语言本地化）<para>Localized sync report.</para></param>
        /// <returns>成功返回 true<para>True on success.</para></returns>
        public static bool SyncAndRegenerate(out string report)
        {
            EnsureLoaded();
            string legacyPath = Path.Combine(Utility.Path.GetEfPath(), LegacyListPath);
            if (!File.Exists(legacyPath))
            {
                report = Tr($"未找到键清单文件：\n{legacyPath}", $"Key list file not found:\n{legacyPath}");
                return false;
            }

            LegacyLanguageModel legacy;
            try
            {
                legacy = JsonConvert.DeserializeObject<LegacyLanguageModel>(ReadFileSafe(legacyPath));
            }
            catch (Exception ex)
            {
                D.Error(ex);
                report = Tr("解析键清单 JSON 失败，请检查文件格式。", "Failed to parse the key list JSON. Please check the file format.");
                return false;
            }
            if (legacy?.LcList == null || legacy.LcList.Count == 0)
            {
                report = Tr("键清单中没有包含任何条目。", "The key list contains no entries.");
                return false;
            }

            // 1) 内置 en：读一次全程复用（en 是键真相，枚举与外部同步都从它出发）
            string englishPath = Path.Combine(Utility.Path.GetEfPath(), BuiltinLanguageDir, nameof(SystemLanguage.English) + ".json");
            var englishModel = ReadLanguageFile(englishPath, out string error);
            if (englishModel == null)
            {
                report = error;
                return false;
            }
            int englishAdded = SyncEntriesInto(englishModel, legacy.LcList, 0, out int skipped);
            if (englishAdded > 0)
                WriteLanguageFile(englishPath, englishModel);

            // 2) 内置 zh 增量（array[1]=中文）
            int chineseAdded = SyncBuiltinFile(nameof(SystemLanguage.Chinese), 1, legacy.LcList, out _, out error);
            if (error != null)
            {
                report = error;
                return false;
            }

            // 3) EFTools 外部语言：以 English.json 全键为源补 text=null（保证与 English 键集一致）
            var externalLines = new List<string>();
            int externalAdded = SyncExternalFiles(englishModel.entries, externalLines, out error);
            if (error != null)
            {
                report = error;
                return false;
            }

            // 3) 从 English.json 重新生成 Lc 枚举
            if (!RegenerateEnum(out error))
            {
                report = error;
                return false;
            }

            // 4) 刷新运行中缓存
            ReloadLanguages();

            var builder = new StringBuilder();
            builder.AppendLine(Tr("同步完成。", "Sync completed."));
            builder.AppendLine(Tr($"en +{englishAdded}，zh +{chineseAdded}；非法键跳过 {skipped}。",
                $"en +{englishAdded}, zh +{chineseAdded}; {skipped} invalid keys skipped."));
            foreach (string line in externalLines)
                builder.AppendLine(line);
            if (externalLines.Count > 0)
                builder.AppendLine(Tr($"外部语言共 +{externalAdded}。", $"External languages +{externalAdded} in total."));
            builder.Append(Tr("Lc 枚举已重新生成。", "Lc enum regenerated."));
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
        /// 将键清单中缺失的键追加进语言模型（不碰已有条目）
        /// <para>Append keys missing from a language model; existing entries untouched.</para>
        /// </summary>
        /// <param name="model">语言文件模型<para>Language model.</para></param>
        /// <param name="items">键清单条目<para>Key list items.</para></param>
        /// <param name="textIndex">文本数组下标（0=English, 1=中文）<para>Text array index.</para></param>
        /// <param name="skipped">跳过的非法键数量<para>Count of invalid keys skipped.</para></param>
        /// <returns>追加的键数量<para>Number of appended keys.</para></returns>
        private static int SyncEntriesInto(LanguageFileModel model, List<LegacyItemModel> items, int textIndex, out int skipped)
        {
            skipped = 0;
            var existing = CollectEntryKeys(model.entries);
            int added = 0;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.name))
                    continue;
                if (!IdentifierPattern.IsMatch(item.name))
                {
                    skipped++;
                    continue;
                }
                if (existing.Contains(item.name))
                    continue;
                string text = item.array != null && item.array.Count > textIndex ? item.array[textIndex] : null;
                model.entries.Add(new EntryModel { key = item.name, desc = item.desc, text = text, ParamHints = item.ParamHints });
                existing.Add(item.name);
                added++;
            }
            return added;
        }

        /// <summary>
        /// 将键清单中缺失的键追加到内置语言文件（不碰已有条目）
        /// <para>Append keys missing from a builtin language file; existing entries untouched.</para>
        /// </summary>
        /// <param name="code">内置语言 code<para>Builtin language code.</para></param>
        /// <param name="textIndex">文本数组下标（0=English, 1=中文）<para>Text array index.</para></param>
        /// <param name="items">键清单条目<para>Key list items.</para></param>
        /// <param name="skipped">跳过的非法键数量<para>Count of invalid keys skipped.</para></param>
        /// <param name="error">失败原因<para>Failure reason.</para></param>
        /// <returns>追加的键数量<para>Number of appended keys.</para></returns>
        private static int SyncBuiltinFile(string code, int textIndex, List<LegacyItemModel> items, out int skipped, out string error)
        {
            string path = Path.Combine(Utility.Path.GetEfPath(), BuiltinLanguageDir, code + ".json");
            var model = ReadLanguageFile(path, out error);
            if (model == null)
            {
                skipped = 0;
                return 0;
            }
            int added = SyncEntriesInto(model, items, textIndex, out skipped);
            if (added > 0)
                WriteLanguageFile(path, model);
            return added;
        }

        /// <summary>
        /// 以 en 词条全集为源，将缺失键以 text=null 追加到全部 EFTools 外部语言文件（不碰已有条目）
        /// <para>Append missing keys as text=null to every external language file, keyed by the full
        /// en entry set; existing entries untouched.</para>
        /// </summary>
        /// <param name="keySource">English.json 词条全集（键真相）<para>The full English entry set (key truth).</para></param>
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

        /// <summary>
        /// 从 English.json 重新生成 Lc 枚举文件
        /// <para>Regenerate the Lc enum file from English.json.</para>
        /// </summary>
        /// <param name="error">失败原因<para>Failure reason.</para></param>
        /// <returns>成功返回 true<para>True on success.</para></returns>
        private static bool RegenerateEnum(out string error)
        {
            error = null;
            string englishPath = Path.Combine(Utility.Path.GetEfPath(), BuiltinLanguageDir, nameof(SystemLanguage.English) + ".json");
            LanguageFileModel source;
            try
            {
                source = JsonConvert.DeserializeObject<LanguageFileModel>(File.ReadAllText(englishPath));
            }
            catch (Exception ex)
            {
                error = Tr($"读取 English.json 失败：{ex.Message}", $"Failed to read English.json: {ex.Message}");
                return false;
            }
            if (source?.entries == null || source.entries.Count == 0)
            {
                error = Tr("语言 JSON 中没有包含任何条目。", "The language JSON contains no entries.");
                return false;
            }

            string enumContent = GenerateEnumContent(source.entries);
            string outputDir = Path.Combine(Utility.Path.GetEfPath(), EnumOutputDir);
            string outputPath = Path.Combine(outputDir, EnumOutputFile);
            try
            {
                Directory.CreateDirectory(outputDir);
                File.WriteAllText(outputPath, enumContent, new UTF8Encoding(false));
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                error = Tr("写入枚举文件失败:\n{0}", "Failed to write enum file:\n{0}", ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 根据词条集合生成 Lc 枚举的 C# 代码内容
        /// <para>Generate the Lc enum C# content from entries.</para>
        /// </summary>
        /// <param name="entries">词条集合（key + desc）<para>Entries (key + desc).</para></param>
        /// <returns>完整的枚举代码字符串<para>The full enum code string.</para></returns>
        private static string GenerateEnumContent(List<EntryModel> entries)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// 此文件由 LC.UpdateLanguageConfig() 自动生成");
            builder.AppendLine("// 请勿手动修改");
            builder.AppendLine();
            builder.AppendLine("namespace EasyFramework.Edit");
            builder.AppendLine("{");
            builder.AppendLine("\t/// <summary> 编辑器语言键 </summary>");
            builder.AppendLine("\tpublic enum Lc");
            builder.AppendLine("\t{");
            foreach (var entry in entries)
            {
                string desc = string.IsNullOrEmpty(entry.desc) ? entry.key : entry.desc;
                builder.AppendLine($"\t\t/// <summary> {desc} </summary>");
                builder.AppendLine($"\t\t{entry.key},");
            }
            builder.AppendLine("\t}");
            builder.AppendLine("}");
            return builder.ToString();
        }

        #endregion
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
