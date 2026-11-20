/*
 * ================================================
 * Describe:      UI视窗动画目录 SO。按类型名匹配动画预设，未匹配时回退默认。
 *                UI View animation catalog. Matches by type name, falls back to default.
 * Author:        Alvin8412
 * CreationTime:  2026-06-22 15:45:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-06-22 15:45:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFramework.Managers.Ui
{
    /// <summary>
    /// UI视窗动画目录。按<see cref="UIViewType"/>匹配预设，未匹配时回退到默认预设。
    /// <para>UI view animation directory. Match preset based on<see cref="UIViewType"/>
    /// And revert to default preset if no match is found</para>
    /// </summary>
    public class UiAnimationConfig : ScriptableObject
    {
        /// <summary>
        /// 单条动画预设条目
        /// <para>Single animation preset entry</para>
        /// </summary>
        [Serializable]
        public struct Entry
        {
            [HeaderPro("匹配", "Match", HeaderProType.Subtitle)]
            [HeaderPro("视窗类型", "UI view type")]
            public UIViewType viewType;

            [HeaderPro("动画", "Animation", HeaderProType.Subtitle)]
            [HeaderPro("动画类型", "Animation type")]
            public UiViewAnimationType type;

            [HeaderPro("动画过渡时长", "Duration of animation")]
            public float duration;

            [HeaderPro("动画曲线（留空则取全局默认）", "Animation curve (empty = global default)")]
            public AnimationCurve curve;
        }

        [HeaderPro("单视图预设", "Per-view presets", HeaderProType.Title)]
        [HeaderPro("按视窗类型匹配，命中即用该条目；未命中回退下方默认值",
            "Matched by view type; falls back to the defaults below when no entry matches")]
        [SerializeField]
        private List<Entry> entries = new()
        {
            new Entry
            {
                viewType = UIViewType.Popup,
                type = UiViewAnimationType.None
            }
        };

        [HeaderPro("", "", HeaderProType.Separator), Space]
        [HeaderPro("全局默认值", "Global defaults", HeaderProType.Title)]
        [HeaderPro("无匹配条目时的过渡时长", "Transition duration when no entry matches")]
        [SerializeField]
        private float defaultDuration = 0.15f;

        [HeaderPro("无匹配条目时的动画曲线", "Animation curve when no entry matches")]
        [SerializeField]
        private AnimationCurve defaultCurve = AnimationCurve.EaseInOut(0, 0.75f, 1, 1);

        /// <summary>
        /// 按视窗类型取动画时长与曲线；无匹配条目时回退默认值。
        /// 供已自行指定动画类型的 View 使用——动画类型/是否反播由 View 自身决定，此处仅提供时长与曲线。
        /// </summary>
        /// <param name="viewType">UI视窗类型</param>
        /// <param name="duration">输出动画时长</param>
        /// <param name="curve">输出动画曲线</param>
        public void GetDurationAndCurve(UIViewType viewType, out float duration, out AnimationCurve curve)
        {
            foreach (var e in entries)
            {
                if (e.viewType != viewType) continue;
                duration = e.duration;
                curve = e.curve ?? defaultCurve;
                return;
            }

            duration = defaultDuration;
            curve = defaultCurve;
        }
    }
}