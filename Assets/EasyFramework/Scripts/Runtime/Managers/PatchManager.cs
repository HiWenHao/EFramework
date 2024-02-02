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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;
using Object = UnityEngine.Object;

namespace EasyFramework.Managers
{
    /// <summary>
    /// 资源运行模式
    /// </summary>
    public enum EFPlayMode
    {
        /// <summary>
        /// 编辑器下的模拟模式
        /// </summary>
        EditorSimulateMode,

        /// <summary>
        /// 离线运行模式
        /// </summary>
        OfflinePlayMode,

        /// <summary>
        /// 联机运行模式
        /// </summary>
        HostPlayMode,

        /// <summary>
        /// WebGL运行模式
        /// </summary>
        WebPlayMode,
    }

    /// <summary>
    /// 资源更新
    /// </summary>
    public class PatchManager : Singleton<PatchManager>, ISingleton
    {
        EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        Text m_Tips;
        Button btn_MessgeBox, btn_Done;
        Slider m_updaterSlider;
        Transform m_patchUpdater;

        string m_packageVersion;
        ResourcePackage m_Package;
        static Dictionary<string, bool> m_CacheData;
        ResourceDownloaderOperation m_Downloader;
        Action m_Callback;
        IEnumerator m_ie_currentIE;
        Queue<IEnumerator> m_que_updaterState;

        void ISingleton.Init()
        {
            // 初始化资源系统
            YooAssets.Initialize();
            m_que_updaterState = new Queue<IEnumerator>();
        }

        void ISingleton.Quit()
        {
            m_CacheData.Clear();
            m_CacheData = null;
            if (null != m_Package)
            {
                //清空该包体的全部缓存
                //m_Package.ClearAllCacheFilesAsync();
                m_Package = null;
            }

            m_Downloader = null;

            m_que_updaterState.Clear();
            m_que_updaterState = null;
            m_Callback = null;
        }

        /// <summary>
        /// 开始更新补丁
        /// </summary>
        /// <param name="mode">更新模式</param>
        /// <param name="callback">更新完成回调</param>
        /// <param name="packageName">要更新的包名</param>
        public void StartUpdatePatch(EFPlayMode mode, Action callback = null, string packageName = "DefaultPackage")
        {
            m_CacheData = new Dictionary<string, bool>(1000);
            PlayMode = (EPlayMode)mode;
            D.Correct($"资源系统运行模式：{mode}");
            m_Callback = callback;
            // 创建默认的资源包
            m_Package = YooAssets.CreatePackage(packageName);

            //设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
            YooAssets.SetDefaultPackage(m_Package);
            EF.Load.AddResourcePackage(m_Package);

            m_que_updaterState.Clear();
            m_que_updaterState.Enqueue(Initialize());
            m_que_updaterState.Enqueue(GetStaticVersion());
            m_que_updaterState.Enqueue(GetManifest());
            m_que_updaterState.Enqueue(CreateDownloader());
            m_que_updaterState.Enqueue(BeginDownload());

            Run("Initialize");
        }

        #region Run progress. 跑更新流程
        void Run(string nextState)
        {
            //D.Correct($"Next state is {nextState}       IEnumerator.Count = {m_que_updaterState.Count}");

            if (null != m_ie_currentIE)
                EF.StopCoroutines(m_ie_currentIE);
            if (nextState.Equals("Done") || m_que_updaterState.Count <= 0)
            {
                m_ie_currentIE = null;
                if (m_patchUpdater)
                    btn_MessgeBox.gameObject.SetActive(true);
                else
                    m_Callback?.Invoke();
                return;
            }

            m_ie_currentIE = m_que_updaterState.Dequeue();
            EF.StartCoroutines(m_ie_currentIE);
        }

        void UpdateDone()
        {
            m_Tips = null;
            m_updaterSlider = null;
            btn_MessgeBox.onClick.RemoveAllListeners();
            btn_Done.onClick.RemoveAllListeners();
            btn_MessgeBox = null;
            btn_Done = null;
            Object.Destroy(m_patchUpdater.gameObject);
            m_patchUpdater = null;
            m_Callback?.Invoke();
        }
        #endregion

        #region Setting config Initialize.初始化更新设置
        IEnumerator Initialize()
        {
            InitializeParameters initParameters = null;
            switch (PlayMode)
            {
                case EPlayMode.EditorSimulateMode:
                    initParameters = new EditorSimulateModeParameters
                    {
                        SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, m_Package.PackageName)
                    };
                    break;
                case EPlayMode.OfflinePlayMode:
                    initParameters = new OfflinePlayModeParameters
                    {
                        DecryptionServices = new FileStreamDecryption()
                    };
                    break;
                case EPlayMode.HostPlayMode:
                    initParameters = new HostPlayModeParameters
                    {
                        DecryptionServices = new FileStreamDecryption(),
                        BuildinQueryServices = new GameQueryServices(),
                        RemoteServices = new RemoteServices(EF.Projects.ResourcesArea.InnerUrl, EF.Projects.ResourcesArea.StandbyUrl)
                    };
                    break;
                case EPlayMode.WebPlayMode:
                    initParameters = new WebPlayModeParameters
                    {
                        BuildinQueryServices = new GameQueryServices(),
                        RemoteServices = new RemoteServices(EF.Projects.ResourcesArea.InnerUrl, EF.Projects.ResourcesArea.StandbyUrl)
                    };
                    break;
            }
            yield return m_Package.InitializeAsync(initParameters);
            Run("GetStaticVersion");
        }

        /// <summary>
        /// 远端资源地址查询服务类
        /// </summary>
        private class RemoteServices : IRemoteServices
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public RemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }
            string IRemoteServices.GetRemoteMainURL(string fileName)
            {
                return $"{_defaultHostServer}/{fileName}";
            }
            string IRemoteServices.GetRemoteFallbackURL(string fileName)
            {
                return $"{_fallbackHostServer}/{fileName}";
            }
        }

        /// <summary>
        /// 资源文件流加载解密类
        /// </summary>
        private class FileStreamDecryption : IDecryptionServices
        {
            /// <summary>
            /// 同步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                managedStream = bundleStream;
                return AssetBundle.LoadFromStream(bundleStream, fileInfo.ConentCRC, GetManagedReadBufferSize());
            }

            /// <summary>
            /// 异步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                managedStream = bundleStream;
                return AssetBundle.LoadFromStreamAsync(bundleStream, fileInfo.ConentCRC, GetManagedReadBufferSize());
            }

            private static uint GetManagedReadBufferSize()
            {
                return 1024;
            }
        }

        /// <summary>
        /// 资源文件偏移加载解密类
        /// </summary>
        private class FileOffsetDecryption : IDecryptionServices
        {
            /// <summary>
            /// 同步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                managedStream = null;
                return AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
            }

            /// <summary>
            /// 异步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                managedStream = null;
                return AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
            }

            private static ulong GetFileOffset()
            {
                return 32;
            }
        }
        #endregion

        #region Update the StaticViersion file.更新静态版本文件
        /// <summary>
        /// Update the StaticViersion file.更新静态版本文件
        /// </summary>
        IEnumerator GetStaticVersion()
        {
            var operation = m_Package.UpdatePackageVersionAsync();
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                //更新成功
                string packageVersion = operation.PackageVersion;
                m_packageVersion = packageVersion;
                D.Log($"Updated package Version : {packageVersion}");

                //拿到版本号接下来去获取Manifest信息     GetManifestInfo
                Run("GetManifestInfo");
            }
            else
            {
                //更新失败
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
            var operation = m_Package.UpdatePackageManifestAsync(m_packageVersion);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                //更新成功
                //注意：保存资源版本号作为下次默认启动的版本!
                operation.SavePackageVersion();

                //拿到配置信息接下来去获取热更资源
                Run("CreateDownloader");
            }
            else
            {
                //更新失败
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
            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            m_Downloader = m_Package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

            yield return null;

            if (m_Downloader.TotalDownloadCount == 0)
            {
                Run("Done");
            }
            else
            {
                // 注意：开发者需要在下载前检测磁盘空间不足
                EF.Ui.ShowDialog($"一共发现了{m_Downloader.TotalDownloadCount}个资源，总大小为{(int)(m_Downloader.TotalDownloadBytes / (1024f * 1024f))}mb需要更新,是否下载。",
                    okEvent: delegate
                    {
                        Run("BeginDownload");
                    },
                    okBtnText: "下载",
                    noBtnText: "取消"
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
            m_patchUpdater = Object.Instantiate(EF.Load.LoadInResources<GameObject>(EF.Projects.AppConst.UIPath + "PatchUpdater")).transform;
            m_updaterSlider = m_patchUpdater.Find("Slider").GetComponent<Slider>();
            m_Tips = m_patchUpdater.Find("Slider/txt_Tips").GetComponent<Text>();
            btn_MessgeBox = m_patchUpdater.Find("btn_MessgeBox").GetComponent<Button>();
            btn_Done = m_patchUpdater.Find("btn_MessgeBox/Frame/btn_Done").GetComponent<Button>();
            btn_MessgeBox.onClick.AddListener(UpdateDone);
            btn_Done.onClick.AddListener(UpdateDone);

            //注册下载回调
            m_Downloader.OnDownloadErrorCallback = OnDownloadErrorFunction;
            m_Downloader.OnDownloadProgressCallback = OnDownloadProgressUpdateFunction;
            m_Downloader.OnDownloadOverCallback = OnDownloadOverFunction;
            m_Downloader.OnStartDownloadFileCallback = OnStartDownloadFileFunction;

            //开启下载
            m_Downloader.BeginDownload();
            yield return m_Downloader;

            // 检测下载结果
            if (m_Downloader.Status != EOperationStatus.Succeed)
                yield break;

            Run("Done");
        }

        private void OnDownloadErrorFunction(string fileName, string error)
        {
            D.Error($"Download the file failed. The file name is {fileName} ,  Error info is {error}");
        }
        private void OnDownloadProgressUpdateFunction(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes)
        {
            m_updaterSlider.value = (float)currentDownloadBytes / totalDownloadBytes;

            string currentSizeMB = (currentDownloadBytes / 1048576f).ToString("f1");
            string totalSizeMB = (totalDownloadBytes / 1048576f).ToString("f1");
            m_Tips.text = $"{currentDownloadCount}/{totalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB";
        }
        private void OnStartDownloadFileFunction(string fileName, long sizeBytes)
        {
            //D.Error("当前下载：" + fileName + "   大小为： " + sizeBytes);
        }
        private void OnDownloadOverFunction(bool isSucceed)
        {
            //D.Log("下载完成，结果为：" + isSucceed);
        }
        #endregion
    }

    /// <summary>
    /// 资源文件解密流
    /// </summary>
    public class BundleStream : FileStream
    {
        public const byte KEY = 64;

        public BundleStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
        {
        }
        public BundleStream(string path, FileMode mode) : base(path, mode)
        {
        }

        public override int Read(byte[] array, int offset, int count)
        {
            var index = base.Read(array, offset, count);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] ^= KEY;
            }
            return index;
        }
    }
}
