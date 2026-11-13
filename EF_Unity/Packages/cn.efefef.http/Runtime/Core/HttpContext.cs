/*
 * ================================================
 * Describe:      HTTP 服务共享运行时上下文。设置实现写、发送器读，避免接口之间互相持有引用。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 14:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 14:40
 * ScriptVersion: 1.0
 * ================================================
 */

using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// HTTP 服务共享上下文，存放跨切面默认值与共享状态，作为接口之间唯一的中介
    /// <para>Shared context holding cross-cutting defaults and shared state, the sole mediator between interfaces</para>
    /// </summary>
    public sealed class HttpContext
    {
        #region 门面序列化的默认快照（由设置实现在 Initialize 时写入） / Default Snapshot

        /// <summary>
        /// 默认超时阈值（秒）
        /// <para>Default timeout in seconds</para>
        /// </summary>
        public int DefaultTimeout = 30;

        /// <summary>
        /// 默认 User-Agent 请求头
        /// <para>Default User-Agent header</para>
        /// </summary>
        public string DefaultUserAgent = "EasyFramework/1.0 (Unity)";

        /// <summary>
        /// 默认请求头副本（设置实现写入，发送器只读）
        /// <para>Copy of default headers (written by settings, read by sender)</para>
        /// </summary>
        public Dictionary<string, string> DefaultHeaders;

        /// <summary>
        /// 证书处理器
        /// <para>Certificate handler</para>
        /// </summary>
        public CertificateHandler CertificateHandler;

        #endregion

        #region 执行策略（由门面解析后写入，发送器只读） / Execution Strategy

        /// <summary>
        /// 重试策略实例
        /// <para>Resolved retry policy instance</para>
        /// </summary>
        public IHttpRetryPolicy RetryPolicy;

        #endregion

        #region 门面注入的工具 / Injected Tools

        /// <summary>
        /// 统一警告入口（按 OpenDebug 过滤）
        /// <para>Shared warning sink</para>
        /// </summary>
        public Action<string> Warn;

        #endregion
    }
}