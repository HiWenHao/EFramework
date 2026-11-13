/*
 * ================================================
 * Describe:      HTTP 请求描述模型。门面所有公开方法都先构建本对象再交给编排器，保持单一入口。
 * Author:        Alvin8412
 * CreationTime:  2026-11-13 14:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-13 14:40
 * ScriptVersion: 1.0
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// HTTP 请求描述，作为门面与发送器之间的统一契约
    /// <para>HTTP request description, the unified contract between facade and sender</para>
    /// </summary>
    public sealed class HttpRequest
    {
        #region 枚举 / Enums

        /// <summary>
        /// 响应解析类型，决定发送器使用哪种 <see cref="DownloadHandler"/>
        /// <para>Response kind, decides which <see cref="DownloadHandler"/> the sender uses</para>
        /// </summary>
        public enum ResultKind
        {
            /// <summary>
            /// 文本 / 字节（缓冲下载，可参与缓存）
            /// <para>Text / bytes (buffered, cacheable)</para>
            /// </summary>
            Text = 0,

            /// <summary>
            /// 纹理（<see cref="DownloadHandlerTexture"/>）
            /// <para>Texture (<see cref="DownloadHandlerTexture"/>)</para>
            /// </summary>
            Texture = 1,

            /// <summary>
            /// 资源包（<see cref="DownloadHandlerAssetBundle"/>）
            /// <para>AssetBundle (<see cref="DownloadHandlerAssetBundle"/>)</para>
            /// </summary>
            AssetBundle = 2,

            /// <summary>
            /// 下载到本地文件（<see cref="DownloadHandlerFile"/>）
            /// <para>Download to a local file (<see cref="DownloadHandlerFile"/>)</para>
            /// </summary>
            File = 3
        }

        #endregion

        #region 字段 / Fields

        /// <summary>
        /// 链接地址
        /// <para>Request URL</para>
        /// </summary>
        public string Url;

        /// <summary>
        /// 请求方法（GET / POST / PUT / DELETE）
        /// <para>HTTP method</para>
        /// </summary>
        public string Method = "GET";

        /// <summary>
        /// 请求体（POST / PUT 的字节内容）
        /// <para>Request body (raw bytes for POST / PUT)</para>
        /// </summary>
        public byte[] Body;

        /// <summary>
        /// 请求体内容类型（如 application/json）
        /// <para>Body content type (e.g. application/json)</para>
        /// </summary>
        public string ContentType;

        /// <summary>
        /// 单次请求头，覆盖默认请求头
        /// <para>Per-request headers, overriding defaults</para>
        /// </summary>
        public Dictionary<string, string> Headers;

        /// <summary>
        /// 多部份表单分段；若设置，发送器走 <see cref="UnityWebRequest"/>.Post 路径
        /// <para>Multipart form sections; when set the sender uses <see cref="UnityWebRequest"/>.Post</para>
        /// </summary>
        public List<IMultipartFormSection> FormSections;

        /// <summary>
        /// 超时阈值覆盖（秒）；null 使用默认
        /// <para>Timeout override in seconds; null uses the default</para>
        /// </summary>
        public int? TimeoutOverride;

        /// <summary>
        /// 响应解析类型
        /// <para>Response kind</para>
        /// </summary>
        public ResultKind Result = ResultKind.Text;

        /// <summary>
        /// 下载到本地文件的保存路径（Result == File 时生效）
        /// <para>Save path for file download (used when Result == File)</para>
        /// </summary>
        public string SavePath;

        /// <summary>
        /// 下载进度回调
        /// <para>Download progress callback</para>
        /// </summary>
        public IProgress<float> Progress;

        /// <summary>
        /// 是否可被缓存（仅 Result == Text 且本标志为 true 时门面才走缓存）
        /// <para>Whether the request may be cached (facade routes to cache only when Result == Text and this is true)</para>
        /// </summary>
        public bool Cacheable = true;

        /// <summary>
        /// 缓存键：方法 + 地址 + 有序请求头 + 请求体哈希，稳定且唯一
        /// <para>Cache key: method + url + sorted headers + body hash, stable and unique</para>
        /// </summary>
        public string CacheKey
        {
            get
            {
                var sb = new StringBuilder();
                sb.Append(Method).Append(' ').Append(Url);

                if (Headers is { Count: > 0 })
                {
                    var keys = new List<string>(Headers.Keys);
                    keys.Sort(System.StringComparer.Ordinal);
                    foreach (var key in keys)
                        sb.Append('&').Append(key).Append('=').Append(Headers[key]);
                }

                if (Body == null) return sb.ToString();

                const uint offset = 2166136261u;
                const uint prime = 16777619u;
                uint hash = offset;
                foreach (byte b in Body)
                {
                    hash ^= b;
                    hash *= prime;
                }

                sb.Append('#').Append(hash.ToString("X8"));

                return sb.ToString();
            }
        }

        #endregion
    }
}