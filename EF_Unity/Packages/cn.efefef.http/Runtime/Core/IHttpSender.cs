/*
 * ================================================
 * Describe:      HTTP 发送接口（① 执行/传输）：只负责把 HttpRequest 变成 HttpResponse，不认识缓存与设置。
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
    /// HTTP 发送接口，执行一次真实的网络请求
    /// <para>HTTP sender interface executing a real network request</para>
    /// </summary>
    public interface IHttpSender : IHttpService
    {
        /// <summary>
        /// 发送请求并返回统一响应
        /// <para>Send the request and return the unified response</para>
        /// </summary>
        /// <param name="request">请求描述 <para>Request description</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <returns>统一响应 <para>Unified response</para></returns>
        UniTask<HttpResponse> SendAsync(HttpRequest request, CancellationToken token);
    }
}