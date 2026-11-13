/*
 * ================================================
 * Describe:      默认音频设置：音量、静音、混音器组、偏好持久化与淡入淡出。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 11:20
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 11:20
 * ScriptVersion: 1.0
 * ================================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Preference;
using UnityEngine;
using UnityEngine.Audio;

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 默认音频设置实现，写入共享上下文并持久化
    /// <para>Default audio settings writing to the shared context and preferences</para>
    /// </summary>
    public sealed class DefaultAudioSettings : IAudioSettings
    {
        // 主混音器组名称
        private const string MasterGroupName = "Master";

        // BGM 混音器组名称
        private const string BgmGroupName = "BGM";

        // 音效混音器组名称
        private const string EffectGroupName = "Effect";

        // 静音状态偏好键，与历史版本一致以保证升级不丢设置
        private const string MuteKey = "Audio_Muted";

        // BGM 音量偏好键
        private const string BgmVolumeKey = "Audio_BGM_Volume";

        // 音效音量偏好键
        private const string EffectVolumeKey = "Audio_Effect_Volume";

        // 主音量偏好键
        private const string MasterVolumeKey = "Audio_Master_Volume";

        // 共享运行时上下文，由门面在 Initialize 时注入
        private AudioContext _context;

        /// <summary>
        /// 是否全局静音
        /// <para>Whether globally muted.</para>
        /// </summary>
        public bool IsMuted => _context is { IsMuted: true };

        /// <summary>
        /// 绑定共享上下文
        /// <para>Bind the shared context.</para>
        /// </summary>
        /// <param name="context">共享运行时上下文 <para>Shared runtime context</para></param>
        public void Initialize(AudioContext context) => _context = context;

        /// <summary>
        /// 解绑上下文
        /// <para>Release the context reference.</para>
        /// </summary>
        public void Dispose() => _context = null;

        #region 音量与静音

        /// <summary>
        /// 设置全局静音
        /// <para>Set global mute.</para>
        /// </summary>
        /// <param name="mute">是否静音 <para>Whether to mute</para></param>
        public void SetMute(bool mute)
        {
            if (_context == null) return;
            _context.IsMuted = mute;
            SetMixerVolume(_context.MasterGroup, mute ? -80f : ToDecibel(_context.MasterVolume));
            PreferenceManager.Instance.Set(MuteKey, mute);
            _context.NotifyMuteChanged();
        }

        /// <summary>
        /// 设置主音量
        /// <para>Set master volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        public void SetMasterVolume(float volume)
        {
            if (_context == null) return;
            volume = Mathf.Clamp01(volume);
            _context.MasterVolume = volume;
            SetMixerVolume(_context.MasterGroup,
                _context.IsMuted ? -80f : ToDecibel(volume));
            PreferenceManager.Instance.Set(MasterVolumeKey, volume);
        }

        /// <summary>
        /// 设置 BGM 音量
        /// <para>Set BGM volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        public void SetBgmVolume(float volume)
        {
            if (_context == null) return;
            volume = Mathf.Clamp01(volume);
            _context.BgmVolume = volume;
            SetMixerVolume(_context.BgmGroup, ToDecibel(volume));
            PreferenceManager.Instance.Set(BgmVolumeKey, volume);
        }

        /// <summary>
        /// 设置音效音量
        /// <para>Set effect volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        public void SetEffectVolume(float volume)
        {
            if (_context == null) return;
            volume = Mathf.Clamp01(volume);
            _context.EffectVolume = volume;
            SetMixerVolume(_context.EffectGroup, ToDecibel(volume));
            PreferenceManager.Instance.Set(EffectVolumeKey, volume);
        }

        /// <summary>
        /// 获取主音量
        /// <para>Get master volume</para>
        /// </summary>
        /// <returns>当前音量（0~1） <para>Current volume (0~1)</para></returns>
        public float GetMasterVolume() => _context?.MasterVolume ?? 1f;

        /// <summary>
        /// 获取 BGM 音量
        /// <para>Get BGM volume.</para>
        /// </summary>
        /// <returns>当前音量（0~1） <para>Current volume (0~1)</para></returns>
        public float GetBgmVolume() => _context?.BgmVolume ?? 1f;

        /// <summary>
        /// 获取音效音量
        /// <para>Get effect volume.</para>
        /// </summary>
        /// <returns>当前音量（0~1） <para>Current volume (0~1)</para></returns>
        public float GetEffectVolume() => _context?.EffectVolume ?? 1f;

        #endregion

        #region 混音器

        /// <summary>
        /// 替换主混音器
        /// <para>Replace the main AudioMixer</para>
        /// </summary>
        /// <param name="mixer">新的主混音器 <para>New main AudioMixer</para></param>
        public void SetAudioMixer(AudioMixer mixer)
        {
            if (_context == null || mixer == _context.Mixer) return;

            _context.Mixer = mixer;
            _context.MasterGroup = AudioContext.FindGroup(mixer, MasterGroupName, _context.Warn);
            _context.BgmGroup = AudioContext.FindGroup(mixer, BgmGroupName, _context.Warn);
            _context.EffectGroup = AudioContext.FindGroup(mixer, EffectGroupName, _context.Warn);

            // 新混音器不继承旧的分贝值，需按当前状态重设一次
            SetMixerVolume(_context.MasterGroup,
                _context.IsMuted ? -80f : ToDecibel(_context.MasterVolume));
            SetMixerVolume(_context.BgmGroup, ToDecibel(_context.BgmVolume));
            SetMixerVolume(_context.EffectGroup, ToDecibel(_context.EffectVolume));

            _context.NotifyEffectGroupChanged();
        }

        // 设置混音器组音量（分贝）
        private void SetMixerVolume(AudioMixerGroup group, float db)
        {
            if (_context != null && _context.Mixer && group)
                _context.Mixer.SetFloat(group.name + "Volume", db);
        }

        // 线性值转分贝
        private static float ToDecibel(float linear)
        {
            return linear <= 0.0001f ? -80f : 20f * Mathf.Log10(linear);
        }

        #endregion

        #region 持久化

        /// <summary>
        /// 从偏好存储恢复设置
        /// <para>Restore settings from preferences.</para>
        /// </summary>
        public void Load()
        {
            if (_context == null) return;
            var preference = PreferenceManager.Instance;
            float master = preference.Get(MasterVolumeKey, 1f);
            float bgm = preference.Get(BgmVolumeKey, _context.DefaultBgmVolume);
            float effect = preference.Get(EffectVolumeKey, _context.DefaultEffectVolume);
            bool muted = preference.Get(MuteKey, false);

            SetMasterVolume(master);
            SetBgmVolume(bgm);
            SetEffectVolume(effect);
            if (muted) SetMute(true);
        }

        /// <summary>
        /// 保存设置到偏好存储
        /// <para>Persist settings to preferences.</para>
        /// </summary>
        public void Save()
        {
            if (_context == null) return;
            var preference = PreferenceManager.Instance;
            preference.Set(MasterVolumeKey, _context.MasterVolume);
            preference.Set(BgmVolumeKey, _context.BgmVolume);
            preference.Set(EffectVolumeKey, _context.EffectVolume);
            preference.Set(MuteKey, _context.IsMuted);
            preference.Flush();
        }

        #endregion

        #region 淡入淡出

        /// <summary>
        /// BGM 淡入到目标音量
        /// <para>Fade BGM to a target volume.</para>
        /// </summary>
        /// <param name="targetVolume">目标音量 <para>Target volume</para></param>
        /// <param name="duration">淡变时长（秒） <para>Fade duration in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        public async UniTask FadeBGM(float targetVolume, float duration, CancellationToken token)
        {
            if (_context == null || !_context.BgmSource) return;
            float start = _context.BgmSource.volume;
            await FadeVolume(_context.BgmSource, start, targetVolume, duration, token);
        }

        /// <summary>
        /// BGM 淡出并停止
        /// <para>Fade out BGM and stop.</para>
        /// </summary>
        /// <param name="duration">淡变时长（秒） <para>Fade duration in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        public async UniTask FadeOutBGM(float duration, CancellationToken token)
        {
            if (_context == null || !_context.BgmSource) return;
            await FadeVolume(_context.BgmSource, _context.BgmSource.volume, 0f, duration, token);
            _context.BgmSource.Stop();
            _context.BgmSource.volume = 0f;
        }

        /// <summary>
        /// 淡入淡出 AudioSource 音量
        /// <para>Fade an AudioSource volume</para>
        /// </summary>
        /// <param name="source">目标播放器 <para>Target AudioSource</para></param>
        /// <param name="from">起始音量 <para>Starting volume</para></param>
        /// <param name="to">目标音量 <para>Target volume</para></param>
        /// <param name="duration">淡变时长（秒） <para>Fade duration in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        public async UniTask FadeVolume(AudioSource source, float from, float to, float duration,
            CancellationToken token)
        {
            if (!source) return;

            if (duration <= 0f)
            {
                source.volume = to;
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(from, to, elapsed / duration);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            source.volume = to;
        }

        #endregion
    }
}