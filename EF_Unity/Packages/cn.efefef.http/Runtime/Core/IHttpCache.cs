/*
 * ================================================
 * Describe:      HTTP 缓存接口（② 缓存）：包内无默认实现，由业务侧注入；未注入时门面直通发送器。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 14:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 14:40
 * ScriptVersion: 1.0
 * ================================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// HTTP 缓存接口，包住一次完整发送（厚加载装饰器）
    /// <para>HTTP cache interface wrapping a full send</para>
    /// </summary>
    public interface IHttpCache : IHttpService
    {
        /// <summary>
        /// 取出或发送响应
        /// <para>Get or send a response</para>
        /// </summary>
        /// <param name="key">缓存键（由请求计算得出） <para>Cache key derived from the request</para></param>
        /// <param name="sender">下游发送器 <para>Downstream sender supplied by the facade</para></param>
        /// <param name="request">原始请求 <para>Original request</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <returns>统一响应 <para>Unified response</para></returns>
        UniTask<HttpResponse> GetOrLoadAsync(string key, IHttpSender sender,
            HttpRequest request, CancellationToken token);

        /// <summary>
        /// 清空缓存（显式删除，区别于 Dispose 的释放）
        /// <para>Clear the cache (explicit delete, distinct from Dispose)</para>
        /// </summary>
        void Clear();
    }
}