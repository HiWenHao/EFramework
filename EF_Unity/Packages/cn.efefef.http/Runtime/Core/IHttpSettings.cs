/*
 * ================================================
 * Describe:      HTTP 设置接口（④ 设置）：默认超时、UA、默认请求头、证书处理器等跨切面默认值。
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
    /// HTTP 设置接口，管理默认请求头、超时、UA 与证书处理器
    /// <para>HTTP settings interface for default headers, timeout, UA and certificate handler</para>
    /// </summary>
    public interface IHttpSettings : IHttpService
    {
        /// <summary>
        /// 默认超时阈值（秒）
        /// <para>Default timeout in seconds</para>
        /// </summary>
        int DefaultTimeout { get; set; }

        /// <summary>
        /// 默认 User-Agent 请求头
        /// <para>Default User-Agent header</para>
        /// </summary>
        string DefaultUserAgent { get; set; }

        /// <summary>
        /// 默认请求头，会合并进每一次请求（可被单次请求头覆盖）
        /// <para>Default headers merged into every request (overridable per-request)</para>
        /// </summary>
        Dictionary<string, string> DefaultHeaders { get; }

        /// <summary>
        /// 负责对 https 请求接收到的证书进行拒绝或接受处理
        /// <para>Responsible for rejecting or accepting certificates on https requests</para>
        /// </summary>
        CertificateHandler CertificateHandler { get; set; }

        /// <summary>
        /// 设置单个默认请求头
        /// <para>Set a single default header</para>
        /// </summary>
        /// <param name="key">键 <para>Key</para></param>
        /// <param name="value">值 <para>Value</para></param>
        void SetDefaultHeader(string key, string value);

        /// <summary>
        /// 移除单个默认请求头
        /// <para>Remove a single default header</para>
        /// </summary>
        /// <param name="key">键 <para>Key</para></param>
        void RemoveDefaultHeader(string key);

        /// <summary>
        /// 清空所有默认请求头
        /// <para>Clear all default headers</para>
        /// </summary>
        void ClearDefaultHeaders();
    }
}