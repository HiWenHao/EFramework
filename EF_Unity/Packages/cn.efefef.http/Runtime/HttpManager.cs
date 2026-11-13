/*
 * ================================================
 * Describe:        HTTP 请求门面：唯一对外入口，编排「缓存 → 发送」并转发框架生命周期。
 *                  发送 / 缓存 / 重试 / 设置由可替换接口提供默认实现；门面只编排不实现细节。
 * Author:          Alvin8412
 * CreationTime:    2026-04-02 21:36:59
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-13 15:38:54
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// HTTP 管理器门面，编排缓存与发送并对外提供统一入口
    /// <para>HTTP facade orchestrating cache and send behind one entry point</para>
    /// </summary>
    [Manager(Order = 89100)]
    public sealed class HttpManager : MonoSingleton<HttpManager>, ISingleton
    {
        #region 向后兼容便捷属性（路由内部服务，勿新增序列化锚点） / Backward-Compatible Properties

        /// <summary>
        /// 默认超时阈值（秒），路由到设置服务
        /// <para>Default timeout in seconds, routed to the settings service</para>
        /// </summary>
        public int DefaultTimeout
        {
            get => _settings?.DefaultTimeout ?? 30;
            set
            {
                if (_settings != null) _settings.DefaultTimeout = value;
            }
        }

        /// <summary>
        /// 默认 User-Agent 请求头，路由到设置服务
        /// <para>Default User-Agent header, routed to the settings service</para>
        /// </summary>
        public string DefaultUserAgent
        {
            get => _settings?.DefaultUserAgent ?? "EasyFramework/1.0 (Unity)";
            set
            {
                if (_settings != null) _settings.DefaultUserAgent = value;
            }
        }

        /// <summary>
        /// 默认请求头（运行时原地修改即时生效），路由到设置服务
        /// <para>Default headers (in-place edits apply immediately), routed to the settings service</para>
        /// </summary>
        public Dictionary<string, string> DefaultHeaders => _settings?.DefaultHeaders;

        /// <summary>
        /// 证书处理器，路由到设置服务
        /// <para>Certificate handler, routed to the settings service</para>
        /// </summary>
        public CertificateHandler CertificateHandler
        {
            get => _settings?.CertificateHandler;
            set
            {
                if (_settings != null) _settings.CertificateHandler = value;
            }
        }

        /// <summary>
        /// 重试策略（具体类型，便于旧代码直接访问字段）；设为 null 恢复默认
        /// <para>Retry policy (concrete type for legacy access); null restores the default</para>
        /// </summary>
        public HttpRetryPolicy RetryPolicy
        {
            get => _retry as HttpRetryPolicy;
            set => SetRetryPolicy(value);
        }

        /// <summary>
        /// 开启调试日志
        /// <para>Enable debug log</para>
        /// </summary>
        public bool OpenDebug { get; set; }

        #endregion

        #region 四类服务实例 / Service Instances

        // 共享运行时上下文，由 Init 创建、Quit 置空
        private HttpContext _context;

        // 当前发送实现，默认 DefaultHttpSender
        private IHttpSender _sender;

        // 当前缓存实现，null 表示不缓存（包内无默认实现）
        private IHttpCache _cache;

        // 当前重试策略，默认 HttpRetryPolicy
        private IHttpRetryPolicy _retry;

        // 当前设置实现，默认 DefaultHttpSettings
        private IHttpSettings _settings;

        // Init 之前到达的注入请求，Init 完成后补应用
        private HttpServices _pending;

        /// <summary>
        /// 当前生效的发送实现
        /// <para>Active sender</para>
        /// </summary>
        public IHttpSender CurrentSender => _sender;

        /// <summary>
        /// 当前生效的缓存实现（null = 不缓存）
        /// <para>Active cache (null = no cache)</para>
        /// </summary>
        public IHttpCache CurrentCache => _cache;

        /// <summary>
        /// 当前生效的重试策略
        /// <para>Active retry policy</para>
        /// </summary>
        public IHttpRetryPolicy CurrentRetryPolicy => _retry;

        /// <summary>
        /// 当前生效的设置实现
        /// <para>Active settings</para>
        /// </summary>
        public IHttpSettings CurrentSettings => _settings;

        #endregion

        #region 框架生命周期 / Framework Lifecycle

        // 框架初始化：构建共享上下文并装配四类服务
        void ISingleton.Init()
        {
            _context = new HttpContext { Warn = Warning };

            if (_pending != null)
            {
                var pending = _pending;
                _pending = null;
                Configure(pending);
            }
            else
                ResetConfigureToDefaults();
        }

        // 框架退出：逆序释放服务并清空引用，避免 HybridCLR 域重载残留热更侧实例
        void ISingleton.Quit()
        {
            _settings?.Dispose();
            _cache?.Dispose();
            _sender?.Dispose();
            _retry = null;
            _context = null;
            _pending = null;
            _sender = null;
            _cache = null;
            _settings = null;
        }

        // 统一警告入口，按 OpenDebug 过滤
        private void Warning(string message)
        {
            if (OpenDebug)
                D.Warning($"[ HttpManager ] {message}");
        }

        #endregion

        #region 服务注入（四类接口均可单独或整体替换） / Service Injection

        // Init 之前到达的单次替换请求合并进配置包，Init 完成时统一应用
        private HttpServices EnsurePending() => _pending ??= new HttpServices();

        // 局部替换发送器
        private void SwapSender(IHttpSender next)
        {
            IHttpSender resolved = next ?? new DefaultHttpSender();
            if (ReferenceEquals(resolved, _sender) && _sender != null) return;
            _sender?.Dispose();
            _sender = resolved;
            _sender.Initialize(_context);
        }

        // 局部替换缓存实现；null 表示关闭缓存（直通发送器）
        private void SwapCache(IHttpCache next)
        {
            if (ReferenceEquals(next, _cache)) return;
            _cache?.Dispose();
            _cache = next; // null = 不缓存
            _cache?.Initialize(_context);
        }

        // 局部替换重试策略并写入上下文
        private void SwapRetry(IHttpRetryPolicy next)
        {
            _retry = next ?? new HttpRetryPolicy();
            if (_context != null) _context.RetryPolicy = _retry;
        }

        // 局部替换设置实现
        private void SwapSettings(IHttpSettings next)
        {
            IHttpSettings resolved = next ?? new DefaultHttpSettings();
            if (ReferenceEquals(resolved, _settings) && _settings != null) return;
            _settings?.Dispose();
            _settings = resolved;
            _settings.Initialize(_context);
        }

        /// <summary>
        /// 一次性配置四类服务
        /// <para>Configure all four HTTP services at once</para>
        /// </summary>
        /// <param name="services">四类服务配置包，如果为空则会回归到默认实现 <para>Services bundle, If it is empty, it will fall back to the default implementation.</para></param>
        public void Configure(HttpServices services)
        {
            if (_context == null)
            {
                _pending = services;
                return;
            }

            if (services == null)
            {
                ResetConfigureToDefaults();
                return;
            }

            SwapSender(services.Sender);
            SwapCache(services.Cache);
            SwapRetry(services.RetryPolicy);
            SwapSettings(services.Settings);
        }

        /// <summary>
        /// 恢复全部默认实现
        /// <para>Restore all package defaults</para>
        /// </summary>
        public void ResetConfigureToDefaults()
        {
            if (_context == null) return;
            SwapSender(null);
            SwapCache(null);
            SwapRetry(null);
            SwapSettings(null);
        }

        /// <summary>
        /// 替换发送实现（null = 默认 <see cref="DefaultHttpSender"/>）
        /// <para>Replace the sender (null = default <see cref="DefaultHttpSender"/>)</para>
        /// </summary>
        /// <param name="sender">下游发送器 <para>Downstream sender</para></param>
        public void SetSender(IHttpSender sender)
        {
            if (_context == null)
            {
                EnsurePending().Sender = sender;
                return;
            }

            SwapSender(sender);
        }

        /// <summary>
        /// 替换缓存实现（null = 不缓存 / 直通，不会删除任何外部持久化数据），当然你也可以使用<see cref="MemoryHttpCache"/>
        /// <para>Replace the cache (null = no cache / passthrough, never deletes external data). Of course, you can also use <see cref="MemoryHttpCache"/></para>
        /// </summary>
        /// <param name="cache">缓存实现 <para>Cache implementation</para></param>
        public void SetCache(IHttpCache cache)
        {
            if (_context == null)
            {
                EnsurePending().Cache = cache;
                return;
            }

            SwapCache(cache);
        }

        /// <summary>
        /// 替换重试策略（null = 默认 <see cref="HttpRetryPolicy"/>）
        /// <para>Replace the retry policy (null = default <see cref="HttpRetryPolicy"/>)</para>
        /// </summary>
        /// <param name="retry">重试策略 <para>Retry policy</para></param>
        public void SetRetryPolicy(IHttpRetryPolicy retry)
        {
            if (_context == null)
            {
                EnsurePending().RetryPolicy = retry;
                return;
            }

            SwapRetry(retry);
        }

        /// <summary>
        /// 替换设置实现（null = 默认 <see cref="DefaultHttpSettings"/>）
        /// <para>Replace the settings (null = default <see cref="DefaultHttpSettings"/>)</para>
        /// </summary>
        /// <param name="settings">设置实现 <para>Settings implementation</para></param>
        public void SetSettings(IHttpSettings settings)
        {
            if (_context == null)
            {
                EnsurePending().Settings = settings;
                return;
            }

            SwapSettings(settings);
        }

        #endregion

        #region 编排（门面独有职责） / Orchestration

        // 门面唯一知道「缓存 + 发送」组合顺序的地方
        private UniTask<HttpResponse> ResolveAsync(HttpRequest request, CancellationToken token)
        {
            if (_sender == null)
                return UniTask.FromResult(new HttpResponse { IsSuccess = false, Error = "HttpManager 尚未初始化" });

            // 仅文本结果参与缓存；纹理 / 资源包 / 文件不进缓存
            if (_cache != null && request.Result == HttpRequest.ResultKind.Text && request.Cacheable)
                return _cache.GetOrLoadAsync(request.CacheKey, _sender, request, token);

            return _sender.SendAsync(request, token);
        }

        // 失败时抛出与旧版一致的异常，保持调用方行为
        private static void ThrowIfFailed(HttpResponse response, string url)
        {
            if (!response.IsSuccess)
                throw new HttpRequestException($"HTTP Error: {response.Error} (URL: {url})");
        }

        #endregion

        #region GET 请求 / GET Requests

        /// <summary>
        /// 异步 Get 请求，返回文本
        /// <para>Async GET request returning text</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="headers">附加请求头，可空<para>Extra headers, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>响应文本<para>Response text</para></returns>
        public async UniTask<string> GetStringAsync(string url, Dictionary<string, string> headers = null,
            int? timeout = null, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequest
            {
                Url = url,
                Method = UnityWebRequest.kHttpVerbGET,
                Headers = headers,
                TimeoutOverride = timeout,
                Result = HttpRequest.ResultKind.Text
            };

            var response = await ResolveAsync(request, cancellationToken);
            ThrowIfFailed(response, url);
            return response.Text;
        }

        /// <summary>
        /// 异步 Get 泛型请求
        /// <para>Async GET generic request</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="headers">附加请求头，可空<para>Extra headers, nullable</para></param>
        /// <param name="serializerSettings">JSON 序列化设置，可空<para>JSON serializer settings, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>反序列化后的对象<para>Deserialized object of type T</para></returns>
        public async UniTask<T> GetAsync<T>(string url, Dictionary<string, string> headers = null,
            JsonSerializerSettings serializerSettings = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            string json = await GetStringAsync(url, headers, timeout, cancellationToken);
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
        }

        /// <summary>
        /// 异步 Get 纹理请求
        /// <para>Async GET texture request</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="headers">附加请求头，可空<para>Extra headers, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>下载的纹理<para>Downloaded texture</para></returns>
        public async UniTask<Texture2D> GetTextureAsync(string url, Dictionary<string, string> headers = null,
            int? timeout = null, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequest
            {
                Url = url,
                Method = UnityWebRequest.kHttpVerbGET,
                Headers = headers,
                TimeoutOverride = timeout,
                Result = HttpRequest.ResultKind.Texture
            };

            var response = await ResolveAsync(request, cancellationToken);
            ThrowIfFailed(response, url);
            return response.Texture;
        }

        /// <summary>
        /// 异步 Get 资源包请求
        /// <para>Async GET asset bundle request</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="headers">附加请求头，可空<para>Extra headers, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>下载的资源包<para>Downloaded asset bundle</para></returns>
        public async UniTask<AssetBundle> GetAssetBundleAsync(string url, Dictionary<string, string> headers = null,
            int? timeout = null, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequest
            {
                Url = url,
                Method = UnityWebRequest.kHttpVerbGET,
                Headers = headers,
                TimeoutOverride = timeout,
                Result = HttpRequest.ResultKind.AssetBundle
            };

            var response = await ResolveAsync(request, cancellationToken);
            ThrowIfFailed(response, url);
            return response.Bundle;
        }

        #endregion

        #region POST 请求 / POST Requests

        /// <summary>
        /// 异步 Post 请求（JSON 字符串）
        /// <para>Async POST request (raw JSON string)</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="postData">JSON 请求体<para>JSON request body</para></param>
        /// <param name="headers">附加请求头，可空<para>Extra headers, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>响应文本<para>Response text</para></returns>
        public async UniTask<string> PostJsonAsync(string url, string postData,
            Dictionary<string, string> headers = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequest
            {
                Url = url,
                Method = UnityWebRequest.kHttpVerbPOST,
                Body = Encoding.UTF8.GetBytes(postData ?? string.Empty),
                ContentType = "application/json",
                Headers = headers,
                TimeoutOverride = timeout,
                Result = HttpRequest.ResultKind.Text
            };

            var response = await ResolveAsync(request, cancellationToken);
            ThrowIfFailed(response, url);
            return response.Text;
        }

        /// <summary>
        /// 异步 Post 泛型请求
        /// <para>Async POST generic request</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="postData">请求体对象<para>Request body object</para></param>
        /// <param name="headers">附加请求头，可空<para>Extra headers, nullable</para></param>
        /// <param name="serializerSettings">JSON 序列化设置，可空<para>JSON serializer settings, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>反序列化后的响应对象<para>Deserialized response of type T2</para></returns>
        public async UniTask<T2> PostJsonAsync<T1, T2>(string url, T1 postData,
            Dictionary<string, string> headers = null, JsonSerializerSettings serializerSettings = null,
            int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            string jsonData = JsonConvert.SerializeObject(postData, serializerSettings);
            string response = await PostJsonAsync(url, jsonData, headers, timeout, cancellationToken);
            return JsonConvert.DeserializeObject<T2>(response, serializerSettings);
        }

        /// <summary>
        /// 异步 Post 多部份请求
        /// <para>Async POST multipart request</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="formData">多部份表单内容<para>Multipart form sections</para></param>
        /// <param name="headers">附加请求头，可空<para>Extra headers, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>响应文本<para>Response text</para></returns>
        public async UniTask<string> PostMultipartAsync(string url, List<IMultipartFormSection> formData,
            Dictionary<string, string> headers = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequest
            {
                Url = url,
                Method = UnityWebRequest.kHttpVerbPOST,
                FormSections = formData,
                Headers = headers,
                TimeoutOverride = timeout,
                Result = HttpRequest.ResultKind.Text
            };

            var response = await ResolveAsync(request, cancellationToken);
            ThrowIfFailed(response, url);
            return response.Text;
        }

        /// <summary>
        /// 异步 Post 上传文件请求
        /// <para>Async POST file upload request</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="filePath">本地文件路径<para>Local file path</para></param>
        /// <param name="fieldName">表单字段名<para>Form field name</para></param>
        /// <param name="additionalFields">附加表单字段，可空<para>Extra form fields, nullable</para></param>
        /// <param name="headers">附加请求头，可空<para>Extra headers, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>响应文本；文件不存在或失败返回空<para>Response text; empty on missing file or failure</para></returns>
        public async UniTask<string> UploadFileAsync(string url, string filePath, string fieldName = "fileName",
            Dictionary<string, string> additionalFields = null, Dictionary<string, string> headers = null,
            int? timeout = null, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                D.Error($"The file not exist in the path: {filePath}");
                return string.Empty;
            }

            try
            {
                var formData = new List<IMultipartFormSection>();
                byte[] fileData = await File.ReadAllBytesAsync(filePath, cancellationToken);
                string fileName = Path.GetFileName(filePath);
                formData.Add(new MultipartFormFileSection(fieldName, fileData, fileName, GetMimeType(fileName)));

                if (additionalFields != null)
                {
                    foreach (var kv in additionalFields)
                        formData.Add(new MultipartFormDataSection(kv.Key, kv.Value));
                }

                return await PostMultipartAsync(url, formData, headers, timeout, cancellationToken);
            }
            catch (Exception ex)
            {
                D.Exception($"Upload file failed: {ex.Message} (Path: {filePath})");
                return string.Empty;
            }
        }

        #endregion

        #region PUT | DELETE 请求 / PUT & DELETE Requests

        /// <summary>
        /// 异步 Put 请求
        /// <para>Async PUT request</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="postData">请求体<para>Request body</para></param>
        /// <param name="headers">附加请求头，可空<para>Extra headers, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>响应文本<para>Response text</para></returns>
        public async UniTask<string> PutAsync(string url, string postData,
            Dictionary<string, string> headers = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequest
            {
                Url = url,
                Method = UnityWebRequest.kHttpVerbPUT,
                Body = Encoding.UTF8.GetBytes(postData ?? string.Empty),
                ContentType = "application/json",
                Headers = headers,
                TimeoutOverride = timeout,
                Result = HttpRequest.ResultKind.Text
            };

            var response = await ResolveAsync(request, cancellationToken);
            ThrowIfFailed(response, url);
            return response.Text;
        }

        /// <summary>
        /// 异步 Delete 请求
        /// <para>Async DELETE request</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="headers">附加请求头，可空<para>Extra headers, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>响应文本<para>Response text</para></returns>
        public async UniTask<string> DeleteAsync(string url, Dictionary<string, string> headers = null,
            int? timeout = null, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequest
            {
                Url = url,
                Method = UnityWebRequest.kHttpVerbDELETE,
                Headers = headers,
                TimeoutOverride = timeout,
                Result = HttpRequest.ResultKind.Text
            };

            var response = await ResolveAsync(request, cancellationToken);
            ThrowIfFailed(response, url);
            return response.Text;
        }

        #endregion

        #region DOWNLOAD 请求 / Download Requests

        /// <summary>
        /// 异步 Get 文件下载请求（返回是否成功，不抛异常）
        /// <para>Async GET file download (returns success, no exception)</para>
        /// </summary>
        /// <param name="url">请求地址<para>Request URL</para></param>
        /// <param name="savePath">本地保存路径<para>Local save path</para></param>
        /// <param name="progress">下载进度回调，可空<para>Download progress callback, nullable</para></param>
        /// <param name="timeout">超时覆盖（秒），null 用默认<para>Timeout override in seconds, null = default</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>是否成功<para>Success flag</para></returns>
        public async UniTask<bool> DownloadFileAsync(string url, string savePath,
            IProgress<float> progress = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            var request = new HttpRequest
            {
                Url = url,
                Method = UnityWebRequest.kHttpVerbGET,
                TimeoutOverride = timeout,
                Result = HttpRequest.ResultKind.File,
                SavePath = savePath,
                Progress = progress
            };

            var response = await ResolveAsync(request, cancellationToken);
            return response.IsSuccess;
        }

        #endregion

        #region 工具 / Utilities

        /// <summary>
        /// 获取文件拓展类型
        /// <para>Get the MIME type for a file extension</para>
        /// </summary>
        private static string GetMimeType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".pdf" => "application/pdf",
                ".zip" => "application/zip",
                ".txt" => "text/plain",
                ".html" or ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                _ => "application/octet-stream"
            };
        }

        #endregion
    }
}