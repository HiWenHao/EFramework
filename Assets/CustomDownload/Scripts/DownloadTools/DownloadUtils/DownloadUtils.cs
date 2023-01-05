using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace CustomUwrDownload
{
    /// <summary>
    /// 下载请使用DownloadTools
    /// </summary>
    internal class DownloadUtils : MonoBehaviour
    {
        internal static DownloadUtils Instance;
        private void Awake() => Instance = this;

        //当前文件的字节下载进度
        public float downloadProgress;
        #region 正常下载        _normal
        /// <summary>
        /// 开启下载
        /// </summary>
        internal void StartDownload(string assetsName, string url, Action isDoneCallback = null, List<string> allMd5 = null, int timeout = 10)
        {
            if (!string.IsNullOrEmpty(assetsName))
                StartCoroutine(DownloadFile(assetsName, url, isDoneCallback, index: 0, timeout: timeout));
            else
            {
                Debug.LogError("DownloadUtils => dan ge资源路径为空");
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        private IEnumerator DownloadFile(string assetName, string url, Action callback, string md5 = default, int index = default, int timeout = default)
        {
            UnityWebRequest __uwr = UnityWebRequest.Get(url + assetName);
            __uwr.SendWebRequest();

            downloadProgress = 0;
            while (!__uwr.isDone)
            {
                downloadProgress = __uwr.downloadProgress;
                yield return null;
            }
            yield return __uwr;

            bool __mSuccess = false;
            switch (__uwr.result)
            {
                case UnityWebRequest.Result.InProgress:
                    Debug.Log($"{assetName}请求还未结束。");
                    break;
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.ProtocolError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError($"{assetName}资源请求错误!!!!!：{__uwr.error}");
                    break;
                case UnityWebRequest.Result.Success:
                    __mSuccess = true;
                    break;
                default:
                    break;
            }

            if (__mSuccess && __uwr.downloadProgress == 1.0f)
            {
                //个数当前进度
                //_normal_downloadProgress = ++_normal_downloadLocation / _normal_downloadMaxCount;

                //Debuger.Log($"_downloadProgress = {_downloadProgress}, _downloadLocation = {_downloadLocation}, _downloadMaxCount = {_downloadMaxCount}");
                downloadProgress = 1;
                UwrFileHandling.CreateFile(
                    Application.persistentDataPath + $"/{DownloadTools.PorjectName}/video/{assetName}",
                    __uwr.downloadHandler.data, () =>
                    {
                        callback?.Invoke();
                    },
                    md5
                );
                //Debug.Log($"任务: {url + assetName}下载完成");
                //AssetDatabase.Refresh(); //刷新一下
                //Debug.Log(_downloadLocation + "进度为：" + _downloadProgress);
                __uwr.Abort();
                __uwr.Dispose();
                __uwr = null;
            }
        }
        #endregion

        #region 获取下载文件大小 _normal


        /// <summary>
        /// 获取本次下载文件总大小
        /// </summary>
        internal void GetAllFileSize(string assetsName, string url, Action<float> callback)
        {
            StartCoroutine(GetAllFilesSize(assetsName, url, callback));

        }
        /// <summary>
        /// 获取单个下载文件大小
        /// </summary>
        private IEnumerator GetAllFilesSize(string assetName, string url, Action<float> callback)
        {
            UnityWebRequest _uwr = UnityWebRequest.Head(url + assetName);
            yield return _uwr.SendWebRequest();

            if (_uwr.isDone)
            {
                float _normal_downloadSize = 0;
                _normal_downloadSize += float.Parse(_uwr.GetResponseHeader("Content-Length"));
                callback.Invoke(_normal_downloadSize);
                _uwr.Abort();
                _uwr.Dispose();
                _uwr = null;
            }
        }
        #endregion
    }
}