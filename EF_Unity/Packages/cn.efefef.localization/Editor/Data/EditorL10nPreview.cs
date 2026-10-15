/*
 * ================================================
 * Describe:        编辑器下按 key 预览译文的数据源桥接（自动发现使用者 IL10nSource 实现）
 * Author:          Alvin5100
 * CreationTime:    2026-10-14 17:35:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-14 17:35:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// 编辑器下预览译文的数据源桥接
    /// <para><b>设计要点：不硬编码使用者的实现类名。</b>本包不能 <c>using Cfg</c>（会把包
    /// 硬绑回 Luban），但使用者侧的 <c>LubanL10nSource</c> 是通过 <see cref="IL10nSource"/>
    /// 接入的——所以这里用 <see cref="TypeCache"/> 反查出所有实现类并激活一个即可。
    /// 使用者换数据源、换目录、换语言数量，本包一行代码都不用改。</para>
    /// <para><b>为什么不用 CSV：</b>预览走的是导表后的bytes，与运行时读取的是同一份产物，
    /// 所见即所得；若读 CSV 则会出现「编辑器看到的」与「运行时显示的」不一致。</para>
    /// <para>Editor-side preview bridge. Discovers the consuming project's
    /// <see cref="IL10nSource"/> implementation via <see cref="TypeCache"/> instead of
    /// hard-coding a type name, so this package stays free of any Luban coupling.</para>
    /// </summary>
    public static class EditorL10nPreview
    {
        #region 字段 Fields

        /// <summary>
        /// 已激活的数据源实例
        /// <para>不缓存到磁盘，仅进程内复用：bytes 读取有IO 开销，
        /// 但 Inspector 每帧都会调用，必须复用同一个实例。</para>
        /// </summary>
        private static IL10nSource _cached;

        /// <summary>已尝试过激活且失败，避免每帧重复反射报错 </summary>
        private static bool _activationFailed;

        /// <summary>
        /// 编辑器预览当前使用的语言
        /// <para>与运行时的当前语言<strong>刻意独立</strong>：编辑器下 Runtime 还没起来，
        /// 且预览语言是「美术要看哪一版」的主观选择，不该被运行时切换影响。</para>
        /// </summary>
        public static string PreviewLanguage = LanguageCode.Zn;

        #endregion

        #region 公开方法 Public Methods

        /// <summary>
        /// 使缓存失效，下次 <see cref="Get"/> 重新激活数据源
        /// <para>重新导表 / 换了语言之后调用，使预览立刻反映最新bytes。</para>
        /// </summary>
        public static void Invalidate()
        {
            _cached = null;
            _activationFailed = false;
        }

        /// <summary>
        /// 取译文预览
        /// </summary>
        /// <param name="key">本地化 key</param>
        /// <returns>
        /// 译文；未命中或数据源不可用时返回 <c>null</c>（由调用方决定显示占位还是警告）
        /// </returns>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            var source = ResolveSource();
            if (source == null) return null;

            var value = source.Get(key, PreviewLanguage);

            // 预览语言没有该词时回落到兜底语言，与运行时行为保持一致
            if (string.IsNullOrEmpty(value) && PreviewLanguage != LanguageCode.En)
                value = source.Get(key, LanguageCode.En);

            return string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// 预览数据源是否可用
        /// <para>供 Inspector 区分「没找到数据源」与「数据源有但缺这个词」两种情况——
        /// 前者是配置问题，后者是翻译缺失，提示文案完全不同。</para>
        /// </summary>
        public static bool IsSourceAvailable => ResolveSource() != null;

        #endregion

        #region 私有方法 Private Methods

        /// <summary>
        /// 解析可用的数据源实例
        /// <para>优先复用运行时的 <see cref="I18NManager"/>（播放模式或已注册时），
        /// 这保证预览与运行时读到的是同一份数据；否则反射激活一个使用者实现。</para>
        /// </summary>
        private static IL10nSource ResolveSource() 
        {
            // 播放模式 / 使用者已手动注册：直接用运行时的那个，所见即所得
            if (Application.isPlaying && I18NManager.Instance != null && I18NManager.Instance.IsLoaded)
                return I18NManager.Instance.TryGetSource();

            if (_cached != null) return _cached;
            if (_activationFailed) return null;

            try
            {
                _cached = ActivateFromTypeCache();
                if (_cached == null) _activationFailed = true;
            }
            catch (Exception e)
            {
                _activationFailed = true;
                Debug.LogError($"[Localization] 编辑器预览数据源激活失败：{e.Message}\n" +
                               "若使用Luban，请确认已导表并把 bytes 拷进 Resources/Luban/。");
            }

            return _cached;
        }

        /// <summary>
        /// 用 <see cref="TypeCache"/> 反查并激活使用者侧的数据源
        /// <para>优先选public 无参可构造的类；构造失败或不可用则返回 null，
        /// 宁可显示「无数据源」也不让整个 Inspector 崩掉。</para>
        /// </summary>
        private static IL10nSource ActivateFromTypeCache()
        {
            // 先按类型名排序，保证多个实现时选择稳定（TypeCache 返回顺序不保证）
            var candidates = new List<Type>(TypeCache.GetTypesDerivedFrom<IL10nSource>());
            if (candidates.Count == 0) return null;

            candidates.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));

            foreach (var type in candidates)
            {
                // 接口/抽象类/泛型定义无法实例化，直接跳过
                if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters) continue;
                // 非 public 的类 Unity 可能无法实例化
                if (!type.IsPublic) continue;

                try
                {
                    var instance = Activator.CreateInstance(type) as IL10nSource;
                    if (instance != null) return instance;
                }
                catch (Exception)
                {
                    // 该实现构造失败（常见于表没导好），试下一个
                }
            }

            return null;
        }

        #endregion
    }
}