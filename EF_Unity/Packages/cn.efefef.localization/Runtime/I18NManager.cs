/*
 * ================================================
 * Describe:        运行时多语言服务：管理当前语言，按 key 提供译文
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 16:40:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 12:08:00
 * ScriptVersion:   0.4
 * ================================================
 */

using System;
using System.Collections.Generic;
using EasyFramework.Managers;
using EasyFramework.Managers.Event;
using UnityEngine;

namespace EasyFramework.Localization
{
    /// <summary>
    /// 多语言服务
    /// <para>持有当前语言，译文统一从 <see cref="L10nTableStore"/>（Resources 下的导表产物）读取；
    /// 语言切换成功后经 EventManager 广播 <see cref="LanguageChangedEvent"/>，
    /// 界面层（<see cref="LocalizedText"/>）自动刷新。</para>
    /// <para><b>零接入成本：</b>数据通道由包内置（<see cref="L10nTableStore"/>），
    /// 不需要使用者实现任何接口或调用任何注册方法——只要 <c>Resources/L10n</c> 下
    /// 存在 <c>l10n_tbtext_*.json</c>，本服务在 Init 时即自动装载；
    /// 一张表都没有时保持静默（未接入多语言的工程不刷任何日志）。</para>
    /// <para>Runtime i18n service. Reads translations from the built-in
    /// <see cref="L10nTableStore"/>; fully automatic, zero consumer code.</para>
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

        /// <summary>兜底语言代码：任何语言缺词都回落这里 </summary>
        private const string FallbackLanguage = LanguageCode.En;

        /// <summary>
        /// PlayerPrefs 中保存玩家语言选择的键
        /// <para>带包名前缀避免与工程内其他设置撞名。</para>
        /// </summary>
        private const string PrefsLanguageKey = "EF.Localization.Language";

        /// <summary>
        /// 是否把玩家选择持久化
        /// <para>关掉后每次启动都回到默认语言（适合调试或按场景强制语言）。</para>
        /// </summary>
        public bool AutoPersist { get; set; } = true;

        /// <summary>
        /// 当前语言代码，未装载时为空串
        /// </summary>
        private string _currentLanguage = string.Empty;

        /// <summary>
        /// 服务是否可用（磁盘上存在至少一张语言表）
        /// <para>语言表是随包只读数据，装载动作集中在 <see cref="L10nTableStore"/> 内懒执行，
        /// 这里只做闸门判断：无表时界面组件保持美术填写的原文，不刷 [Missing]。</para>
        /// </summary>
        public bool IsLoaded => L10nTableStore.HasTables;

        /// <summary>
        /// 当前语言代码，未装载时为空串
        /// <para>Current language code, empty string when not loaded</para>
        /// </summary>
        public string CurrentLanguage => _currentLanguage;

        #region 生命周期 Lifecycle

        /// <summary>
        /// 由 EFC 驱动调用（排序在 EventManager 之后，事件服务已就绪）
        /// <para>自动装载：无表 → 静默返回（本工程未接入多语言，属正常状态）；
        /// 有表 → 确定初始语言并广播一次事件，让已启用的界面立即取到文案。
        /// 初始语言优先取玩家上一次的选择（需在当前语言表集合内），否则回落兜底语言。</para>
        /// </summary>
        public void Init()
        {
            // 闸门一：一张语言表都没有 = 工程未接入多语言，静默（防分发污染）
            if (!L10nTableStore.HasTables) return;

            // 已初始化过（如使用者提前手动指定过语言）不重复走默认流程
            if (!string.IsNullOrEmpty(_currentLanguage)) return;

            _currentLanguage = ResolveInitialLanguage();

            Debug.Log($"[I18NManager] 语言表装载完成，当前语言 {_currentLanguage}" +
                      $"{(AutoPersist ? "（玩家偏好）" : "（默认）")}，" +
                      $"可选：{string.Join(" / ", L10nTableStore.Languages)}");

            NotifyLanguageChanged(_currentLanguage);
        }

        /// <summary>由 EFC 驱动调用；仅复位当前语言（语言表缓存随域重载自然清空） </summary>
        public void Quit() => _currentLanguage = string.Empty;

        #endregion

        #region 公开方法 Public Methods

        /// <summary>
        /// 按 key 取译文
        /// <para>先查当前语言，未命中回落到兜底语言（en），
        /// 两边都没有时返回 <c>[Missing:key]</c> 而非空串，便于一眼定位缺词。
        /// 未装载（无表）时同样返回 <c>[Missing:key]</c>——
        /// 调用方若关心无表场景，应先判 <see cref="IsLoaded"/>。</para>
        /// <para>Look up a translation; falls back to the fallback language, then to a
        /// visible placeholder so missing entries are obvious in-game.</para>
        /// </summary>
        /// <param name="key">本地化 key</param>
        /// <returns>译文；未命中时返回 <c>[Missing:{key}]</c></returns>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            // 未初始化（Init 尚未被 EFC 调到）时先用兜底语言顶上，
            // 否则首帧启用的界面会拿不到文案
            var language = string.IsNullOrEmpty(_currentLanguage) ? FallbackLanguage : _currentLanguage;

            var value = L10nTableStore.TryGet(key, language);
            if (!string.IsNullOrEmpty(value)) return value;

            // 回落兜底语言（当前语言本身就是兜底语言时这步是多余的，但避免重复逻辑）
            if (language != FallbackLanguage)
            {
                value = L10nTableStore.TryGet(key, FallbackLanguage);
                if (!string.IsNullOrEmpty(value)) return value;
            }

            return BuildMissing(key);
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
        /// <para>仅当语言真正发生变化时才广播事件，避免重复设置同一语言引发无谓刷新。</para>
        /// <para>Switch language and broadcast on success.</para>
        /// </summary>
        /// <param name="language">目标语言代码（须在语言表集合内）</param>
        /// <returns>切换是否成功；无表 / 语言为空 / 语言未发现 / 语言未变化时返回 false</returns>
        public bool SwitchLanguage(string language)
        {
            if (!L10nTableStore.HasTables)
            {
                Debug.LogWarning("[I18NManager] 未找到语言表（Resources/L10n/l10n_tbtext_*.json），无法切换语言。");
                return false;
            }

            if (string.IsNullOrEmpty(language)) return false;

            if (!ContainsLanguage(L10nTableStore.Languages, language))
            {
                Debug.LogWarning($"[I18NManager] 不支持的语言：{language}。" +
                                 $"当前可选：{string.Join(" / ", L10nTableStore.Languages)}");
                return false;
            }

            if (language == _currentLanguage) return false;

            _currentLanguage = language;
            if (AutoPersist) SavePersistedLanguage(language);

            NotifyLanguageChanged(language);
            return true;
        }

        /// <summary>
        /// 当前可用的全部语言代码
        /// <para>供语言选择 UI 生成选项，来自语言表文件名自动发现，新增语言零改码。</para>
        /// <para>All available language codes, discovered from table file names.</para>
        /// </summary>
        public IReadOnlyList<string> GetSupportedLanguages() => L10nTableStore.Languages;

        #endregion

        #region 私有方法 Private Methods

        /// <summary>
        /// 确定初始语言：玩家偏好优先（校验仍受支持），否则兜底语言
        /// </summary>
        /// <returns>初始语言代码</returns>
        private string ResolveInitialLanguage()
        {
            if (!AutoPersist) return FallbackLanguage;

            var saved = PlayerPrefs.GetString(PrefsLanguageKey, string.Empty);
            if (string.IsNullOrEmpty(saved)) return FallbackLanguage;

            // 存档里的语言可能已被本版本移除，这里校验一次，
            // 否则界面会用一种「所有 key 都缺词」的语言展示，比回落到 en 更糟
            if (!ContainsLanguage(L10nTableStore.Languages, saved))
            {
                Debug.LogWarning($"[I18NManager] 存档中的语言 {saved} 已不在语言表内，改用 {FallbackLanguage}。");
                return FallbackLanguage;
            }

            return saved;
        }

        /// <summary>
        /// 判断语言列表是否包含指定语言
        /// </summary>
        private static bool ContainsLanguage(IReadOnlyList<string> languages, string language)
        {
            if (languages == null) return false;

            foreach (var item in languages)
            {
                if (string.Equals(item, language, StringComparison.Ordinal)) return true;
            }

            return false;
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
    }
}
