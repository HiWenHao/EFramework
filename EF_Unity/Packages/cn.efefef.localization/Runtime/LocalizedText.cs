/*
 * ================================================
 * Describe:        多语言文本组件：持有本地化 key 字符串
 * Author:          Alvin5100
 * CreationTime:    2026-10-10 19:15:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 18:25:00
 * ScriptVersion:   0.6
 * ================================================
 */

using System;
using System.Collections.Generic;
using EasyFramework.Edit;
using EasyFramework.Managers.Event;
using TMPro;
using UnityEngine;

namespace EasyFramework.Localization
{
    /// <summary>
    /// 本地化文本：继承 TextMeshProUGUI，key 以字符串存储，运行时由 I18NManager 按 key 提供文案，语言切换时自动刷新
    /// <para>Inherits TextMeshProUGUI; refreshes itself on language change.</para>
    /// </summary>
    [AddComponentMenu("UI/TextMeshPro - Localization")]
    [DisallowMultipleComponent]
    public class LocalizedText : TextMeshProUGUI
    {
        #region Fields

        // 本地化 key（string 而非 enum：UPM 分发后目录只读无法生成枚举，且改 key 不触发重编译）
        [SerializeField]
        [HeaderPro("本地化文案键。可在编辑器中通过下拉或搜索窗选择。",
            "Localization text key. Selectable in the editor via dropdown or search window.")]
        private string key = NoKey;

        // 格式化参数（Dynamic 文案用，与 key 值中的 {0} {1}… 按序对应；序列化保存，静态参数可直接在 Inspector 配置）
        [SerializeField]
        [HeaderPro("格式化参数（动态文案用）。key 值含 {0} 占位符时按序替换。",
            "Format args for dynamic texts. Replaces {0}/{1}... in order.")]
        private List<string> formatArgs = new List<string>();

        // 语言切换事件的订阅令牌
        private IDisposable _languageChangedToken;

        // 「语言表未就绪」告警是否已打出（按需加载下装载完成前启用的一批组件都会走到该分支，同类告警只打一次防刷屏）
        private static bool _unreadyWarned;

        /// <summary>
        /// 未选择 key 时的占位值（空串）
        /// <para>Placeholder value used when no key is selected (empty string).</para>
        /// </summary>
        public const string NoKey = "";

        #endregion

        #region Properties

        /// <summary>
        /// 本地化 key，赋 null 时自动回落为空串（运行期改 key 立即生效，不必等语言切换）
        /// <para>Localization key; null falls back to an empty string.</para>
        /// </summary>
        public string Key
        {
            get => key;
            set
            {
                key = value ?? NoKey;
                // 运行期改 key 立即生效，不必等语言切换
                Refresh();
            }
        }

        /// <summary>
        /// 是否已选择 key
        /// <para>Whether a key has been selected.</para>
        /// </summary>
        public bool HasKey => !string.IsNullOrEmpty(key);

        /// <summary>
        /// 当前格式化参数（只读，供 Inspector 预览格式化结果）
        /// <para>Current format args (read-only), for Inspector preview.</para>
        /// </summary>
        public IReadOnlyList<string> FormatArgs => formatArgs;

        #endregion

        #region Methods

        protected override void OnEnable()
        {
            base.OnEnable();

            // 非运行态不订阅也不刷新（预览由 Inspector 负责）
            if (!Application.isPlaying) return;

            TrySubscribeLanguageChanged();
            Refresh();
        }

        // 尝试订阅语言切换事件（EventManager 尚未就绪时返回 false，退化为等下一次手动 Refresh）
        private bool TrySubscribeLanguageChanged()
        {
            try
            {
                _languageChangedToken = EventManager.Instance.Subscribe<LanguageChangedEvent>(_ => Refresh());
                return _languageChangedToken != null;
            }
            catch (Exception e)
            {
                // 订阅失败不该让组件不可用：退化为「等下一次手动 Refresh」即可
                D.Warning($"[ Localization ] 订阅语言切换事件失败：{e.Message}");
                return false;
            }
        }

        protected override void OnDisable()
        {
            _languageChangedToken?.Dispose();
            _languageChangedToken = null;
            base.OnDisable();
        }

        protected override void Reset()
        {
            base.Reset();
            key = NoKey;
        }

        /// <summary>
        /// 设置运行时格式化参数并立即刷新（Dynamic 文案用；传空则清除参数）
        /// <para>Sets runtime format args and refreshes immediately.</para>
        /// </summary>
        /// <param name="args">参数，按序对应 {0} {1}…；传空则清除参数<para>Args matched in order; empty clears them.</para></param>
        public void SetFormatArgs(params object[] args)
        {
            formatArgs.Clear();
            if (args != null)
            {
                foreach (var arg in args)
                {
                    formatArgs.Add(arg?.ToString() ?? string.Empty);
                }
            }

            Refresh();
        }

        /// <summary>
        /// 刷新显示内容：按 key 从 I18NManager 取译文写入 text
        /// <para>Refreshes the displayed text from the key. Does nothing when no key is set.</para>
        /// <para>未选 key 时不改写 text，以免覆盖美术在 Inspector 中填写的文本；查不到译文时显示 [Missing:key]。</para>
        /// <para>编辑态下静默返回（改写会把译文写进场景文件造成 diff，预览由 Inspector 负责）。</para>
        /// <para>语言表经 AssetsManager 异步装载：未就绪时保持美术原文，装载完成后的事件会触发本方法自动刷新。</para>
        /// </summary>
        public void Refresh()
        {
            if (!HasKey) return;

            if (!Application.isPlaying) return;

            var manager = I18NManager.Instance;
            if (manager == null || !manager.IsLoaded)
            {
                if (_unreadyWarned) return;
                _unreadyWarned = true;
                D.Warning($"[ Localization ] 语言表未装载（经 AssetsManager 加载失败或尚未导表），暂以原文显示；装载完成后自动刷新。key：{key}");
                return;
            }

            text = manager.Get(key);

            // Dynamic 文案：组件配置了格式化参数时，把 {0} {1}… 替换为实际值
            if (formatArgs.Count <= 0 || text.IndexOf('{') < 0) return;
            try
            {
                var args = new object[formatArgs.Count];
                for (var i = 0; i < args.Length; i++)
                {
                    args[i] = formatArgs[i];
                }

                text = string.Format(text, args);
            }
            catch (FormatException)
            {
                // 占位符与参数不匹配：保持原文（v{0} 可见比静默错误更利于排查）
            }
        }

        #endregion
    }
}