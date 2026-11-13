/*
 * ================================================
 * Describe:      默认音频加载器：按名走 AssetsManager，按 URL 走 UnityWebRequestMultimedia。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 11:20
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 11:20
 * ScriptVersion: 1.0
 * ================================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Assets;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 默认音频加载器，按名走 AssetsManager、按 URL 走网络
    /// <para>Default clip loader using AssetsManager by key and network by URL</para>
    /// </summary>
    public sealed class DefaultAudioClipLoader : IAudioClipLoader
    {
        // 共享运行时上下文，仅用于输出加载警告
        private AudioContext _context;

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

        /// <summary>
        /// 按资源名加载音频
        /// <para>Load an audio clip by asset key</para>
        /// </summary>
        /// <param name="key">资源键 <para>Asset key</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <returns>可播放的 AudioClip；失败为 null <para>Playable AudioClip, or null on failure</para></returns>
        public UniTask<AudioClip> LoadAsync(string key, CancellationToken token)
        {
            string path = AssetsManager.Instance.CurrentSystemType == AssetsSystemType.Default
                ? $"{EFC.Projects.AppConst.AudioPath}{key}"
                : key;
            return UniTask.FromResult(AssetsManager.Instance.Load<AudioClip>(path));
        }

        /// <summary>
        /// 按 URL 下载并解码音频
        /// <para>Download and decode an audio clip by URL</para>
        /// </summary>
        /// <param name="url">网络音频直链 <para>Remote audio URL</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <returns>可播放的 AudioClip；失败为 null <para>Playable AudioClip, or null on failure</para></returns>
        public async UniTask<AudioClip> LoadFromUrlAsync(string url, CancellationToken token)
        {
            using (var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                try
                {
                    await request.SendWebRequest().WithCancellation(token);
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        _context?.Warn?.Invoke($"网络音频加载失败 url={url} error={request.error}");
                        return null;
                    }

                    return DownloadHandlerAudioClip.GetContent(request);
                }
                catch (System.OperationCanceledException)
                {
                    return null;
                }
                catch (UnityWebRequestException e)
                {
                    _context?.Warn?.Invoke($"网络音频异常 url={url} error={e.Message}");
                    return null;
                }
            }
        }
    }
}