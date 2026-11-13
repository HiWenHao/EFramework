/*
 * ================================================
 * Describe:      四类音频策略的一次性注入配置包。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 11:20
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 11:20
 * ScriptVersion: 1.0
 * ================================================
 */

namespace EasyFramework.Managers.Audio
{
    /// <summary>
    /// 四类音频服务的一次性注入配置包
    /// <para>Bundle for injecting all four audio services at once</para>
    /// </summary>
    public sealed class AudioServices
    {
        /// <summary>
        /// 音频加载实现
        /// <para>Clip loader</para>
        /// </summary>
        public IAudioClipLoader Loader;

        /// <summary>
        /// 音频缓存实现
        /// <para>Clip cache</para>
        /// </summary>
        public IAudioClipCache Cache;

        /// <summary>
        /// 音频播放实现
        /// <para>Audio player</para>
        /// </summary>
        public IAudioPlayer Player;

        /// <summary>
        /// 音频设置实现
        /// <para>Audio settings</para>
        /// </summary>
        public IAudioSettings Settings;
    }
}