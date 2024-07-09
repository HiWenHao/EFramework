/* 
 * ================================================
 * Describe:      This script is used to Update the StaticViersion file. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-10-19 10:31:31
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-07-09 15:38:31
 * ScriptVersion: 0.1
 * ===============================================
*/

using EasyFramework.UI;
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

        Transform m_patchUpdater;
        RectTransform Tran_Updater;
        RectTransform Tran_HintsBox;
        RectTransform Tran_MessgeBox;
        Text Txt_Hints;
        Text Txt_UpdaterTips;
        Slider Sld_UpdaterSlider;
        List<Button> m_AllButtons;

        string m_packageVersion;
        ResourcePackage m_Package;
        Dictionary<string, bool> m_CacheData;
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
        /// Start update patch.
        /// <para>开始更新补丁</para>
        /// </summary>
        /// <param name="mode">Refresh scheme.<para>更新模式</para></param>
        /// <param name="callback">Update the completion callback.<para>更新完成回调</para></param>
        /// <param name="packageName">The name of the package to update<para>要更新的包名</para></param>
        public void StartUpdatePatch(EFPlayMode mode, Action callback = null, string packageName = "DefaultPackage")
        {
            m_CacheData = new Dictionary<string, bool>(1000);
            PlayMode = (EPlayMode)mode;
            D.Emphasize($"资源系统运行模式：{mode}");
            m_Callback = callback;
            m_Package = YooAssets.TryGetPackage(packageName);
            if (m_Package == null)
            {
                // 创建默认的资源包
                m_Package = YooAssets.CreatePackage(packageName);

                //设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
                YooAssets.SetDefaultPackage(m_Package);
                EF.Load.AddResourcePackage(m_Package);
            }
            else
            {
                m_Package = YooAssets.GetPackage(packageName);
            }

            m_que_updaterState.Clear();
            if (m_Package.InitializeStatus != EOperationStatus.Succeed)
            {
                m_que_updaterState.Enqueue(Initialize());
                m_que_updaterState.Enqueue(GetStaticVersion());
                m_que_updaterState.Enqueue(GetManifest());
            }

            m_que_updaterState.Enqueue(CreateDownloader());
            m_que_updaterState.Enqueue(BeginDownload());

            Run("Initialize");
        }

        #region Run progress. 跑更新流程
        void Run(string nextState)
        {
            //D.Emphasize($"Next state is {nextState}       IEnumerator.Count = {m_que_updaterState.Count}");

            if (null != m_ie_currentIE)
                EF.StopCoroutines(m_ie_currentIE);
            if (nextState.Equals("Done") || m_que_updaterState.Count <= 0)
            {
                m_ie_currentIE = null;
                if (m_patchUpdater)
                    Tran_MessgeBox.gameObject.SetActive(true);
                else
                    m_Callback?.Invoke();
                return;
            }

            m_ie_currentIE = m_que_updaterState.Dequeue();
            EF.StartCoroutines(m_ie_currentIE);
        }

        void UpdateDone()
        {
            Txt_Hints = null;
            Tran_Updater = null;
            Tran_HintsBox = null;
            Tran_MessgeBox = null;
            Txt_UpdaterTips = null;
            Sld_UpdaterSlider = null;

            m_AllButtons.ReleaseAndRemoveEvent();
            m_AllButtons = null;

            Object.Destroy(m_patchUpdater.gameObject);
            m_patchUpdater = null;
            m_Callback?.Invoke();

            m_CacheData.Clear();
            m_CacheData = null;
            m_Callback = null;
            m_Package = null;
        }
        #endregion

        #region Setting config Initialize.初始化更新设置
        IEnumerator Initialize()
        {
            InitializeParameters _initParameters = null;
            switch (PlayMode)
            {
                case EPlayMode.EditorSimulateMode:
                    var _simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, m_Package.PackageName);
                    _initParameters = new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(_simulateBuildResult)
                    };
                    break;
                case EPlayMode.OfflinePlayMode:
                    _initParameters = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    break;
                case EPlayMode.HostPlayMode:
                    _initParameters = new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters
                        (
                            new RemoteServices(
                                EF.Projects.ResourcesArea.InnerUrl,
                                EF.Projects.ResourcesArea.StandbyUrl
                            )
                        )
                    };
                    break;
                case EPlayMode.WebPlayMode:
                    _initParameters = new WebPlayModeParameters
                    {
                        WebFileSystemParameters = FileSystemParameters.CreateDefaultWebFileSystemParameters()
                    };
                    break;
            }

            InitializationOperation _initializationOperation = m_Package.InitializeAsync(_initParameters);
            yield return _initializationOperation;

            if (_initializationOperation.Status != EOperationStatus.Succeed)
            {
                D.Error(_initializationOperation.Error);
            }
            else
            {
                Run("GetStaticVersion");
            }
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
                return AssetBundle.LoadFromStream(bundleStream, fileInfo.FileLoadCRC, GetManagedReadBufferSize());
            }

            /// <summary>
            /// 异步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                managedStream = bundleStream;
                return AssetBundle.LoadFromStreamAsync(bundleStream, fileInfo.FileLoadCRC, GetManagedReadBufferSize());
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
                return AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, GetFileOffset());
            }

            /// <summary>
            /// 异步方式获取解密的资源包对象
            /// 注意：加载流对象在资源包对象释放的时候会自动释放
            /// </summary>
            AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
            {
                managedStream = null;
                return AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, GetFileOffset());
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
            var operation = m_Package.RequestPackageVersionAsync();
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
        /// CreateTimeEvent one downloader.创建一个下载器
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
                if (null == m_patchUpdater)
                {
                    m_patchUpdater = Object.Instantiate(EF.Load.LoadInResources<GameObject>(EF.Projects.AppConst.UIPrefabsPath + "PatchUpdater")).transform;

                    Txt_Hints = EF.Tool.Find<Text>(m_patchUpdater, "Txt_Hints");
                    Tran_Updater = EF.Tool.Find<RectTransform>(m_patchUpdater, "Tran_Updater");
                    Tran_HintsBox = EF.Tool.Find<RectTransform>(m_patchUpdater, "Tran_HintsBox");
                    Tran_MessgeBox = EF.Tool.Find<RectTransform>(m_patchUpdater, "Tran_MessgeBox");
                    EF.Tool.Find<Button>(m_patchUpdater, "Btn_True").RegisterInListAndBindEvent(OnClickBtn_True, ref m_AllButtons);
                    EF.Tool.Find<Button>(m_patchUpdater, "Btn_False").RegisterInListAndBindEvent(UpdateDone, ref m_AllButtons);
                }

                Txt_Hints.text = $"一共发现了{m_Downloader.TotalDownloadCount}个资源，总大小为{(int)(m_Downloader.TotalDownloadBytes / (1024f * 1024f))}mb需要更新,是否下载。";
            }
        }
        #endregion

        #region Download service pack.下载补丁包
        /// <summary>
        /// Download service pack.下载补丁包
        /// </summary>
        IEnumerator BeginDownload()
        {
            if (null == Txt_UpdaterTips)
            {
                Txt_UpdaterTips = EF.Tool.Find<Text>(m_patchUpdater, "Txt_UpdaterTips");
                Sld_UpdaterSlider = EF.Tool.Find<Slider>(m_patchUpdater, "Sld_UpdaterSlider");

                EF.Tool.Find<Button>(m_patchUpdater, "Btn_Done").RegisterInListAndBindEvent(UpdateDone, ref m_AllButtons);
            }

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
            Sld_UpdaterSlider.value = (float)currentDownloadBytes / totalDownloadBytes;

            string currentSizeMB = (currentDownloadBytes / 1048576f).ToString("f1");
            string totalSizeMB = (totalDownloadBytes / 1048576f).ToString("f1");
            Txt_UpdaterTips.text = $"{currentDownloadCount}/{totalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB";
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

        void OnClickBtn_True()
        {
            Tran_Updater.gameObject.SetActive(true);
            Tran_HintsBox.gameObject.SetActive(false);
            Run("BeginDownload");
        }
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
