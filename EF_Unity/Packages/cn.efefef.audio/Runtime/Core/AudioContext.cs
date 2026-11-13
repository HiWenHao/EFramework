/*
 * ================================================
 * Describe:      音频服务共享运行时上下文。设置实现写、播放实现读，避免接口之间互相持有引用。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 11:20
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 11:20
 * ScriptVersion: 1.0
 * ================================================
 */

using System;
using UnityEngine;
using UnityEngine.Audio;

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 音频服务共享上下文，存放配置、场景引用与共享状态
    /// <para>Shared context holding config, scene references and shared state</para>
    /// </summary>
    public sealed class AudioContext
    {
        #region 门面序列化配置快照

        /// <summary>
        /// 主混音器资源
        /// <para>Main AudioMixer asset.</para>
        /// </summary>
        public AudioMixer Mixer;

        /// <summary>
        /// 音效对象池最大空闲数量
        /// <para>Max idle AudioSources in the effect pool.</para>
        /// </summary>
        public int MaxEffectPoolSize;

        /// <summary>
        /// 音效对象池预热数量
        /// <para>Prewarm count of the effect pool.</para>
        /// </summary>
        public int PrewarmCount;

        /// <summary>
        /// 音效播放器闲置超时（秒）
        /// <para>Effect idle timeout in seconds</para>
        /// </summary>
        public float EffectIdleTimeout;

        /// <summary>
        /// 默认 BGM 音量
        /// <para>Default BGM volume.</para>
        /// </summary>
        public float DefaultBgmVolume;

        /// <summary>
        /// 默认音效音量
        /// <para>Default effect volume.</para>
        /// </summary>
        public float DefaultEffectVolume;

        #endregion

        #region 运行时共享引用

        /// <summary>
        /// 门面根节点
        /// <para>Facade root transform</para>
        /// </summary>
        public Transform OwnerRoot;

        /// <summary>
        /// 音效对象池根节点
        /// <para>Root of the effect pool.</para>
        /// </summary>
        public Transform EffectRoot;

        /// <summary>
        /// BGM 专用播放器
        /// <para>Dedicated BGM AudioSource.</para>
        /// </summary>
        public AudioSource BgmSource;

        /// <summary>
        /// 主混音器组
        /// <para>Master mixer group.</para>
        /// </summary>
        public AudioMixerGroup MasterGroup;

        /// <summary>
        /// BGM 混音器组
        /// <para>BGM mixer group.</para>
        /// </summary>
        public AudioMixerGroup BgmGroup;

        /// <summary>
        /// 音效混音器组
        /// <para>Effect mixer group.</para>
        /// </summary>
        public AudioMixerGroup EffectGroup;

        #endregion

        #region 设置写 / 播放读（消除接口互相引用）

        /// <summary>
        /// 主音量
        /// <para>Master volume</para>
        /// </summary>
        public float MasterVolume = 1f;

        /// <summary>
        /// BGM 音量
        /// <para>BGM volume</para>
        /// </summary>
        public float BgmVolume = 1f;

        /// <summary>
        /// 音效音量
        /// <para>Effect volume</para>
        /// </summary>
        public float EffectVolume = 1f;

        /// <summary>
        /// 是否全局静音
        /// <para>Whether globally muted.</para>
        /// </summary>
        public bool IsMuted;

        #endregion

        #region 服务间通知（事件驱动，禁止直接互持引用）

        /// <summary>
        /// 混音器组变更事件
        /// <para>Raised after mixer groups change</para>
        /// </summary>
        public event Action EffectGroupChanged;

        /// <summary>
        /// 静音状态变更后触发
        /// <para>Raised after mute state changes.</para>
        /// </summary>
        public event Action MuteChanged;

        /// <summary>
        /// 触发混音器组变更通知
        /// <para>Raise the mixer group changed event.</para>
        /// </summary>
        public void NotifyEffectGroupChanged() => EffectGroupChanged?.Invoke();

        /// <summary>
        /// 触发静音变更通知
        /// <para>Raise the mute changed event.</para>
        /// </summary>
        public void NotifyMuteChanged() => MuteChanged?.Invoke();

        /// <summary>
        /// 清理所有事件订阅
        /// <para>Clear all event subscriptions</para>
        /// </summary>
        public void ClearEvents()
        {
            EffectGroupChanged = null;
            MuteChanged = null;
        }

        #endregion

        #region 门面注入的工具

        /// <summary>
        /// 统一警告入口
        /// <para>Shared warning sink</para>
        /// </summary>
        public Action<string> Warn;

        /// <summary>
        /// 查找混音器组
        /// <para>Find a mixer group</para>
        /// </summary>
        /// <param name="mixer">新的主混音器 <para>New main AudioMixer</para></param>
        /// <param name="groupName">混音器组名称 <para>Mixer group name</para></param>
        /// <param name="warn">警告输出委托 <para>Warning sink</para></param>
        /// <returns>匹配的混音器组；缺失为 null <para>Matching mixer group, or null</para></returns>
        public static AudioMixerGroup FindGroup(AudioMixer mixer, string groupName, Action<string> warn = null)
        {
            if (mixer == null) return null;
            var groups = mixer.FindMatchingGroups(groupName);
            if (groups.Length > 0) return groups[0];
            warn?.Invoke($"Mixer group '{groupName}' not found");
            return null;
        }

        #endregion
    }
}
