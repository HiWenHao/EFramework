/*
 * ================================================
 * Describe:      可选的内存缓存示例（非默认）。业务可注入它，也可自行实现磁盘/持久化缓存。门面默认不缓存。
 *                注意：仅对 Result == Text 的响应生效（纹理/资源包/文件不进缓存）。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 14:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 14:40
 * ScriptVersion: 1.0
 * ================================================
 */

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// 可选的内存缓存实现，供业务按需注入。门面默认不启用缓存（Cache = null 即直通）。
    /// <para>Optional in-memory cache for business injection. The facade defaults to no cache (Cache = null = passthrough).</para>
    /// </summary>
    public sealed class MemoryHttpCache : IHttpCache
    {
        #region 字段 / Fields

        // 共享运行时上下文
        private HttpContext _context;

        // 响应存储；命中直接返回，避免重复网络请求
        private Dictionary<string, HttpResponse> _store;

        #endregion

        #region 方法 / Methods

        /// <summary>
        /// 绑定共享上下文
        /// <para>Bind the shared context.</para>
        /// </summary>
        /// <param name="context">共享运行时上下文 <para>Shared runtime context</para></param>
        public void Initialize(HttpContext context)
        {
            _context = context;
            _store = new Dictionary<string, HttpResponse>();
        }

        /// <summary>
        /// 解绑上下文并清空缓存（仅释放运行时引用，不删除任何外部持久化数据）
        /// <para>Release context and clear the store (releases runtime references only)</para>
        /// </summary>
        public void Dispose()
        {
            _context = null;
            Clear();
            _store = null;
        }

        /// <summary>
        /// 取出或发送响应：命中返回缓存，未命中才调用下游发送器并缓存成功结果
        /// <para>Get or send: return cache on hit, otherwise call the downstream sender and cache successful results</para>
        /// </summary>
        /// <param name="key">缓存键 <para>Cache key</para></param>
        /// <param name="sender">下游发送器 <para>Downstream sender</para></param>
        /// <param name="request">原始请求 <para>Original request</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <returns>统一响应 <para>Unified response</para></returns>
        public async UniTask<HttpResponse> GetOrLoadAsync(string key, IHttpSender sender,
            HttpRequest request, CancellationToken token)
        {
            if (_store.TryGetValue(key, out var cached))
                return cached;

            var fresh = await sender.SendAsync(request, token);
            if (fresh.IsSuccess)
                _store[key] = fresh;

            return fresh;
        }

        /// <summary>
        /// 清空缓存（显式删除）
        /// <para>Clear the cache (explicit delete)</para>
        /// </summary>
        public void Clear() => _store.Clear();

        #endregion
    }
}