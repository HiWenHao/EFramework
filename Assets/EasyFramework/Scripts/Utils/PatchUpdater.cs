/* 
 * ================================================
 * Describe:      This script is used to Update the StaticViersion file. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-19 10:31:31
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-10-19 10:31:31
 * ScriptVersion: 0.1
 * ===============================================
*/
using EasyFramework.Framework.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XHTools;
using YooAsset;

namespace EasyFramework.Utils
{
    public class PatchUpdater : MonoSingleton<PatchUpdater>,ISingleton
    {
        public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        Text m_Tips;
        Button btn_MessgeBox, btn_Done;
        Slider m_updaterSlider;
        Transform m_patchUpdater;

        string PackageCRC;
        PatchDownloaderOperation Downloader;
        IEnumerator m_ie_currentIE;
        Queue<IEnumerator> m_que_updaterState;

        void ISingleton.Init()
        {
            D.Correct($"资源系统运行模式：{PlayMode}");
            // 初始化BetterStreaming
            BetterStreamingAssets.Initialize();

            m_que_updaterState = new Queue<IEnumerator>();
            m_que_updaterState.Enqueue(Initialize());
            m_que_updaterState.Enqueue(GetStaticVersion());
            m_que_updaterState.Enqueue(GetManifest());
            m_que_updaterState.Enqueue(CreateDownloader());
            m_que_updaterState.Enqueue(BeginDownload());
            
            Run("Initialize");
        }

        void ISingleton.Quit()
        {

        }

        void Run(string nextState)
        {
            D.Correct("Next state is " + nextState);
            if ("Done" == nextState || m_que_updaterState.Count <= 0)
            {
                StopCoroutine(m_ie_currentIE);
                m_ie_currentIE = null;

                if(m_patchUpdater)
                    btn_MessgeBox.gameObject.SetActive(true);
                return;
            }

            if (null != m_ie_currentIE)
                StopCoroutine(m_ie_currentIE);
            m_ie_currentIE = m_que_updaterState.Dequeue();
            StartCoroutine(m_ie_currentIE);

        }

        void UpdateDone()
        {
            m_Tips = null;
            m_updaterSlider = null;
            btn_MessgeBox.onClick.RemoveAllListeners();
            btn_Done.onClick.RemoveAllListeners();
            btn_MessgeBox = null;
            btn_Done = null;
            Destroy(m_patchUpdater.gameObject);
            m_patchUpdater = null;
        }

        #region Setting config Initialize
        IEnumerator Initialize()
        {
            // 初始化资源系统
            YooAssets.Initialize();

            // 创建默认的资源包
            var defaultPackage = YooAssets.CreateAssetsPackage("DefaultPackage");

            // 设置该资源包为默认的资源包
            YooAssets.SetDefaultAssetsPackage(defaultPackage);

            // 编辑器下的模拟模式
            if (PlayMode == EPlayMode.EditorSimulateMode)
            {
                var createParameters = new EditorSimulateModeParameters();
                createParameters.LocationServices = new AddressLocationServices();
                createParameters.SimulatePatchManifestPath = EditorSimulateModeHelper.SimulateBuild("DefaultPackage");
                yield return defaultPackage.InitializeAsync(createParameters);
            }

            // 单机运行模式
            if (PlayMode == EPlayMode.OfflinePlayMode)
            {
                var createParameters = new OfflinePlayModeParameters();
                createParameters.LocationServices = new AddressLocationServices();
                yield return defaultPackage.InitializeAsync(createParameters);
            }

            // 联机运行模式
            if (PlayMode == EPlayMode.HostPlayMode)
            {
                var createParameters = new HostPlayModeParameters();
                createParameters.LocationServices = new AddressLocationServices();
                createParameters.QueryServices = new QueryStreamingAssetsFileServices();
                createParameters.DefaultHostServer = GetHostServerURL();
                createParameters.FallbackHostServer = GetHostServerURL();
                yield return defaultPackage.InitializeAsync(createParameters);
            }

            Run("GetStaticVersion");
        }

        private string GetHostServerURL()
        {
            string hostServerIP = "http://127.0.0.1/dashboard/Adownload";
            string gameVersion = "v1.0";

#if UNITY_EDITOR
            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
                return $"{hostServerIP}/CDN/Android/{gameVersion}";
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
                return $"{hostServerIP}/CDN/IPhone/{gameVersion}";
            else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
                return $"{hostServerIP}/CDN/WebGL/{gameVersion}";
            else
                return $"{hostServerIP}/CDN/PC/{gameVersion}";
#else
		if (Application.platform == RuntimePlatform.Android)
			return $"{hostServerIP}/CDN/Android/{gameVersion}";
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
			return $"{hostServerIP}/CDN/IPhone/{gameVersion}";
		else if (Application.platform == RuntimePlatform.WebGLPlayer)
			return $"{hostServerIP}/CDN/WebGL/{gameVersion}";
		else
			return $"{hostServerIP}/CDN/PC/{gameVersion}";
#endif
        }
        private class QueryStreamingAssetsFileServices : IQueryServices
        {
            public bool QueryStreamingAssets(string fileName)
            {
                return BetterStreamingAssets.FileExists($"YooAssets/{fileName}");
            }
        }
        #endregion

        #region Update the StaticViersion file.更新静态版本文件
        /// <summary>
        /// Update the StaticViersion file.更新静态版本文件
        /// </summary>
        IEnumerator GetStaticVersion()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            var package = YooAssets.GetAssetsPackage("DefaultPackage");
            UpdateStaticVersionOperation operation = package.UpdateStaticVersionAsync(30);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                PackageCRC = operation.PackageCRC;
                //拿到版本号接下来去获取Manifest信息     GetManifestInfo
                Run("GetManifestInfo");
            }
            else
            {
                D.Error($"Get the StaticVersion file error: {operation.Error}");
            }
        }
        #endregion

        #region Update the GetManifest file.更新配置文件清单
        /// <summary>
        /// Update the Manifest file.更新配置文件清单
        /// </summary>
        IEnumerator GetManifest()
        {
            var package = YooAssets.GetAssetsPackage("DefaultPackage");
            UpdateManifestOperation operation = package.UpdateManifestAsync(PackageCRC);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                //拿到配置信息接下来去获取热更资源
                Run("CreateDownloader");
            }
            else
            {
                D.Error($"Get the Manifest file error: {operation.Error}");
            }
        }
        #endregion

        #region Create one downloader.创建一个下载器
        /// <summary>
        /// Create one downloader.创建一个下载器
        /// </summary>
        IEnumerator CreateDownloader()
        {
            yield return new WaitForSecondsRealtime(0.5f);

            Downloader = YooAssets.CreatePatchDownloader(10, 3);
            if (Downloader.TotalDownloadCount == 0)
            {
                Run("Done");
            }
            else
            {
                // 注意：开发者需要在下载前检测磁盘空间不足
                EF.Ui.ShowDialog($"一共发现了{Downloader.TotalDownloadCount}个资源，总大小为{(int)(Downloader.TotalDownloadBytes / (1024f * 1024f))}mb需要更新,是否下载。",
                    okEvent: delegate 
                    {
                        Run("BeginDownload");
                    },
                    okBtnText:"下载",
                    noBtnText:"取消"
                );
            }
        }
        #endregion

        #region Download service pack.下载补丁包
        /// <summary>
        /// Download service pack.下载补丁包
        /// </summary>
        IEnumerator BeginDownload()
        {
            m_patchUpdater = Instantiate(EF.Load.Load<GameObject>(AppConst.UI + "PatchUpdater")).transform;
            m_updaterSlider = m_patchUpdater.Find("Slider").GetComponent<Slider>();
            m_Tips = m_patchUpdater.Find("Slider/txt_Tips").GetComponent<Text>();
            btn_MessgeBox = m_patchUpdater.Find("btn_MessgeBox").GetComponent<Button>();
            btn_Done = m_patchUpdater.Find("btn_MessgeBox/Frame/btn_Done").GetComponent<Button>();
            btn_MessgeBox.onClick.AddListener(UpdateDone);
            btn_Done.onClick.AddListener(UpdateDone);

            // 注册下载回调
            Downloader.OnDownloadErrorCallback = SendWebFileDownloadFailedMsg;
            Downloader.OnDownloadProgressCallback = SendDownloadProgressUpdateMsg;
            Downloader.BeginDownload();
            yield return Downloader;

            // 检测下载结果
            if (Downloader.Status != EOperationStatus.Succeed)
                yield break;

            Run("Done");
        }

        public void SendWebFileDownloadFailedMsg(string fileName, string error)
        {
            D.Error($"Download the file failed. The file name is {fileName} ,  Error info is {error}");
        }
        public void SendDownloadProgressUpdateMsg(int totalDownloadCount, int currentDownloadCount, long totalDownloadSizeBytes, long currentDownloadSizeBytes)
        {
            m_updaterSlider.value = (float)currentDownloadSizeBytes / totalDownloadSizeBytes;

            string currentSizeMB = (currentDownloadSizeBytes / 1048576f).ToString("f1");
            string totalSizeMB = (totalDownloadSizeBytes / 1048576f).ToString("f1");
            m_Tips.text = $"{currentDownloadCount}/{totalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB";
        }
        #endregion
    }
}
