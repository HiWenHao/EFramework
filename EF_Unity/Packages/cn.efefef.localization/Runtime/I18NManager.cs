/*
 * ================================================
 * Describe:        运行时多语言服务：管理当前语言，经 AssetsManager 按需加载语言表
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 16:40:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 16:10:00
 * ScriptVersion:   0.6
 * ================================================
 */

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using EasyFramework.Managers.Assets;
using EasyFramework.Managers.Event;
using Newtonsoft.Json;
using UnityEngine;

namespace EasyFramework.Localization
{
    /// <summary>
    /// 语言代码常量
    /// <para>运行时的语言表经 AssetsManager 按需加载，支持哪些语言由 bundle 里的
    /// 语言表资源决定；本类只登记兜底语言（en）与工程既定的语言代码。</para>
    /// <para>Language code constants; const string by design (config-driven list).</para>
    /// </summary>
    public static class LanguageCode
    {
        /// <summary>英语（兜底语言） </summary>
        public const string En = "en";

        /// <summary>中文（zh：ISO 639-1 语言码；语言表资源 l10n_tbtext_zh.json 与之一一对应） </summary>
        public const string Zh = "zh";
    }

    /// <summary>
    /// 语言切换事件
    /// <para>由 <see cref="I18NManager"/> 在语言真正切换成功后发布，
    /// 界面层据此刷新所有 LocalizedText。只在「实际语言发生变化」时发布，
    /// 避免重复设置同一语言引发无谓刷新。</para>
    /// <para>Published by I18NManager after a successful language switch.</para>
    /// </summary>
    public readonly struct LanguageChangedEvent
    {
        /// <summary>切换后的语言代码 </summary>
        public string Language { get; }

        /// <summary>
        /// 构造语言切换事件
        /// </summary>
        /// <param name="language">切换后的语言代码</param>
        public LanguageChangedEvent(string language)
        {
            Language = language;
        }
    }

    /// <summary>
    /// 多语言服务
    /// <para>持有当前语言与其译文表。<strong>语言表经 AssetsManager 按需加载</strong>：
    /// 初始化与每次切换都只加载目标语言一张表（Yoo 热更友好，译文改动无需发版），
    /// 旧表整体替换、交由 GC 回收——内存中任何时刻只有当前语言一张表。</para>
    ///
    /// <para>语言切换成功后经 EventManager 广播 <see cref="LanguageChangedEvent"/>，
    /// 界面层（<see cref="LocalizedText"/>）自动刷新。编辑态预览与随包兜底走
    /// <see cref="L10nTableStore"/>（Resources 通道）。</para>
    ///
    /// <para>Runtime i18n service. Language tables are loaded on demand via
    /// AssetsManager (one table per switch, YooAsset hot-update friendly).</para>
    /// </summary>
    [Manager(Order = 99700)]
    [Dependency(typeof(EventManager))]
    public sealed class I18NManager : MonoSingleton<I18NManager>, ISingleton
    {
        /// <summary>
        /// 查询失败时返回的占位文本前缀
        /// <para>显式返回可辨识的串而不是空串——界面上出现 [Missing:xxx] 一眼就知道是查表没命中，
        /// 而空白无法区分「译文本身是空」还是「key 没找到」。</para>
        /// </summary>
        public const string MissingPrefix = "[Missing:";

        /// <summary>兜底语言代码：目标语言加载失败时回落这里 </summary>
        private const string FallbackLanguage = LanguageCode.En;

        /// <summary>
        /// PlayerPrefs 中保存玩家语言选择的键
        /// <para>带包名前缀避免与工程内其他设置撞名。</para>
        /// </summary>
        private const string PrefsLanguageKey = "EF.Localization.Language";

        /// <summary>
        /// 语言表资源路径模板（{0} = 语言代码）
        /// <para>经 AssetsManager 加载，路径须与 Yoo 收集器配置一致；
        /// 项目资源位置不同时，在启动引导中覆写本字段。</para>
        /// <para>Asset path template for language tables ({0} = language code).</para>
        /// </summary>
        public static string TableAssetPathFormat = "l10n_tbtext_{0}";

        /// <summary>
        /// 是否把玩家选择持久化（内部开关，始终为 true）
        /// <para>玩家切换语言后立即写入 PlayerPrefs 并在下次启动恢复；
        /// 收敛为固定行为，不再对外暴露关闭入口（按场景强制语言请直接调用
        /// <see cref="SwitchLanguage"/>，持久化不构成障碍）。</para>
        /// </summary>
        private bool AutoPersist { get; set; } = true;

        /// <summary>
        /// 当前语言代码，未初始化时为空串
        /// <para>在装载完成前先行赋值（并发防串：装载完成时若语言已被再次切换，结果作废）。</para>
        /// </summary>
        private string _currentLanguage = string.Empty;

        /// <summary>
        /// 当前语言的译文表（仅当前语言驻留内存；切换时整体替换，旧表交给 GC）
        /// </summary>
        private Dictionary<string, string> _table;

        /// <summary>
        /// 装载请求代数（每次发起装载/切换时递增）
        /// <para>异步完成后与当前代数比对：不一致说明期间已发起新的装载请求，
        /// 本次结果作废——防止慢速的旧装载覆盖新选择。</para>
        /// </summary>
        private int _loadGeneration;

        /// <summary>
        /// 当前语言的译文表是否已在内存
        /// <para>未装载时界面组件保持美术填写的原文，不刷 [Missing]。</para>
        /// </summary>
        public bool IsLoaded => _table != null;

        /// <summary>
        /// 当前语言代码，未初始化时为空串
        /// <para>Current language code, empty string when not initialized.</para>
        /// </summary>
        public string CurrentLanguage => _currentLanguage;

        #region 生命周期 Lifecycle

        /// <summary>
        /// 由 EFC 驱动调用（排序在 EventManager 之后，事件服务已就绪）
        /// <para>确定初始语言（玩家偏好优先，否则兜底 en）后<strong>异步装载</strong>该语言的表；
        /// 装载完成前启用的界面保持美术原文，装载完成广播事件后自动刷新。</para>
        /// <para>闸门：随包副本不存在（<see cref="L10nTableStore.HasTables"/> 为 false）
        /// = 工程未接入多语言，静默（防分发污染）。副本同时是 Yoo 工程的接入标记，
        /// 建议始终保留（兼作编辑器预览源与热更失败兜底）。</para>
        /// <para>资源系统尚未就绪导致装载失败时，可在其就绪后补调
        /// <see cref="LoadLanguageAsync"/> 重试。</para>
        /// </summary>
        public void Init()
        {
            // 闸门：随包副本不存在 = 工程未接入多语言，静默（防分发污染）
            if (!L10nTableStore.HasTables) return;

            // 已初始化过（如使用者提前手动指定过语言）不重复走默认流程
            if (!string.IsNullOrEmpty(_currentLanguage)) return;

            _currentLanguage = ResolveInitialLanguage();
            LoadLanguageAsync(_currentLanguage).Forget();
        }

        /// <summary>由 EFC 驱动调用；复位当前语言与译文表 </summary>
        public void Quit()
        {
            _table = null;
            _currentLanguage = string.Empty;
        }

        #endregion

        #region 公开方法 Public Methods

        /// <summary>
        /// 按 key 取译文
        /// <para>查当前语言的译文表，未命中时返回 <c>[Missing:key]</c> 而非空串，
        /// 便于一眼定位缺词。未装载（表未在内存）时同样返回占位符——
        /// 调用方若关心该场景，应先判 <see cref="IsLoaded"/>。</para>
        /// <para><b>按需加载的取舍：</b>内存中只有当前语言一张表，en 兜底表不再常驻——
        /// 非当前语言的缺词会直接显示 [Missing]，倒逼翻译完整。</para>
        /// <para>Look up a translation from the in-memory table of the current language;
        /// missing entries become a visible placeholder.</para>
        /// </summary>
        /// <param name="key">本地化 key</param>
        /// <returns>译文；未命中时返回 <c>[Missing:{key}]</c></returns>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_table == null) return BuildMissing(key);

            return _table.TryGetValue(key, out var value) ? value : BuildMissing(key);
        }

        /// <summary>
        /// 按 key 取译文并格式化占位符（Dynamic 文案专用入口）
        /// <para>key 在清单中登记为 Dynamic 时（值含 <c>{0}</c> <c>{1}</c>… 占位符），
        /// 用本方法传入参数：<c>GetFormat("Txt_Version", "1.2.3")</c> → <c>v1.2.3</c>。
        /// Static 文案（无占位符）误传参数也安全——多余参数被忽略，原样返回译文。</para>
        /// <para>占位符与参数不匹配（缺参 / 格式非法）时不抛异常：返回未格式化原文并告警，
        /// 界面显示 <c>v{0}</c> 比调用链中断更容易定位问题。</para>
        /// <para>Fetch a translation and format its placeholders. Falls back to the raw
        /// value with a warning when args mismatch.</para>
        /// </summary>
        /// <param name="key">本地化 key</param>
        /// <param name="args">占位符参数，按序对应 <c>{0}</c> <c>{1}</c>…</param>
        /// <returns>格式化后的译文；格式化失败时返回未格式化原文</returns>
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
                Debug.LogWarning($"[I18NManager] 文案格式化失败（key：{key}，参数数：{args.Length}），已返回原文。\n{e.Message}");
                return raw;
            }
        }

        /// <summary>
        /// 切换语言
        /// <para>经 AssetsManager <strong>按需加载目标语言的表</strong>（Yoo 热更友好），
        /// 成功后整体替换内存中的旧表（旧表交给 GC 回收——任何时刻只有当前语言驻留），
        /// 广播事件并持久化玩家选择。</para>
        /// <para>加载失败（资源缺失 / 资源系统未就绪）时保持原语言不变并返回 false，
        /// 原译文表继续生效。</para>
        /// <para>Switch language on demand; keeps the old language on failure.</para>
        /// </summary>
        /// <param name="language">目标语言代码（须存在对应语言表资源）</param>
        /// <returns>切换是否成功；语言为空 / 与当前相同且已装载 / 加载失败时返回 false</returns>
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
        /// 装载指定语言的表并应用（供启动引导与重试使用）
        /// <para>目标语言加载失败时自动回落兜底语言（en）——按需加载下非当前语言的表
        /// 不驻留内存，各语言翻译需完整，否则缺词显示 [Missing]。</para>
        /// <para>Loads and applies a language table; falls back to the fallback
        /// language on failure.</para>
        /// </summary>
        /// <param name="language">语言代码</param>
        /// <returns>是否成功装载并应用了某个语言的表</returns>
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
                    Debug.LogError($"[I18NManager] 兜底语言 {FallbackLanguage} 的语言表缺失，本地化不可用。");
                    return false;
                }

                Debug.LogWarning($"[I18NManager] 语言 {language} 加载失败，已回落 {FallbackLanguage}。");
                if (!await TryApplyAsync(generation, FallbackLanguage)) return false;
            }

            return true;
        }

        #endregion

        #region 私有方法 Private Methods

        /// <summary>
        /// 应用指定语言的表（装载管线的终点）
        /// <para>成功：替换内存表、记录当前语言并广播事件；
        /// 失败：不动任何状态（由调用方决定回退策略）。
        /// 代数不一致说明期间已发起新的装载请求，本次结果作废。</para>
        /// </summary>
        /// <param name="generation">发起本次装载时的代数</param>
        /// <param name="language">语言代码</param>
        /// <returns>是否成功应用</returns>
        private async UniTask<bool> TryApplyAsync(int generation, string language)
        {
            var table = await LoadTableAsync(language);
            if (generation != _loadGeneration) return false;   // 已被更新的请求抢占
            if (table == null || table.Count == 0) return false;

            _table = table;
            _currentLanguage = language;
            Debug.Log($"[I18NManager] 语言表装载完成：{language}（{table.Count} 条）");
            NotifyLanguageChanged(language);
            return true;
        }

        /// <summary>
        /// 经 AssetsManager 加载并解析指定语言的表
        /// </summary>
        /// <param name="language">语言代码</param>
        /// <returns>key → 译文映射；资源缺失时返回 null</returns>
        private async UniTask<Dictionary<string, string>> LoadTableAsync(string language)
        {
            var asset = await AssetsManager.Instance.LoadAsync<TextAsset>(
                string.Format(TableAssetPathFormat, language));

            return asset != null ? ParseTable(asset.text) : null;
        }

        /// <summary>
        /// 解析语言表 JSON
        /// <para>解析失败属于真故障（有表但读不出），会输出错误日志并按空表处理。</para>
        /// </summary>
        /// <param name="text">表 JSON 文本</param>
        /// <returns>解析结果；文本为空或解析失败返回空表（不为 null）</returns>
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
                Debug.LogError($"[I18NManager] 解析语言表失败\n{e.Message}");
            }

            return table;
        }

        /// <summary>
        /// 确定初始语言：玩家偏好优先，否则兜底语言
        /// <para>存档语言是否真的可用由装载阶段校验（加载失败自动回落兜底语言）。</para>
        /// </summary>
        /// <returns>初始语言代码</returns>
        private string ResolveInitialLanguage()
        {
            if (!AutoPersist) return FallbackLanguage;

            var saved = PlayerPrefs.GetString(PrefsLanguageKey, string.Empty);
            return string.IsNullOrEmpty(saved) ? FallbackLanguage : saved;
        }

        /// <summary>
        /// 保存玩家选择的语言
        /// </summary>
        /// <param name="language">语言代码</param>
        private static void SavePersistedLanguage(string language)
        {
            if (string.IsNullOrEmpty(language)) return;
            PlayerPrefs.SetString(PrefsLanguageKey, language);
            // 立刻落盘：不落盘的话，进程被系统杀掉（安卓后台回收）时这次设置会丢
            PlayerPrefs.Save();
        }

        /// <summary>构造缺词占位文本 </summary>
        private static string BuildMissing(string key) => $"{MissingPrefix}{key}]";

        /// <summary>广播语言切换事件 </summary>
        private static void NotifyLanguageChanged(string language)
        {
            if (EventManager.Instance != null)
                EventManager.Instance.Publish(new LanguageChangedEvent(language));
        }

        #endregion

        /// <summary>
        /// 语言表单行（与导表产物 JSON 结构一一对应）
        /// <para>字段名必须与 Luban 导出的 JSON 键完全一致（key / value），不可改名。</para>
        /// </summary>
        [Serializable]
        private sealed class TableRow
        {
            /// <summary>本地化 key </summary>
            public string key = string.Empty;

            /// <summary>译文 </summary>
            public string value = string.Empty;
        }
    }
}
