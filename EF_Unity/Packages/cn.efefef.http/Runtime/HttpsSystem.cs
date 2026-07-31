/*
 * ================================================
 * Describe:      Https 请求管理器：封装 UnityWebRequest
 * Author:        Alvin8412
 * CreationTime:  2026-04-02 21:36:59
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-07-30 17:35:00
 * ScriptVersion: 0.1
 * ===============================================
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
    /// Https请求管理器
    /// </summary>
    [Manager]
    public class HttpsSystem : MonoSingleton<HttpsSystem>, ISingleton
    {
        /// <summary>
        /// 默认超时阈值
        /// </summary>
        public int DefaultTimeout { get; set; } = 30;

        /// <summary>
        /// 默认User-Agent请求头
        /// </summary>
        public string DefaultUserAgent { get; set; } = "EasyFramework/1.0 (Unity)";

        /// <summary>
        /// 负责对通过 https 请求接收到的证书进行拒绝或接受处理。
        /// <para>Responsible for rejecting or accepting certificates received on https requests.</para>
        /// </summary>
        public CertificateHandler CertificateHandler { get; set; }

        /// <summary>
        /// 默认请求头，会合并进每一次请求（可被单次请求头覆盖）
        /// <para>Default headers merged into every request (overridable per-request).</para>
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; set; }

        /// <summary>
        /// 重试策略
        /// <para>Retry policy</para>
        /// </summary>
        public HttpRetryPolicy RetryPolicy { get; set; }

        void ISingleton.Init()
        {
        }

        void ISingleton.Quit()
        {
        }

        #region GET 请求

        /// <summary>
        /// 异步Get请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="headers">请求头</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns></returns>
        public async UniTask<string> GetStringAsync(string url, Dictionary<string, string> headers = null,
            int? timeout = null, CancellationToken cancellationToken = default)
        {
            using var request = UnityWebRequest.Get(url);
            return await SendRequestAsync(request, headers, timeout, cancellationToken);
        }

        /// <summary>
        /// 异步Get泛型请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="headers">请求头</param>
        /// <param name="serializerSettings">序列化设置</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask<T> GetAsync<T>(string url, Dictionary<string, string> headers = null,
            JsonSerializerSettings serializerSettings = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            string json = await GetStringAsync(url, headers, timeout, cancellationToken);
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
        }

        /// <summary>
        /// 异步Get纹理请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="headers">请求头</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask<Texture2D> GetTextureAsync(string url, Dictionary<string, string> headers = null,
            int? timeout = null, CancellationToken cancellationToken = default)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            await SendRawRequestAsync(request, headers, timeout, cancellationToken);
            return DownloadHandlerTexture.GetContent(request);
        }

        /// <summary>
        /// 异步Get资源包请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="headers">请求头</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask<AssetBundle> GetAssetBundleAsync(string url, Dictionary<string, string> headers = null,
            int? timeout = null, CancellationToken cancellationToken = default)
        {
            using var request = UnityWebRequestAssetBundle.GetAssetBundle(url);
            await SendRawRequestAsync(request, headers, timeout, cancellationToken);
            return DownloadHandlerAssetBundle.GetContent(request);
        }

        #endregion

        #region POST 请求

        /// <summary>
        /// 异步Post请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="postData">请求数据</param>
        /// <param name="headers">请求头</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask<string> PostJsonAsync(string url, string postData,
            Dictionary<string, string> headers = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            return await SendRequestAsync(request, headers, timeout, cancellationToken);
        }

        /// <summary>
        /// 异步Post泛型请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="postData">请求数据</param>
        /// <param name="headers">请求头</param>
        /// <param name="serializerSettings">序列化设置</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <typeparam name="T1">请求类型</typeparam>
        /// <typeparam name="T2">请求回复类型</typeparam>
        /// <returns>回复<typeparamref name="T2"/>类型数据</returns>
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
        /// 异步Post多部份请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="formData">请求数据</param>
        /// <param name="headers">请求头</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask<string> PostMultipartAsync(string url, List<IMultipartFormSection> formData,
            Dictionary<string, string> headers = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            using var request = UnityWebRequest.Post(url, formData);
            return await SendRequestAsync(request, headers, timeout, cancellationToken);
        }

        /// <summary>
        /// 异步Post上传文件请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="filePath">文件地址</param>
        /// <param name="fieldName">文件名</param>
        /// <param name="additionalFields">附加字段</param>
        /// <param name="headers">请求头</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
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

        #region PUT | DELETE    请求

        /// <summary>
        /// 异步Put请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="postData">请求数据</param>
        /// <param name="headers">请求头</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask<string> PutAsync(string url, string postData,
            Dictionary<string, string> headers = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            return await SendRequestAsync(request, headers, timeout, cancellationToken);
        }

        /// <summary>
        /// 异步Delete请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="headers">请求头</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask<string> DeleteAsync(string url, Dictionary<string, string> headers = null,
            int? timeout = null, CancellationToken cancellationToken = default)
        {
            using var request = UnityWebRequest.Delete(url);
            return await SendRequestAsync(request, headers, timeout, cancellationToken);
        }

        #endregion

        #region DOWNLOAD    请求

        /// <summary>
        /// 异步Get文件下载请求
        /// </summary>
        /// <param name="url">链接地址</param>
        /// <param name="savePath">保存地址</param>
        /// <param name="progress">进度回调</param>
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask<bool> DownloadFileAsync(string url, string savePath,
            IProgress<float> progress = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerFile(savePath);
            await SendRawRequestAsync(request, null, timeout, cancellationToken, progress);
            return request.result == UnityWebRequest.Result.Success;
        }

        #endregion

        #region 请求核心

        /// <summary> 发送请求并返回文本（适用于 DownloadHandlerBuffer）</summary>
        private async UniTask<string> SendRequestAsync(UnityWebRequest request,
            Dictionary<string, string> headers, int? timeout, CancellationToken cancellationToken)
        {
            using var cts = CreateLinkedCancellationToken(request, cancellationToken);
            SetDefaultCertificateHandler(request);
            SetHeadersAndTimeout(request, headers, timeout);
            await SendWebRequestWithRetry(request, cts.Token);
            return request.downloadHandler.text;
        }

        /// <summary> 发送请求不关心响应内容（适用于 Texture/AssetBundle/File 等）</summary>
        private async UniTask SendRawRequestAsync(UnityWebRequest request,
            Dictionary<string, string> headers, int? timeout, CancellationToken cancellationToken,
            IProgress<float> progress = null)
        {
            using var cts = CreateLinkedCancellationToken(request, cancellationToken);
            SetDefaultCertificateHandler(request);
            SetHeadersAndTimeout(request, headers, timeout);
            await SendWebRequestWithRetry(request, cts.Token, progress);
        }

        /// <summary> 设置默认证书处理 </summary>
        private void SetDefaultCertificateHandler(UnityWebRequest request)
        {
            if (null != CertificateHandler)
                request.certificateHandler = CertificateHandler;
        }

        /// <summary> 设置请求头和超时阈值 </summary>
        private void SetHeadersAndTimeout(UnityWebRequest request, Dictionary<string, string> headers, int? timeout)
        {
            request.SetRequestHeader("User-Agent", DefaultUserAgent);

            if (DefaultHeaders != null)
                foreach (var kv in DefaultHeaders)
                {
                    request.SetRequestHeader(kv.Key, kv.Value);
                }

            if (headers != null)
                foreach (var kv in headers)
                {
                    request.SetRequestHeader(kv.Key, kv.Value);
                }

            request.timeout = timeout ?? DefaultTimeout;
        }

        /// <summary>
        /// 带重试地发送请求并校验响应
        /// <para>Sends the request with retry and validates the response.</para>
        /// </summary>
        private async UniTask SendWebRequestWithRetry(UnityWebRequest request, CancellationToken token, IProgress<float> progress = null)
        {
            int maxRetries = RetryPolicy?.MaxRetries ?? 0;
            int attempt = 0;
            while (true)
            {
                attempt++;
                if (progress != null)
                    await request.SendWebRequest().ToUniTask(progress: progress, cancellationToken: token);
                else
                    await request.SendWebRequest().ToUniTask(cancellationToken: token);

                // 成功则直接返回；未达重试上限则退避后重试
                if (request.result == UnityWebRequest.Result.Success)
                    return;
                if (attempt > maxRetries)
                    ValidateResponse(request);

                int delay = RetryPolicy?.DelayMs(attempt) ?? 0;
                await UniTask.Delay(delay, cancellationToken: token);
            }
        }

        /// <summary> 创建链接取消令牌 </summary>
        private CancellationTokenSource CreateLinkedCancellationToken(UnityWebRequest request,
            CancellationToken userToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(userToken);
            cts.Token.Register(() =>
            {
                if (request is { isDone: false })
                    request.Abort();
            });
            return cts;
        }

        /// <summary> 验证请求回复 </summary>
        private void ValidateResponse(UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success)
                throw new HttpRequestException($"HTTP Error: {request.error} (URL: {request.url})");
        }

        /// <summary> 获取文件拓展类型 </summary>
        private string GetMimeType(string fileName)
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