/*
 * ================================================
 * Describe:      音频播放接口（③）：只接受 AudioClip，不认识加载器与缓存。
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

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 音频播放接口，只接受 AudioClip
    /// <para>Audio playback interface taking AudioClips only</para>
    /// </summary>
    public interface IAudioPlayer : IAudioService
    {
        #region BGM

        /// <summary>
        /// 播放 BGM
        /// <para>Play BGM.</para>
        /// </summary>
        /// <param name="clip">待播放的音频片段 <para>AudioClip to play</para></param>
        /// <param name="loop">是否循环 <para>Whether to loop</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        void PlayBGM(AudioClip clip, bool loop, float startTime);

        /// <summary>
        /// 停止 BGM
        /// <para>Stop BGM.</para>
        /// </summary>
        void StopBGM();

        /// <summary>
        /// 暂停 BGM
        /// <para>Pause BGM.</para>
        /// </summary>
        void PauseBGM();

        /// <summary>
        /// 恢复 BGM
        /// <para>Unpause BGM.</para>
        /// </summary>
        void UnPauseBGM();

        #endregion

        #region Effect

        /// <summary>
        /// 播放 2D 音效
        /// <para>Play a 2D effect</para>
        /// </summary>
        /// <param name="clip">待播放的音频片段 <para>AudioClip to play</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>音效 ID；失败或取消为 0 <para>Effect id, or 0 on failure/cancellation</para></returns>
        UniTask<uint> Play2DEffect(AudioClip clip, CancellationToken token, float startTime);

        /// <summary>
        /// 播放 3D 音效
        /// <para>Play a 3D effect</para>
        /// </summary>
        /// <param name="clip">待播放的音频片段 <para>AudioClip to play</para></param>
        /// <param name="worldPos">世界坐标 <para>World position</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>音效 ID；失败或取消为 0 <para>Effect id, or 0 on failure/cancellation</para></returns>
        UniTask<uint> Play3DEffect(AudioClip clip, Vector3 worldPos, CancellationToken token, float startTime);

        /// <summary>
        /// 播放循环音效
        /// <para>Play a looping effect</para>
        /// </summary>
        /// <param name="clip">待播放的音频片段 <para>AudioClip to play</para></param>
        /// <param name="is3D">是否按 3D 空间播放 <para>Whether to play in 3D space</para></param>
        /// <param name="pos">世界坐标 <para>World position</para></param>
        /// <param name="startTime">起始播放时间（秒） <para>Start time in seconds</para></param>
        /// <returns>循环音效播放器；失败为 null <para>Looping AudioSource, or null on failure</para></returns>
        AudioSource PlayLoopEffect(AudioClip clip, bool is3D, Vector3 pos, float startTime);

        /// <summary>
        /// 按 ID 停止音效
        /// <para>Stop an effect by id.</para>
        /// </summary>
        /// <param name="id">音效 ID <para>Effect id</para></param>
        void StopEffect(uint id);

        /// <summary>
        /// 按 ID 暂停音效
        /// <para>Pause an effect by id.</para>
        /// </summary>
        /// <param name="id">音效 ID <para>Effect id</para></param>
        void PauseEffect(uint id);

        /// <summary>
        /// 按 ID 恢复音效
        /// <para>Unpause an effect by id.</para>
        /// </summary>
        /// <param name="id">音效 ID <para>Effect id</para></param>
        UniTask UnPauseEffect(uint id);

        /// <summary>
        /// 暂停所有音效
        /// <para>Pause all effects.</para>
        /// </summary>
        void PauseAllEffect();

        /// <summary>
        /// 恢复所有被暂停的音效
        /// <para>Unpause all paused effects.</para>
        /// </summary>
        UniTask UnPauseAllEffect();

        /// <summary>
        /// 停止所有音效
        /// <para>Stop all effects</para>
        /// </summary>
        void StopAllEffects();

        /// <summary>
        /// 停止指定的循环音效
        /// <para>Stop the given looping effect.</para>
        /// </summary>
        /// <param name="source">目标播放器 <para>Target AudioSource</para></param>
        void StopLoopEffect(AudioSource source);

        #endregion

        #region 查询

        /// <summary>
        /// 指定名字的 BGM 是否正在播放
        /// <para>Whether the named BGM is playing.</para>
        /// </summary>
        /// <param name="clipName">资源名 <para>Asset name</para></param>
        /// <returns>是否处于目标状态 <para>Whether the condition holds</para></returns>
        bool IsPlayingBGM(string clipName);

        /// <summary>
        /// 指定名字的音效是否正在播放
        /// <para>Whether any effect with the given name is playing.</para>
        /// </summary>
        /// <param name="clipName">资源名 <para>Asset name</para></param>
        /// <returns>是否处于目标状态 <para>Whether the condition holds</para></returns>
        bool IsPlayingEffect(string clipName);

        /// <summary>
        /// 当前活跃音效数量
        /// <para>Count of active effects</para>
        /// </summary>
        int ActiveEffectCount { get; }

        /// <summary>
        /// BGM 播放器
        /// <para>Dedicated BGM AudioSource</para>
        /// </summary>
        AudioSource BgmSource { get; }

        #endregion
    }
}