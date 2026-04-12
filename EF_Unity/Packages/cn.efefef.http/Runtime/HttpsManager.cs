/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-04-02 21:36:59
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-04-02 21:36:59
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyFramework.Managers
{
    /// <summary>
    /// Https请求管理器
    /// </summary>
    public class HttpManager : MonoSingleton<HttpManager>, ISingleton
    {
        /// <summary>
        /// 默认超时阈值
        /// </summary>
        public int DefaultTimeout { get; set; } = 30;
        
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
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask<T> GetAsync<T>(string url, Dictionary<string, string> headers = null,
            int? timeout = null, CancellationToken cancellationToken = default)
        {
            string json = await GetStringAsync(url, headers, timeout, cancellationToken);
            return JsonUtility.FromJson<T>(json);
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
        /// <param name="timeout">超时阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <typeparam name="T1">请求类型</typeparam>
        /// <typeparam name="T2">请求回复类型</typeparam>
        /// <returns>回复<typeparamref name="T2"/>类型数据</returns>
        public async UniTask<T2> PostJsonAsync<T1, T2>(string url, T1 postData,
            Dictionary<string, string> headers = null, int? timeout = null,
            CancellationToken cancellationToken = default)
        {
            string jsonData = JsonUtility.ToJson(postData);
            string response = await PostJsonAsync(url, jsonData, headers, timeout, cancellationToken);
            return JsonUtility.FromJson<T2>(response);
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
        /// <param name="cancellationToken">取消令牌</param>
        public async UniTask<bool> DownloadFileAsync(string url, string savePath,
            IProgress<float> progress = null, CancellationToken cancellationToken = default)
        {
            using var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerFile(savePath);
            await SendRawRequestAsync(request, null, null, cancellationToken, progress);
            return true;
        }

        #endregion
        
        #region 请求核心

        /// <summary> 发送请求并返回文本（适用于 DownloadHandlerBuffer）</summary>
        private async UniTask<string> SendRequestAsync(UnityWebRequest request,
            Dictionary<string, string> headers, int? timeout, CancellationToken cancellationToken)
        {
            using var cts = CreateLinkedCancellationToken(request, cancellationToken);
            SetHeadersAndTimeout(request, headers, timeout);
            await request.SendWebRequest().ToUniTask(cancellationToken: cts.Token);
            ValidateResponse(request);
            return request.downloadHandler.text;
        }

        /// <summary> 发送请求不关心响应内容（适用于 Texture/AssetBundle/File 等）</summary>
        private async UniTask SendRawRequestAsync(UnityWebRequest request,
            Dictionary<string, string> headers, int? timeout, CancellationToken cancellationToken,
            IProgress<float> progress = null)
        {
            using var cts = CreateLinkedCancellationToken(request, cancellationToken);
            SetHeadersAndTimeout(request, headers, timeout);
            await request.SendWebRequest().ToUniTask(progress: progress, cancellationToken: cts.Token);
            ValidateResponse(request);
        }

        /// <summary> 设置请求头和超时阈值 </summary>
        private void SetHeadersAndTimeout(UnityWebRequest request, Dictionary<string, string> headers, int? timeout)
        {
            if (headers != null)
                foreach (var kv in headers)
                {
                    request.SetRequestHeader(kv.Key, kv.Value);
                }
            request.timeout = timeout ?? DefaultTimeout;
        }

        /// <summary> 创建链接取消令牌 </summary>
        private CancellationTokenSource CreateLinkedCancellationToken(UnityWebRequest request, CancellationToken userToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(userToken);
            cts.Token.Register(() => request?.Abort());
            return cts;
        }

        /// <summary> 验证请求回复 </summary>
        private void ValidateResponse(UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success)
                D.Exception($"HTTP Error: {request.error} (URL: {request.url})");
        }

        /// <summary> 获取文件拓展类型 </summary>
        private string GetMimeType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                ".json" => "application/json",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        #endregion
    }
}