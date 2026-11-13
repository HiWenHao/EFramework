/*
 * ================================================
 * Describe:      音频加载接口（①）：负责把资源键 / URL 变成可播放的 AudioClip，不含缓存与播放。
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
    /// 音频加载接口，按资源键或 URL 取出 AudioClip
    /// <para>Clip loader interface resolving an asset key or URL to an AudioClip</para>
    /// </summary>
    public interface IAudioClipLoader : IAudioService
    {
        /// <summary>
        /// 按资源名加载音频
        /// <para>Load an audio clip by asset key</para>
        /// </summary>
        /// <param name="key">资源键 <para>Asset key.</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token.</para></param>
        /// <returns>可播放的 AudioClip；失败为 null <para>Playable AudioClip, or null on failure.</para></returns>
        UniTask<AudioClip> LoadAsync(string key, CancellationToken token);

        /// <summary>
        /// 按 URL 下载并解码音频
        /// <para>Download and decode an audio clip by URL</para>
        /// </summary>
        /// <param name="url">网络音频直链 <para>Remote audio URL.</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token.</para></param>
        /// <returns>可播放的 AudioClip；失败为 null <para>Playable AudioClip, or null on failure.</para></returns>
        UniTask<AudioClip> LoadFromUrlAsync(string url, CancellationToken token);
    }
}