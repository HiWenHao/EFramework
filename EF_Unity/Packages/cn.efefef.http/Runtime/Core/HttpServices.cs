/*
 * ================================================
 * Describe:      HTTP 四类服务的一次性注入配置包。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 14:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 14:40
 * ScriptVersion: 1.0
 * ================================================
 */

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// HTTP 四类服务的一次性注入配置包
    /// <para>Bundle for injecting all HTTP services at once</para>
    /// </summary>
    public sealed class HttpServices
    {
        /// <summary>
        /// 发送实现
        /// <para>HTTP sender</para>
        /// </summary>
        public IHttpSender Sender;

        /// <summary>
        /// 缓存实现（null = 不缓存 / 直通）
        /// <para>HTTP cache (null = no cache / passthrough)</para>
        /// </summary>
        public IHttpCache Cache;

        /// <summary>
        /// 重试策略
        /// <para>Retry policy</para>
        /// </summary>
        public IHttpRetryPolicy RetryPolicy;

        /// <summary>
        /// 设置实现
        /// <para>HTTP settings</para>
        /// </summary>
        public IHttpSettings Settings;
    }
}