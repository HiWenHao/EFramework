/*
 * ================================================
 * Describe:        多语言文本组件：持有本地化 key 字符串
 * Author:          Alvin5100
 * CreationTime:    2026-10-10 19:15:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 16:38:00
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
    /// 本地化文本
    /// <para>继承 TextMeshProUGUI，key 以字符串存储；编辑器通过下拉 / 搜索窗选填，
    /// 运行时由 <see cref="I18NManager"/> 按 key 提供文案。语言切换时自动刷新，
    /// 无需业务代码遍历界面。</para>
    /// <para>Inherits TextMeshProUGUI; refreshes itself on language change.</para>
    /// </summary>
    [AddComponentMenu("UI/TextMeshPro - Localization")]
    [DisallowMultipleComponent]
    public class LocalizedText : TextMeshProUGUI
    {
        /// <summary>
        /// 未选择 key 时的占位值（空串）
        /// <para>用空串而非 null，便于序列化与下拉框判定。</para>
        /// <para>Use an empty string instead of null, which is more suitable for serialization.</para>
        /// </summary>
        public const string NoKey = "";

        /// <summary>
        /// 本地化 key（字符串）
        /// <para>用 string 而非 enum：包经 UPM 分发后目录只读，无法生成枚举文件；
        /// 且改 key 不触发重编译。</para>
        /// <para>Stored as string: the package directory may be read-only after UPM distribution.</para>
        /// </summary>
        [SerializeField]
        [HeaderPro("本地化文案键。可在编辑器中通过下拉或搜索窗选择。",
            "Localization text key. Selectable in the editor via dropdown or search window.")]
        private string key = NoKey;

        /// <summary>
        /// 本地化 key,赋 null 时自动回落为空串，保证不会出现 null key
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
        /// </summary>
        public bool HasKey => !string.IsNullOrEmpty(key);

        /// <summary>
        /// 格式化参数（Dynamic 文案用，与 key 值中的 {0} {1}… 按序对应）
        /// <para>序列化保存：静态参数（如版本号展示）可直接在 Inspector 的
        /// 「格式化参数」里配置；运行时变化的参数（金币数、错误码等）
        /// 用 <see cref="SetFormatArgs"/> 传入并即时刷新。</para>
        /// <para>Format args for dynamic texts, matched against {0}/{1}... in order.</para>
        /// </summary>
        [SerializeField]
        [HeaderPro("格式化参数（动态文案用）。key 值含 {0} 占位符时按序替换。",
            "Format args for dynamic texts. Replaces {0}/{1}... in order.")]
        private List<string> formatArgs = new List<string>();

        /// <summary>
        /// 当前格式化参数（只读，供 Inspector 预览格式化结果）
        /// </summary>
        public IReadOnlyList<string> FormatArgs => formatArgs;

        /// <summary>
        /// 设置运行时格式化参数并立即刷新（Dynamic 文案用）
        /// <para>参数为运行时数据（金币数、错误码等）时由代码调用；
        /// 静态参数建议直接在 Inspector 配置。传空则清除参数。</para>
        /// <para>Set runtime format args and refresh immediately.</para>
        /// </summary>
        /// <param name="args">参数，按序对应 {0} {1}…；传空则清除参数</param>
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

        #region 生命周期 Lifecycle

        /// <summary>
        /// 语言切换事件的订阅令牌
        /// </summary>
        private IDisposable _languageChangedToken;

        /// <summary>
        /// 启用时订阅语言切换事件并刷新一次
        /// <para><b>注意调用时机</b>：编辑态下组件的 OnEnable 可能早于 EFC.Initialize，
        /// 此时 <c>EFC.TryGetSingleton</c> 内部的字典尚未建立，会抛 NullReferenceException。
        /// 故此处先判 <see cref="Application.isPlaying"/>：非运行态不订阅（本来也收不到事件）。</para>
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            // 非运行态不订阅也不刷新（预览由 Inspector 负责）
            if (!Application.isPlaying) return;

            TrySubscribeLanguageChanged();
            Refresh();
        }

        /// <summary>
        /// 尝试订阅语言切换事件
        /// </summary>
        /// <returns>是否订阅成功；EventManager 尚未就绪时返回 false</returns>
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
                Debug.LogWarning($"[LocalizedText] 订阅语言切换事件失败：{e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 禁用时退订，防止对象被销毁后事件仍回调到失效引用
        /// </summary>
        protected override void OnDisable()
        {
            _languageChangedToken?.Dispose();
            _languageChangedToken = null;
            base.OnDisable();
        }

        /// <summary>
        /// 组件被添加/重置时清空 key
        /// </summary>
        protected override void Reset()
        {
            base.Reset();
            key = NoKey;
        }

        #endregion

        /// <summary>
        /// 「语言表未就绪」告警是否已打出
        /// <para>按需加载下装载完成前启用的一批组件都会走到未就绪分支，
        /// 逐条告警会刷屏——同类告警进程内只打一次。</para>
        /// </summary>
        private static bool _unreadyWarned;

        /// <summary>
        /// 刷新显示内容
        /// <para>按 key 从 <see cref="I18NManager"/>（内建 <see cref="L10nTableStore"/> 数据通道）
        /// 取译文并写入 text。未选 key 时<strong>不改写 text</strong>，以免覆盖美术在
        /// Inspector 中填写的文本；查不到译文时显示 <c>[Missing:key]</c> 便于定位缺词。</para>
        /// <para>Refresh the displayed text from the key. Does nothing when no key is set.</para>
        /// <para><b>编辑态说明：</b>本方法只在运行时改写 text。编辑器下若改写会把译文
        /// 写进场景文件，造成「谁打开过谁脏」的诡异 diff；且 Inspector 每帧都会调用。
        /// 故编辑态下静默返回，译文改由 Inspector 的预览行展示。</para>
        /// <para><b>未就绪说明：</b>语言表经 AssetsManager 异步装载，完成前启用的
        /// 界面保持美术原文，装载完成后的事件会触发本方法自动刷新。</para>
        /// </summary>
        public void Refresh()
        {
            if (!HasKey) return;

            if (!Application.isPlaying) return;

            var manager = I18NManager.Instance;
            if (manager == null || !manager.IsLoaded)
            {
                if (!_unreadyWarned)
                {
                    _unreadyWarned = true;
                    D.Warning($"[LocalizedText] 语言表尚未装载完成（经 AssetsManager 异步加载中），暂以原文显示；装载完成后自动刷新。key：{key}");
                }
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
    }
}