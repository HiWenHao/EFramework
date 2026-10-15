/*
 * ================================================
 * Describe:        运行时多语言服务：管理当前语言与翻译数据源，按 key 提供译文
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 16:40:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-14 17:10:00
 * ScriptVersion:   0.2
 * ================================================
 */

using System;
using EasyFramework;
using EasyFramework.Managers;
using EasyFramework.Managers.Event;
using UnityEngine;

namespace EasyFramework.Localization
{
    /// <summary>
    /// 多语言服务
    /// <para>持有当前语言与一个 <see cref="IL10nSource"/> 数据源，按 key 提供译文；
    /// 语言切换成功后经 EventManager 广播 <see cref="LanguageChangedEvent"/>，
    /// 界面层（<see cref="LocalizedText"/>）自动刷新。</para>
    ///
    /// <para><b>依赖方向说明：</b>本类<strong>不认识 Luban，也不认识任何配置表方案</strong>，
    /// 只认识 <see cref="IL10nSource"/>。数据源由使用者在自己的工程里实现并注册，
    /// 因此本包经 UPM 分发后可在任意工程中独立编译。</para>
    /// <para>Runtime i18n service. Depends only on <see cref="IL10nSource"/>, so this
    /// package stays compilable in any consuming project.</para>
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
        /// 本包已知的语言代码
        /// <para>仅用于<strong>拒绝</strong>明显错误的切换请求，不作为「支持语言」的完整清单
        /// ——真正的语言支持情况由数据源决定。</para>
        /// </summary>
        private static readonly string[] KnownLanguages = { LanguageCode.En, LanguageCode.Zn };

        #region 字段 Fields

        /// <summary>
        /// 翻译数据源，由使用者注册。未注册时本服务不可用。
        /// <para>刻意不给默认实现：包不知道使用者用 Luban、远端下发还是手写字典，
        /// 擅自塞一个 Luban 实现会把包硬绑回 Luban。</para>
        /// </summary>
        private IL10nSource _source;

        /// <summary>当前语言代码，未注册数据源时为空串（表示服务不可用） </summary>
        private string _currentLanguage = string.Empty;

        /// <summary>数据源是否已注册（服务是否可用） </summary>
        public bool IsLoaded => _source != null;

        /// <summary>
        /// 取当前已注册的数据源，未注册时返回 null
        /// <para>供编辑器预览使用：编辑器需要与运行时读到<strong>同一份数据</strong>，
        /// 才能保证「编辑器里看到的」等于「游戏里显示的」。</para>
        /// <para>Returns the registered source, or null. Used by the editor preview
        /// so it reads exactly the same data as the running game.</para>
        /// </summary>
        public IL10nSource TryGetSource() => _source;

        /// <summary>当前语言代码，服务不可用时为空串 </summary>
        public string CurrentLanguage => _currentLanguage;

        #endregion

        #region 生命周期 Lifecycle

        /// <summary>
        /// 由 EFC 驱动调用；Manager 排序在 EventManager 之后，确保事件服务已就绪
        /// <para>本方法<strong>不做初始化动作</strong>：数据源只能由使用者在自己的代码里注册。
        /// 这里只做一次提示，日志里直接给出注册示例，省得使用者回头翻文档。</para>
        /// </summary>
        public void Init()
        {
            if (_source != null) return;

            Debug.Log("[I18NManager] 本地化服务已启动，等待注册数据源（IL10nSource）。" +
                      "请在使用者工程的启动代码中调用：" +
                      "I18NManager.Instance.RegisterSource(new LubanL10nSource());");
        }

        /// <summary>由 EFC 驱动调用 </summary>
        public void Quit() => ResetSource();

        /// <summary>释放数据源并复位状态 </summary>
        private void ResetSource()
        {
            _source = null;
            _currentLanguage = string.Empty;
        }

        #endregion

        #region 公开方法 Public Methods

        /// <summary>
        /// 注册翻译数据源
        /// <para>使用者在自己工程的启动处调用一次即可；若当前尚无语言，
        /// 会以兜底语言（en）作为初始语言并广播一次事件，使已启用的界面立即取到文案。</para>
        /// <para>Register the translation source. Called once by the consuming project.</para>
        /// </summary>
        /// <param name="source">数据源实现；传 null 视为注销</param>
        public void RegisterSource(IL10nSource source)
        {
            _source = source;

            if (_source == null)
            {
                ResetSource();
                Debug.LogWarning("[I18NManager] 数据源已置空，本地化服务不可用。");
                return;
            }

            // 首个数据源注册时确定初始语言，否则界面永远显示缺词占位
            if (string.IsNullOrEmpty(_currentLanguage))
            {
                _currentLanguage = AutoPersist ? LoadPersistedLanguage() : FallbackLanguage;

                Debug.Log($"[I18NManager] 数据源已注册，初始语言 {_currentLanguage}" +
                          $"{(AutoPersist ? "（玩家偏好）" : "（默认）")}。");

                NotifyLanguageChanged(_currentLanguage);
            }
        }

        /// <summary>
        /// 读取玩家上次选择的语言
        /// </summary>
        /// <returns>
        /// 存档中的语言；未存档、存档为空或该语言已不在支持列表中时返回兜底语言
        /// </returns>
        private static string LoadPersistedLanguage()
        {
            var saved = PlayerPrefs.GetString(PrefsLanguageKey, string.Empty);

            if (string.IsNullOrEmpty(saved)) return FallbackLanguage;

            // 存档里的语言可能已被本版本移除，这里校验一次，
            // 否则界面会用一种「所有 key 都缺词」的语言展示，比回落到 en 更糟
            if (Array.IndexOf(KnownLanguages, saved) < 0)
            {
                Debug.LogWarning($"[I18NManager] 存档中的语言 {saved} 已不受支持，改用 {FallbackLanguage}。");
                return FallbackLanguage;
            }

            return saved;
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

        /// <summary>
        /// 按 key 取译文
        /// <para>先查当前语言，未命中回落到兜底语言（en），
        /// 两边都没有时返回 <c>[Missing:key]</c> 而非空串，便于一眼定位缺词。</para>
        /// <para>Look up a translation; falls back to the fallback language, then to a
        /// visible placeholder so missing entries are obvious in-game.</para>
        /// </summary>
        /// <param name="key">本地化 key</param>
        /// <returns>译文；未命中时返回 <c>[Missing:{key}]</c></returns>
        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_source == null) return BuildMissing(key);

            // 先查当前语言
            var value = _source.Get(key, _currentLanguage);
            if (!string.IsNullOrEmpty(value)) return value;

            // 回落兜底语言（当前语言本身就是兜底语言时这步是多余的，但避免重复逻辑）
            if (_currentLanguage != FallbackLanguage)
            {
                value = _source.Get(key, FallbackLanguage);
                if (!string.IsNullOrEmpty(value)) return value;
            }

            return BuildMissing(key);
        }

        /// <summary>构造缺词占位文本 </summary>
        private static string BuildMissing(string key) => $"{MissingPrefix}{key}]";

        /// <summary>
        /// 切换语言
        /// <para>仅当语言真正发生变化时才广播事件，避免重复设置同一语言引发无谓刷新。</para>
        /// <para>Switch language and broadcast on success.</para>
        /// </summary>
        /// <param name="language">目标语言代码，取值见 <see cref="LanguageCode"/></param>
        /// <returns>切换是否成功；服务不可用 / 语言为空 / 语言未知 / 语言未变化时返回 false</returns>
        public bool SwitchLanguage(string language)
        {
            if (_source == null)
            {
                Debug.LogWarning("[I18NManager] 服务未注册数据源，无法切换语言。");
                return false;
            }

            if (string.IsNullOrEmpty(language))
                return false;

            if (Array.IndexOf(KnownLanguages, language) < 0)
            {
                // 这里必须拦：一旦放行未知语言，Get 会静默回落到兜底语言，
                // 界面显示英文却让用户以为是所选语言——比直接报错更难排查。
                Debug.LogWarning($"[I18NManager] 不支持的语言：{language}。" +
                                 $"当前可选：{string.Join(" / ", KnownLanguages)}");
                return false;
            }

            if (language == _currentLanguage) return false;

            _currentLanguage = language;
            if (AutoPersist) SavePersistedLanguage(language);

            NotifyLanguageChanged(language);
            return true;
        }

        /// <summary>
        /// 当前支持的全部语言代码
        /// <para>供语言选择 UI 生成选项，避免界面硬编码语言列表。</para>
        /// <para>All supported language codes, for building a language-picker UI.</para>
        /// </summary>
        public static string[] GetSupportedLanguages() => (string[])KnownLanguages.Clone();

        /// <summary>广播语言切换事件 </summary>
        private static void NotifyLanguageChanged(string language)
        {
            if (EventManager.Instance != null)
                EventManager.Instance.Publish(new LanguageChangedEvent(language));
        }

        #endregion
    }
}