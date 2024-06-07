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

using LitJson;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace EasyFramework.Managers
{
    /// <summary>
    /// Client get data in server.
    /// </summary>
    public class HttpsManager : MonoSingleton<HttpsManager>, IManager
    {
        int m_managerLevel = -99;
        int IManager.ManagerLevel
        {
            get
            {
                if (m_managerLevel < -1)
                    m_managerLevel = EF.Projects.AppConst.ManagerLevels.IndexOf(Name);
                return m_managerLevel;
            }
        }



        const string Token = "";
        const string Domain = "";

        const string GetJson = "application/json";
        const string SendFiles = "multipart/form-data";
        const string GetFiles = "application/x-www-form-urlencoded";

        void ISingleton.Init()
        {

        }

        void ISingleton.Quit()
        {
            StopAllCoroutines();
        }

        /// <summary>
        /// 获取请求
        /// </summary>
        /// <param name="address">请求尾地址</param>
        /// <param name="callback">回调函数</param>
        public void Get(string address, Action<string> callback)
        {
            StartCoroutine(GetFunc(address, callback));
        }

        /// <summary>
        /// 获取请求
        /// </summary>
        /// <param name="address">请求尾地址</param>
        /// <param name="callback">回调函数</param>
        public void Get(string address, Action<byte[]> callback)
        {
            StartCoroutine(GetFunc(address, callback));
        }

        IEnumerator GetFunc(string address, Action<string> callback)
        {
            using UnityWebRequest _uwr = UnityWebRequest.Get(address);

            yield return _uwr.SendWebRequest();

            if (_uwr.result == UnityWebRequest.Result.Success)
                callback?.Invoke(_uwr.downloadHandler.text);
            else
                D.Error($"[ {address} ]\t Error Type: [ {_uwr.result} ]\t>>>>>   {_uwr.error}");
        }
        IEnumerator GetFunc(string address, Action<byte[]> callback)
        {
            using UnityWebRequest _uwr = UnityWebRequest.Get(address);

            yield return _uwr.SendWebRequest();

            if (_uwr.result == UnityWebRequest.Result.Success)
                callback?.Invoke(_uwr.downloadHandler.data);
            else
                D.Error($"[ {address} ]\t Error Type: [ {_uwr.result} ]\t>>>>>   {_uwr.error}");
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
