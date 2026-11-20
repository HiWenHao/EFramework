/*
 * ================================================
 * Describe:        默认 HTTP 发送器：基于 UnityWebRequest 执行请求，按上下文应用默认值并套用重试策略。
 *                  只认识 HttpContext，不认识缓存与设置，替换实现不影响其它服务。
 * Author:          Alvin8412
 * CreationTime:    2026-11-13 14:40
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-20 23:43:38
 * ScriptVersion:   0.1
 * ================================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace EasyFramework.Systems.Http
{
    /// <summary>
    /// 默认 HTTP 发送器，使用 <see cref="UnityWebRequest"/> 执行请求
    /// <para>Default HTTP sender using <see cref="UnityWebRequest"/></para>
    /// </summary>
    public sealed class DefaultHttpSender : IHttpSender
    {
        #region 字段 / Fields

        // 共享运行时上下文，仅用于读取默认值与重试策略
        private HttpContext _context;

        #endregion

        #region 方法 / Methods

        // 按重试策略循环发送，达到上限后返回最后一次结果
        private async UniTask<HttpResponse> SendWithRetryAsync(HttpRequest request, CancellationToken token)
        {
            var policy = _context?.RetryPolicy;
            int max = policy?.MaxRetries ?? 0;
            int attempt = 0;

            while (true)
            {
                // 取消即退出：不在取消态下发起（或重试）任何请求
                token.ThrowIfCancellationRequested();

                attempt++;
                var response = await SendOnceAsync(request, token);
                if (response.IsSuccess || attempt > max || !IsRetriable(response))
                    return response;

                int delay = policy?.DelayMs(attempt) ?? 0;
                if (delay > 0)
                    await UniTask.Delay(delay, cancellationToken: token);
            }
        }

        // 仅对可恢复失败重试：网络错误（无 HTTP 状态）/5xx 服务端错误/408 超时/429 限流。
        // 4xx 客户端错误（401/403/404 等）重试无意义，只会制造重试风暴
        private static bool IsRetriable(HttpResponse response)
        {
            long code = response.StatusCode;

            return code switch
            {
                <= 0 or >= 500 and <= 599 => true,
                _ => code is 408 or 429
            };
        }

        // 单次发送：构建请求 → 应用默认值 → 发送 → 收敛为 HttpResponse
        private async UniTask<HttpResponse> SendOnceAsync(HttpRequest request, CancellationToken token)
        {
            using var uwr = BuildRequest(request);
            if (uwr == null)
                return new HttpResponse { IsSuccess = false, Error = "Failed to build request" };

            ApplyDefaults(uwr, request);

            try
            {
                if (request.Progress != null)
                    await uwr.SendWebRequest().ToUniTask(progress: request.Progress, cancellationToken: token);
                else
                    await uwr.SendWebRequest().ToUniTask(cancellationToken: token);

                return BuildResponse(uwr, request);
            }
            catch (UnityWebRequestException e)
            {
                return new HttpResponse { IsSuccess = false, Error = e.Message, StatusCode = e.ResponseCode };
            }
        }

        // 按 ResultKind 与请求体构建 UnityWebRequest
        private static UnityWebRequest BuildRequest(HttpRequest request)
        {
            switch (request.Result)
            {
                case HttpRequest.ResultKind.File:
                    var fileRequest = new UnityWebRequest(request.Url, UnityWebRequest.kHttpVerbGET)
                    {
                        downloadHandler = new DownloadHandlerFile(request.SavePath)
                    };
                    return fileRequest;

                case HttpRequest.ResultKind.Texture:
                    return UnityWebRequestTexture.GetTexture(request.Url);

                case HttpRequest.ResultKind.AssetBundle:
                    return UnityWebRequestAssetBundle.GetAssetBundle(request.Url);

                case HttpRequest.ResultKind.Text:
                default:
                    if (request.FormSections != null)
                        return UnityWebRequest.Post(request.Url, request.FormSections);

                    var uwr = new UnityWebRequest(request.Url, request.Method)
                    {
                        downloadHandler = new DownloadHandlerBuffer()
                    };

                    if (request.Body != null)
                    {
                        uwr.uploadHandler = new UploadHandlerRaw(request.Body);
                        if (!string.IsNullOrEmpty(request.ContentType))
                            uwr.SetRequestHeader("Content-Type", request.ContentType);
                    }

                    return uwr;
            }
        }

        // 应用来自上下文的默认值与单次请求头覆盖
        private void ApplyDefaults(UnityWebRequest uwr, HttpRequest request)
        {
            if (_context == null) return;

            uwr.SetRequestHeader("User-Agent", _context.DefaultUserAgent);

            if (_context.DefaultHeaders != null)
                foreach (var kv in _context.DefaultHeaders)
                    uwr.SetRequestHeader(kv.Key, kv.Value);

            if (request.Headers != null)
                foreach (var kv in request.Headers)
                    uwr.SetRequestHeader(kv.Key, kv.Value);

            uwr.timeout = request.TimeoutOverride ?? _context.DefaultTimeout;

            if (_context.CertificateHandler == null) return;
            uwr.certificateHandler = _context.CertificateHandler;
            // 共享的 CertificateHandler 不能随单个请求 Dispose 被销毁，
            // 否则第一个请求结束后，所有并发/后续请求都拿到已释放的 handler
            uwr.disposeCertificateHandlerOnDispose = false;
        }

        // 把各类 DownloadHandler 的结果收敛到统一响应
        private static HttpResponse BuildResponse(UnityWebRequest uwr, HttpRequest request)
        {
            var response = new HttpResponse
            {
                IsSuccess = uwr.result == UnityWebRequest.Result.Success,
                StatusCode = uwr.responseCode
            };

            if (!response.IsSuccess)
                response.Error = uwr.error;

            switch (request.Result)
            {
                case HttpRequest.ResultKind.Texture:
                    response.Texture = response.IsSuccess ? DownloadHandlerTexture.GetContent(uwr) : null;
                    break;

                case HttpRequest.ResultKind.AssetBundle:
                    response.Bundle = response.IsSuccess ? DownloadHandlerAssetBundle.GetContent(uwr) : null;
                    break;

                case HttpRequest.ResultKind.File:
                    response.SavePath = request.SavePath;
                    break;

                case HttpRequest.ResultKind.Text:
                default:
                    if (uwr.downloadHandler is DownloadHandlerBuffer buffer)
                    {
                        response.Bytes = buffer.data;
                        response.Text = buffer.text;
                    }

                    break;
            }

            return response;
        }

        /// <summary>
        /// 绑定共享上下文
        /// <para>Bind the shared context.</para>
        /// </summary>
        /// <param name="context">共享运行时上下文 <para>Shared runtime context</para></param>
        public void Initialize(HttpContext context) => _context = context;

        /// <summary>
        /// 解绑上下文
        /// <para>Release the context reference.</para>
        /// </summary>
        public void Dispose() => _context = null;

        /// <summary>
        /// 发送请求，内部套用重试策略
        /// <para>Send the request with the retry policy applied</para>
        /// </summary>
        /// <param name="request">请求描述 <para>Request description</para></param>
        /// <param name="token">取消令牌 <para>Cancellation token</para></param>
        /// <returns>统一响应 <para>Unified response</para></returns>
        public UniTask<HttpResponse> SendAsync(HttpRequest request, CancellationToken token)
        {
            return string.IsNullOrWhiteSpace(request?.Url)
                ? UniTask.FromResult(new HttpResponse { IsSuccess = false, Error = "Empty URL" })
                : SendWithRetryAsync(request, token);
        }

        #endregion
    }
}
