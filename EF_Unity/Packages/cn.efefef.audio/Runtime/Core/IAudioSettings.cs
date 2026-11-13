/*
 * ================================================
 * Describe:      音频设置接口（④）：音量、静音、混音器、偏好持久化与淡入淡出。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 11:20
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 11:20
 * ScriptVersion: 1.0
 * ================================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 音频设置接口，管理音量、静音与混音器
    /// <para>Audio settings interface for volume, mute and mixer</para>
    /// </summary>
    public interface IAudioSettings : IAudioService
    {
        /// <summary>
        /// 是否全局静音
        /// <para>Whether globally muted.</para>
        /// </summary>
        bool IsMuted { get; }

        /// <summary>
        /// 设置全局静音
        /// <para>Set global mute.</para>
        /// </summary>
        /// <param name="mute">是否静音 <para>Whether to mute</para></param>
        void SetMute(bool mute);

        /// <summary>
        /// 设置主音量
        /// <para>Set master volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        void SetMasterVolume(float volume);

        /// <summary>
        /// 设置 BGM 音量
        /// <para>Set BGM volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        void SetBgmVolume(float volume);

        /// <summary>
        /// 设置音效音量
        /// <para>Set effect volume</para>
        /// </summary>
        /// <param name="volume">音量（0~1） <para>Volume (0~1)</para></param>
        void SetEffectVolume(float volume);

        /// <summary>
        /// 获取主音量
        /// <para>Get master volume</para>
        /// </summary>
        /// <returns>当前音量（0~1） <para>Current volume (0~1)</para></returns>
        float GetMasterVolume();

        /// <summary>
        /// 获取 BGM 音量
        /// <para>Get BGM volume.</para>
        /// </summary>
        /// <returns>当前音量（0~1） <para>Current volume (0~1)</para></returns>
        float GetBgmVolume();

        /// <summary>
        /// 获取音效音量
        /// <para>Get effect volume.</para>
        /// </summary>
        /// <returns>当前音量（0~1） <para>Current volume (0~1)</para></returns>
        float GetEffectVolume();

        /// <summary>
        /// 替换主混音器
        /// <para>Replace the main AudioMixer</para>
        /// </summary>
        /// <param name="mixer">新的主混音器 <para>New main AudioMixer</para></param>
        void SetAudioMixer(AudioMixer mixer);

        /// <summary>
        /// 从偏好存储恢复设置
        /// <para>Restore settings from preferences.</para>
        /// </summary>
        void Load();

        /// <summary>
        /// 保存设置到偏好存储
        /// <para>Persist settings to preferences.</para>
        /// </summary>
        void Save();

        /// <summary>
        /// BGM 淡入到目标音量
        /// <para>Fade BGM to a target volume.</para>
        /// </summary>
        /// <param name="targetVolume">目标音量 <para>Target volume</para></param>
        /// <param name="duration">淡变时长（秒） <para>Fade duration in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        UniTask FadeBGM(float targetVolume, float duration, CancellationToken token);

        /// <summary>
        /// BGM 淡出并停止
        /// <para>Fade out BGM and stop.</para>
        /// </summary>
        /// <param name="duration">淡变时长（秒） <para>Fade duration in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        UniTask FadeOutBGM(float duration, CancellationToken token);

        /// <summary>
        /// 淡入淡出 AudioSource 音量
        /// <para>Fade an AudioSource volume</para>
        /// </summary>
        /// <param name="source">目标播放器 <para>Target AudioSource</para></param>
        /// <param name="from">起始音量 <para>Starting volume</para></param>
        /// <param name="to">目标音量 <para>Target volume</para></param>
        /// <param name="duration">淡变时长（秒） <para>Fade duration in seconds</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        UniTask FadeVolume(AudioSource source, float from, float to, float duration, CancellationToken token);
    }
}