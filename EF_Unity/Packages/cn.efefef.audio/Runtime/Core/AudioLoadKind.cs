/*
 * ================================================
 * Describe:      音频加载来源类型，用于门面编排与缓存实现选择下游加载路径。
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
    /// 音频加载来源类型
    /// <para>Audio load source kind</para>
    /// </summary>
    public enum AudioLoadKind
    {
        /// <summary>
        /// 按资源名 / 资源地址加载
        /// <para>Load by asset name or address</para>
        /// </summary>
        Asset = 0,

        /// <summary>
        /// 按 URL 直链加载
        /// <para>Load from a URL</para>
        /// </summary>
        Url = 1
    }
}