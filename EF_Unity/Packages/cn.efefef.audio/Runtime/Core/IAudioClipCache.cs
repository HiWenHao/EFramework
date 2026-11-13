/*
 * ================================================
 * Describe:      音频缓存接口（②）：包内无默认实现，由业务侧注入；未注入时门面直通加载器。
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
    /// 音频缓存接口，包住一次完整加载
    /// <para>Clip cache interface wrapping a full load</para>
    /// </summary>
    public interface IAudioClipCache : IAudioService
    {
        /// <summary>
        /// 取出或加载可播放的 AudioClip
        /// <para>Get or load a playable AudioClip</para>
        /// </summary>
        /// <param name="key">原始资源键 / URL <para>Original asset key or URL.</para></param>
        /// <param name="loader">下游加载器 <para>Downstream loader supplied by the facade.</para></param>
        /// <param name="kind">加载来源类型 <para>Source kind selecting the loader path.</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token.</para></param>
        /// <returns>可播放的 AudioClip；失败为 null <para>Playable AudioClip, or null on failure.</para></returns>
        UniTask<AudioClip> GetOrLoadAsync(string key, IAudioClipLoader loader,
            AudioLoadKind kind, CancellationToken token);

        /// <summary>
        /// 清空缓存
        /// <para>Clear the cache</para>
        /// </summary>
        void Clear();
    }
}
