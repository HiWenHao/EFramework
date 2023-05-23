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
using BestHTTP;
using LitJson;
using System;
using System.Text;
using UnityEngine;
using EasyFramework;

namespace EasyFramework.Managers
{
    /// <summary>
    /// Client get data in server.
    /// </summary>
    public class HttpsManager : Singleton<HttpsManager>, IManager
    {
        /// <summary>
        /// 返回码
        /// </summary>
        enum CallbackCode
        {
            /// <summary>
            /// 成功
            /// </summary>
            Success = 200,
        }

        const string Token = "";
        const string Domain = "";

        const string GetJson = "application/json";
        const string SendFiles = "multipart/form-data";
        const string GetFiles = "application/x-www-form-urlencoded";

        int IManager.ManagerLevel => EF.Projects.AppConst.ManagerLevels.IndexOf("HttpManager");

        void ISingleton.Init()
        {

        }

        void ISingleton.Quit()
        {

        }

        #region BestHttp
        /// <summary>
        /// 基于Best的Get请求
        /// </summary>
        /// <param name="address">请求尾地址</param>
        /// <param name="callback">JsonData回调函数</param>
        public void BestGet(string address, EAction<JsonData> callback)
        {
            HTTPRequest request = new HTTPRequest(new Uri(Domain + address), HTTPMethods.Get, (originalRequest, response) =>
            {
                if (!CheckRequestError(response) && OnRequestFinished(response.DataAsText, out JsonData backJson))
                {
                    callback?.Invoke(backJson);
                }
            });
            D.Log($"{address}");
            SetHeader(request).Send();
        }

        /// <summary>
        /// 基于Best的Get请求
        /// </summary>
        /// <param name="address">请求尾地址</param>
        /// <param name="callback">byte[]回调函数</param>
        public void BestGet(string address, EAction<byte[]> callback)
        {
            HTTPRequest request = new HTTPRequest(new Uri(Domain + address), HTTPMethods.Get, (originalRequest, response) =>
            {
                if (!CheckRequestError(response))
                {
                    callback?.Invoke(response.Data);
                }
            });
            D.Log($"{address}");
            SetHeader(request).Send();
        }

        /// <summary>
        /// 基于Best的Post的获取图片请求
        /// </summary>
        /// <param name="address">请求尾地址</param>
        /// <param name="jd">请求内容</param>
        /// <param name="callback">Texture2D回调函数</param>
        public void BestPostTexture(string address, JsonData jd, EAction<Texture2D> callback)
        {
            D.Correct("The texture path is:    " + Domain + address + jd["path"]);
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
        public void BestPost(string address, JsonData jd, EAction<JsonData> callback)
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
        public void BestPostUpload(string address, byte[] bytes, EAction<JsonData> callback)
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

        /// <summary>
        /// 检查请求是否错误
        /// </summary>
        /// <param name="response">响应</param>
        /// <returns>请求有错误时，返回True</returns>
        bool CheckRequestError(HTTPResponse response)
        {
            if (response.StatusCode == 200)
            {
                return false;
            }
            if (response.StatusCode == 500)
            {
                D.Error("The server error, please called the server programmer..   ..");
            }
            else if (response.StatusCode == 400)
            {
                D.Error("The deta error, please check you data information. If you sure data nothing wrong, called the server programmer. ");
            }
            else
            {
                D.Error(response.StatusCode + "  --  " + response.Message);
            }
            return true;
        }

        /// <summary>
        /// 当请求完成时，检测实际内容
        /// </summary>
        bool OnRequestFinished(string jdText, out JsonData backJson)
        {
            D.Log(jdText);
            JsonData _jd = JsonMapper.ToObject(jdText);

            CallbackCode _code = (CallbackCode)(int)_jd["code"];
            switch (_code)
            {
                case CallbackCode.Success:
                    backJson = _jd["data"];
                    return true;
                default:
                    D.Error($"Current code [{(int)_code}] is not setting in project, please called server!!!!!!!!!!!!!!!!! The server back message is {_jd["msg"]}");
                    break;
            }
            backJson = null;
            return false;
        }
        #endregion
    }
}
