/* 
 * ================================================
 * Describe:      This script is used to control client get data in server . 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-11-23 14:43:45
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-11-23 14:43:45
 * ScriptVersion: 0.1
 * ===============================================
*/

using System;
using System.Collections;
using UnityEngine.Networking;

namespace EasyFramework.Managers
{
    /// <summary>
    /// HTTP request manager.
    /// HTTP 请求管理器
    /// </summary>
    public class HttpsManager : Singleton<HttpsManager>, IManager
    {
        const string TOKEN = "";
        const string DOMAIN = "";

        const string GETJSON = "application/json";
        const string SENDFILES = "multipart/form-data";
        const string GETFILES = "application/x-www-form-urlencoded";

        void ISingleton.Init()
        {

        }

        void ISingleton.Quit()
        {

        }

        /// <summary>
        /// Get请求
        /// </summary>
        /// <param name="address">请求尾地址</param>
        /// <param name="callback">回调函数</param>
        public void Get(string address, Action<DownloadHandler> callback) => EF.StartCoroutines(GetFunc(address, callback));

        /// <summary>
        /// Post请求
        /// </summary>
        /// <param name="address">请求尾地址</param>
        /// <param name="callback">回调函数</param>
        public void Post(string address, Action<DownloadHandler> callback) => EF.StartCoroutines(PostFunc(address, callback));

        IEnumerator GetFunc(string address, Action<DownloadHandler> callback)
        {
            using UnityWebRequest uwr = UnityWebRequest.Get(address);

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
                callback?.Invoke(uwr.downloadHandler);
            else
                D.Error($"[ {address} ]\t Error Type: [ {uwr.result} ]\t>>>>>   {uwr.error}");
        }

        IEnumerator PostFunc(string address, Action<DownloadHandler> callback)
        {
            using UnityWebRequest uwr = UnityWebRequest.Post(address, address);

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
                callback?.Invoke(uwr.downloadHandler);
            else
                D.Error($"[ {address} ]\t Error Type: [ {uwr.result} ]\t>>>>>   {uwr.error}");
        }

        /*
        #region BestHttp
        /// <summary>
        /// 基于Best的Post的获取图片请求
        /// </summary>
        /// <param name="address">请求尾地址</param>
        /// <param name="jd">请求内容</param>
        /// <param name="callback">Texture2D回调函数</param>
        public void BestPostTexture(string address, JsonData jd, Action<Texture2D> callback)
        {
            D.Emphasize("The texture path is:    " + Domain + address + jd["path"]);
            HTTPRequest request = new HTTPRequest(new Uri(Domain + address), HTTPMethods.Post, (originalRequest, response) =>
            {
                if (!CheckRequestError(response))
                {
                    callback?.Invoke(response.DataAsTexture2D);
                }
            });
            request.SetHeader("content-type", GetFiles);
            request.AddHeader("User-Token", Token);
            if (null != jd)
            {
                D.Warning(jd.ToJson());
                request.RawData = Encoding.UTF8.GetBytes(jd.ToJson());
            }
            request.Send();
        }

        /// <summary>
        /// 基于Best的Post基本请求
        /// </summary>
        /// <param name="address">请求尾地址</param>
        /// <param name="jd">请求内容</param>
        /// <param name="callback">JsonData回调函数</param>
        public void BestPost(string address, JsonData jd, Action<JsonData> callback)
        {
            if (null == jd)
            {
                D.Error("Do you up to sky? Why the json data is null?  why");
                return;
            }

            HTTPRequest request = new HTTPRequest(new Uri(Domain + address), HTTPMethods.Post, (originalRequest, response) =>
            {
                if (!CheckRequestError(response) && OnRequestFinished(response.DataAsText, out JsonData backJson))
                {
                    callback?.Invoke(backJson);
                }
            });

            D.Warning($"{address}     -----     {jd.ToJson()}");
            request.RawData = Encoding.UTF8.GetBytes(jd.ToJson());
            SetHeader(request).Send();
        }

        /// <summary>
        /// 基于Best的Post上传请求
        /// </summary>
        /// <param name="address">请求尾地址</param>
        /// <param name="bytes">上传内容</param>
        /// <param name="callback">JsonData回调函数</param>
        public void BestPostUpload(string address, byte[] bytes, Action<JsonData> callback)
        {
            if (null == bytes)
            {
                D.Error("Do you up to sky? Why the bytes is null?  why");
                return;
            }
            HTTPRequest request = new HTTPRequest(new Uri(Domain + address), HTTPMethods.Post, (originalRequest, response) =>
            {
                if (!CheckRequestError(response) && OnRequestFinished(response.DataAsText, out JsonData backJson))
                {
                    callback?.Invoke(backJson);
                }
            });
            request.SetHeader("Content-Type", SendFiles);
            request.SetHeader("User-Token", Token);
            request.AddBinaryData("file", bytes);
            request.Send();
        }

        /// <summary>
        /// 一般接口给请求设置头环境
        /// </summary>
        /// <param name="request">请求</param>
        /// <returns>返回原有请求</returns>
        HTTPRequest SetHeader(HTTPRequest request)
        {
            request.SetHeader("Content-Type", GetJson);
            request.AddHeader("User-Token", Token);
            return request;
        }
        #endregion
        */

    }
}
