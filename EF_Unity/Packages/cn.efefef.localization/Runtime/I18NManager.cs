/*
 * ================================================
 * Describe:        运行时多语言服务：管理当前语言，经 AssetsManager 按需加载语言表
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 16:40:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 18:25:00
 * ScriptVersion:   0.6
 * ================================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using EasyFramework.Managers.Assets;
using EasyFramework.Managers.Event;
using UnityEngine;

namespace EasyFramework.Localization
{
    /// <summary>
    /// 语言代码常量（实际可用语言由语言表资源决定，本类只登记兜底语言与工程既定代码）
    /// <para>Language code constants; available languages are decided by the table assets.</para>
    /// </summary>
    public static class LanguageCode
    {
        #region Fields

        /// <summary>
        /// 英语（兜底语言）
        /// <para>English (fallback language).</para>
        /// </summary>
        public const string En = "en";

        /// <summary>
        /// 中文（与语言表资源 l10n_tbtext_zh.json 一一对应）
        /// <para>Chinese; matches the table asset l10n_tbtext_zh.json.</para>
        /// </summary>
        public const string Zh = "zh";

        #endregion
    }

    /// <summary>
    /// 语言切换事件（只在语言真正发生变化时发布，避免重复设置引发无谓刷新）
    /// <para>Language changed event; published only when the language actually changes.</para>
    /// </summary>
    public readonly struct LanguageChangedEvent
    {
        #region Properties

        /// <summary>
        /// 切换后的语言代码
        /// <para>Language code after the switch.</para>
        /// </summary>
        public string Language { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 构造语言切换事件
        /// <para>Constructs a language changed event.</para>
        /// </summary>
        /// <param name="language">切换后的语言代码<para>Language code after the switch.</para></param>
        public LanguageChangedEvent(string language)
        {
            Language = language;
        }

        #endregion
    }

    /// <summary>
    /// 多语言服务：语言表经 AssetsManager 按需加载，内存中任何时刻只有当前语言一张表
    /// <para>Runtime i18n service. Language tables are loaded on demand via AssetsManager
    /// (one table per switch, YooAsset hot-update friendly).</para>
    /// </summary>
    [Manager(Order = 99700)]
    [Dependency(typeof(EventManager))]
    public sealed class I18NManager : MonoSingleton<I18NManager>, ISingleton
    {
        #region Fields

        // 兜底语言代码：目标语言加载失败时回落这里
        private const string FallbackLanguage = LanguageCode.En;

        // PlayerPrefs 中保存玩家语言选择的键（带包名前缀避免撞名）
        private const string PrefsLanguageKey = "EF.Localization.Language";

        // 当前语言代码，未初始化时为空串（装载完成前先行赋值，供并发防串比对）
        private string _currentLanguage = string.Empty;

        // 当前语言的译文表（仅当前语言驻留内存；切换时整体替换，旧表交给 GC）
        private Dictionary<string, string> _table;

        // 装载请求代数（每次发起装载递增；异步完成后比对，不一致说明结果已过期）
        private int _loadGeneration;

        /// <summary>
        /// 查询失败时返回的占位文本前缀（界面出现 [Missing:xxx] 一眼定位缺词）
        /// <para>Prefix of the placeholder returned on lookup failure.</para>
        /// </summary>
        public const string MissingPrefix = "[Missing:";

        /// <summary>
        /// 语言表资源路径模板（{0} = 语言代码）；须与 AssetsManager 的收集配置一致，项目不同时在启动引导中覆写
        /// <para>Asset path template of language tables ({0} = language code).
        /// Override in bootstrapping when the project uses a different path.</para>
        /// </summary>
        public static string TableAssetPathFormat = "l10n_tbtext_{0}";

        #endregion

        #region Properties

        // 是否把玩家选择持久化（内部开关，始终为 true；按场景强制语言请直接调用 SwitchLanguage）
        private bool AutoPersist { get; set; } = true;

        /// <summary>
        /// 当前语言的译文表是否已在内存（未装载时界面组件保持美术填写的原文）
        /// <para>Whether the current language table is in memory.</para>
        /// </summary>
        public bool IsLoaded => _table != null;

        /// <summary>
        /// 当前语言代码，未初始化时为空串
        /// <para>Current language code; empty when not initialized.</para>
        /// </summary>
        public string CurrentLanguage => _currentLanguage;

        #endregion

        #region Methods

        // 静默装载（Init 自动路径专用）：失败不打任何日志，未接入多语言的工程零噪音
        private async UniTaskVoid TryLoadSilently(string language)
        {
            // 抢占代数：与手动装载互斥，慢速的旧请求不覆盖新状态
            _loadGeneration++;
            var generation = _loadGeneration;

            await TryApplyAsync(generation, language);
        }

        // 应用指定语言的表（装载管线的终点：成功则替换内存表并广播事件，失败不动任何状态）
        private async UniTask<bool> TryApplyAsync(int generation, string language)
        {
            var table = await LoadTableAsync(language);
            if (generation != _loadGeneration) return false; // 已被更新的请求抢占
            if (table == null || table.Count == 0) return false;

            _table = table;
            _currentLanguage = language;
            D.Log($"[ Localization ] 语言表装载完成：{language}（{table.Count} 条）");
            NotifyLanguageChanged(language);
            return true;
        }

        // 经 AssetsManager 加载并解析指定语言的表（资源缺失返回 null；格式无关：json / Luban bytes 等由解析层按内容分发）
        private async UniTask<Dictionary<string, string>> LoadTableAsync(string language)
        {
            var asset = await AssetsManager.Instance.LoadAsync<TextAsset>(
                string.Format(TableAssetPathFormat, language));

            return asset != null ? L10nTableParser.Parse(asset.bytes, asset.name) : null;
        }

        // 确定初始语言：玩家偏好优先，否则兜底语言（是否真的可用由装载阶段校验）
        private string ResolveInitialLanguage()
        {
            if (!AutoPersist) return FallbackLanguage;

            var saved = PlayerPrefs.GetString(PrefsLanguageKey, string.Empty);
            return string.IsNullOrEmpty(saved) ? FallbackLanguage : saved;
        }

        // 保存玩家选择的语言（立刻落盘：进程被系统杀掉时这次设置不丢）
        private static void SavePersistedLanguage(string language)
        {
            if (string.IsNullOrEmpty(language)) return;
            PlayerPrefs.SetString(PrefsLanguageKey, language);
            // 立刻落盘：不落盘的话，进程被系统杀掉（安卓后台回收）时这次设置会丢
            PlayerPrefs.Save();
        }

        // 构造缺词占位文本
        private static string BuildMissing(string key) => $"{MissingPrefix}{key}]";

        // 广播语言切换事件
        private static void NotifyLanguageChanged(string language)
        {
            if (EventManager.Instance != null)
                EventManager.Instance.Publish(new LanguageChangedEvent(language));
        }

        /// <summary>
        /// 由 EFC 驱动调用（排序在 EventManager 之后）：确定初始语言（玩家偏好优先，否则兜底 en）后异步装载
        /// <para>Driven by EFC (ordered after EventManager): resolves the initial language
        /// (player preference, else en) and loads it asynchronously.</para>
        /// <para><b>静默语义：</b>语言表加载失败（工程未接入多语言 / bundle 中无语言表资源）时
        /// 不输出任何日志——未接入的工程零噪音；装载完成前启用的界面保持美术原文，
        /// 完成后广播事件自动刷新。需要感知失败的手动装载请用 <see cref="LoadLanguageAsync"/>。</para>
        /// </summary>
        public void Init()
        {
            // 已初始化过（如使用者提前手动指定过语言）不重复走默认流程
            if (!string.IsNullOrEmpty(_currentLanguage)) return;

            _currentLanguage = ResolveInitialLanguage();
            TryLoadSilently(_currentLanguage).Forget();
        }

        /// <summary>
        /// 由 EFC 驱动调用；复位当前语言与译文表
        /// <para>Driven by EFC; resets language and table.</para>
        /// </summary>
        public void Quit()
        {
            _table = null;
            _currentLanguage = string.Empty;
        }

        /// <summary>
        /// 按 key 取译文（未命中返回 [Missing:key] 便于定位；未装载时同样返回占位符，可先判 <see cref="IsLoaded"/>）
        /// <para>Looks up a translation by key; missing entries become a visible placeholder.</para>
        /// </summary>
        /// <param name="key">本地化 key<para>Localization key.</para></param>
        /// <returns>译文；未命中时返回 [Missing:{key}]<para>The translation, or [Missing:{key}] on a miss.</para></returns>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_table == null) return BuildMissing(key);

            return _table.TryGetValue(key, out var value) ? value : BuildMissing(key);
        }

        /// <summary>
        /// 按 key 取译文并格式化占位符（Dynamic 文案专用入口；占位符与参数不匹配时返回原文并告警，不抛异常）
        /// <para>Fetches a translation and formats its placeholders. Static texts
        /// tolerate extra args; format failures return the raw value with a warning.</para>
        /// </summary>
        /// <param name="key">本地化 key<para>Localization key.</para></param>
        /// <param name="args">占位符参数，按序对应 {0} {1}…<para>Placeholder args, matched in order.</para></param>
        /// <returns>格式化后的译文；格式化失败时返回未格式化原文<para>Formatted text; the raw value when formatting fails.</para></returns>
        public string GetFormat(string key, params object[] args)
        {
            var raw = Get(key);

            // 无参调用（或 Static 误传空参）：直接返回原文，避免 string.Format 空转
            if (args == null || args.Length == 0) return raw;

            try
            {
                return string.Format(raw, args);
            }
            catch (FormatException e)
            {
                D.Warning($"[ Localization ] 文案格式化失败（key：{key}，参数数：{args.Length}），已返回原文。\n{e.Message}");
                return raw;
            }
        }

        /// <summary>
        /// 切换语言：按需加载目标语言的表，成功则整体替换旧表、广播事件并持久化；失败保持原语言
        /// <para>Switches language on demand; keeps the old language on failure.</para>
        /// </summary>
        /// <param name="language">目标语言代码（须存在对应语言表资源）<para>Target language code.</para></param>
        /// <returns>切换是否成功（语言为空 / 与当前相同且已装载 / 加载失败时为 false）<para>Whether the switch succeeded.</para></returns>
        public async UniTask<bool> SwitchLanguage(string language)
        {
            if (string.IsNullOrEmpty(language)) return false;
            if (language == _currentLanguage && _table != null) return false;

            // 抢占代数：使在途的旧装载请求结果作废
            _loadGeneration++;
            var generation = _loadGeneration;

            var previous = _currentLanguage;
            _currentLanguage = language;

            // 释放旧语言资源（引用计数减一；解析后的表由新表整体替换，旧字典交给 GC）
            if (_table != null && !string.IsNullOrEmpty(previous) && previous != language)
            {
                await AssetsManager.Instance.Release(string.Format(TableAssetPathFormat, previous));
            }

            if (!await TryApplyAsync(generation, language))
            {
                // 加载失败：语言标记回退，旧表仍在内存则继续生效
                _currentLanguage = previous;
                return false;
            }

            if (AutoPersist) SavePersistedLanguage(language);
            return true;
        }

        /// <summary>
        /// 装载指定语言的表并应用（供启动引导与重试使用；目标语言失败时自动回落兜底 en）
        /// <para>Loads and applies a language table; falls back to en on failure.</para>
        /// </summary>
        /// <param name="language">语言代码<para>Language code.</para></param>
        /// <returns>是否成功装载并应用了某个语言的表<para>Whether a language table was loaded and applied.</para></returns>
        public async UniTask<bool> LoadLanguageAsync(string language)
        {
            if (string.IsNullOrEmpty(language)) return false;

            // 抢占代数
            _loadGeneration++;
            var generation = _loadGeneration;

            if (!await TryApplyAsync(generation, language))
            {
                if (language == FallbackLanguage)
                {
                    D.Error($"[ Localization ] 兜底语言 {FallbackLanguage} 的语言表缺失，本地化不可用。");
                    return false;
                }

                D.Warning($"[ Localization ] 语言 {language} 加载失败，已回落 {FallbackLanguage}。");
                if (!await TryApplyAsync(generation, FallbackLanguage)) return false;
            }

            return true;
        }

        #endregion
    }
}