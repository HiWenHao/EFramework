/*
 * ================================================
 * Describe:      默认 HTTP 设置：持有默认超时 / UA / 请求头 / 证书，Initialize 时写入上下文， setter 实时同步。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 14:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 14:40
 * ScriptVersion: 1.0
 * ================================================
 */

using System.Collections.Generic;
using UnityEngine.Networking;

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// 默认 HTTP 设置实现，持有跨切面默认值并写入共享上下文
    /// <para>Default HTTP settings holding cross-cutting defaults and pushing them into the shared context</para>
    /// </summary>
    public sealed class DefaultHttpSettings : IHttpSettings
    {
        #region 字段 / Fields

        // 默认超时阈值（秒）
        private int _defaultTimeout = 30;

        // 默认 User-Agent
        private string _defaultUserAgent = "EasyFramework/1.0 (Unity)";

        // 证书处理器
        private CertificateHandler _certificateHandler;

        // 共享运行时上下文，由门面在 Initialize 时注入
        private HttpContext _context;

        #endregion

        #region 属性 / Properties

        /// <summary>
        /// 默认超时阈值（秒）
        /// <para>Default timeout in seconds</para>
        /// </summary>
        public int DefaultTimeout
        {
            get => _defaultTimeout;
            set
            {
                _defaultTimeout = value;
                if (_context != null) _context.DefaultTimeout = value;
            }
        }

        /// <summary>
        /// 默认 User-Agent 请求头
        /// <para>Default User-Agent header</para>
        /// </summary>
        public string DefaultUserAgent
        {
            get => _defaultUserAgent;
            set
            {
                _defaultUserAgent = value;
                if (_context != null) _context.DefaultUserAgent = value;
            }
        }

        /// <summary>
        /// 默认请求头（设置实现是唯一真相源）
        /// <para>Default headers, with the settings implementation as the single source of truth</para>
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; private set; }

        /// <summary>
        /// 证书处理器
        /// <para>Certificate handler</para>
        /// </summary>
        public CertificateHandler CertificateHandler
        {
            get => _certificateHandler;
            set
            {
                _certificateHandler = value;
                if (_context != null) _context.CertificateHandler = value;
            }
        }

        #endregion

        #region 方法 / Methods

        // 把当前默认值写入上下文（请求头共享同一字典引用，运行时原地修改即时生效）
        private void PushToContext()
        {
            if (_context == null) return;
            _context.DefaultTimeout = _defaultTimeout;
            _context.DefaultUserAgent = _defaultUserAgent;
            _context.DefaultHeaders = DefaultHeaders;
            _context.CertificateHandler = _certificateHandler;
        }

        /// <summary>
        /// 绑定共享上下文并写入默认快照
        /// <para>Bind the shared context and push the default snapshot</para>
        /// </summary>
        /// <param name="context">共享运行时上下文 <para>Shared runtime context</para></param>
        public void Initialize(HttpContext context)
        {
            _context = context;
            DefaultHeaders = new Dictionary<string, string>();
            PushToContext();
        }

        /// <summary>
        /// 解绑上下文
        /// <para>Release the context reference.</para>
        /// </summary>
        public void Dispose()
        {
            _context = null;
            ClearDefaultHeaders();
            DefaultHeaders = null;
        }

        /// <summary>
        /// 设置单个默认请求头
        /// <para>Set a single default header</para>
        /// </summary>
        /// <param name="key">键 <para>Key</para></param>
        /// <param name="value">值 <para>Value</para></param>
        public void SetDefaultHeader(string key, string value) => DefaultHeaders[key] = value;

        /// <summary>
        /// 移除单个默认请求头
        /// <para>Remove a single default header</para>
        /// </summary>
        /// <param name="key">键 <para>Key</para></param>
        public void RemoveDefaultHeader(string key) => DefaultHeaders.Remove(key);

        /// <summary>
        /// 清空所有默认请求头
        /// <para>Clear all default headers</para>
        /// </summary>
        public void ClearDefaultHeaders() => DefaultHeaders.Clear();

        #endregion
    }
}
