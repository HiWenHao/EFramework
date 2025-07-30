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
        /// <summary>
        /// The patch update flow.
        /// <para>补丁更新流程</para>
        /// </summary>
        enum EUpdateFlow
        {
            /// <summary> 初始化 </summary>
            Initialize,
            /// <summary> 获取版本 </summary>
            GetStaticVersion,
            /// <summary> 获取配置信息 </summary>
            GetManifestInfo,
            /// <summary> 创建下载 </summary>
            CreateDownloader,
            /// <summary> 开始下载 </summary>
            BeginDownload,
            /// <summary> 更新完成 </summary>
            Done
        }

        EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        Transform _patchUpdater;
        RectTransform _rectUpdater;
        RectTransform _rectHintsBox;
        RectTransform _tranMessgeBox;
        Text _txtHints;
        Text _txtUpdaterTips;
        Slider _sldUpdaterSlider;
        List<Button> _allButtons;

        string _packageVersion;
        ResourcePackage _package;
        Dictionary<string, bool> _cacheData;
        ResourceDownloaderOperation _downloader;

        Action _callback;
        IEnumerator _ieCurrentIE;
        Queue<IEnumerator> _queUupdaterState;

        void ISingleton.Init()
        {
            // 初始化资源系统
            YooAssets.Initialize();
            _queUupdaterState = new Queue<IEnumerator>();
        }

        void ISingleton.Quit()
        {
            if (null != _package)
            {
                //清空该包体的全部缓存
                //m_Package.ClearAllCacheFilesAsync();
                _package = null;
            }

            _downloader = null;

            _queUupdaterState.Clear();
            _queUupdaterState = null;
            _callback = null;
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
            _cacheData = new Dictionary<string, bool>(1000);
            PlayMode = (EPlayMode)mode;
            D.Emphasize($"资源系统运行模式：{mode}");
            _callback = callback;
            _package = YooAssets.TryGetPackage(packageName);
            if (_package == null)
            {
                // 创建默认的资源包
                _package = YooAssets.CreatePackage(packageName);

                //设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
                YooAssets.SetDefaultPackage(_package);
                EF.Load.AddResourcePackage(_package);
            }
            else
            {
                _package = YooAssets.GetPackage(packageName);
            }

            _queUupdaterState.Clear();
            if (_package.InitializeStatus != EOperationStatus.Succeed)
            {
                _queUupdaterState.Enqueue(Initialize());
                _queUupdaterState.Enqueue(GetStaticVersion());
                _queUupdaterState.Enqueue(GetManifest());
            }

            _queUupdaterState.Enqueue(CreateDownloader());
            _queUupdaterState.Enqueue(BeginDownload());

            Run(EUpdateFlow.Initialize);
        }

        #region Run progress. 跑更新流程
        void Run(EUpdateFlow nextFlow)
        {
            //D.Emphasize($"Next state is {nextFlow}       IEnumerator.Count = {m_que_updaterState.Count}");

            if (null != _ieCurrentIE)
                EF.StopCoroutines(_ieCurrentIE);
            if (nextFlow.Equals(EUpdateFlow.Done) || _queUupdaterState.Count <= 0)
            {
                _ieCurrentIE = null;
                if (_patchUpdater)
                    _tranMessgeBox.gameObject.SetActive(true);
                else
                    _callback?.Invoke();
                return;
            }

            _ieCurrentIE = _queUupdaterState.Dequeue();
            EF.StartCoroutines(_ieCurrentIE);
        }

        void UpdateDone()
        {
            _txtHints = null;
            _rectUpdater = null;
            _rectHintsBox = null;
            _tranMessgeBox = null;
            _txtUpdaterTips = null;
            _sldUpdaterSlider = null;

            _allButtons.ReleaseAndRemoveEvent();
            _allButtons = null;

            Object.Destroy(_patchUpdater.gameObject);
            _patchUpdater = null;
            _callback?.Invoke();

            _cacheData.Clear();
            _cacheData = null;
            _callback = null;
            _package = null;
        }
        #endregion

        #region Setting config Initialize.初始化更新设置
        IEnumerator Initialize()
        {
            InitializeParameters initParameters = null;
            switch (PlayMode)
            {
                case EPlayMode.EditorSimulateMode:
                    var _simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(EDefaultBuildPipeline.BuiltinBuildPipeline, _package.PackageName);
                    initParameters = new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(_simulateBuildResult)
                    };
                    break;
                case EPlayMode.OfflinePlayMode:
                    initParameters = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    break;
                case EPlayMode.HostPlayMode:
                    initParameters = new HostPlayModeParameters
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
                    initParameters = new WebPlayModeParameters
                    {

#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
                        IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                        createParameters.WebFileSystemParameters = WechatFileSystemCreater.CreateWechatFileSystemParameters(remoteServices);
#else
                        WebFileSystemParameters = FileSystemParameters.CreateDefaultWebFileSystemParameters()
#endif
                    };
                    break;
            }

            InitializationOperation initializationOperation = _package.InitializeAsync(initParameters);
            yield return initializationOperation;

            if (initializationOperation.Status != EOperationStatus.Succeed)
            {
                D.Error(initializationOperation.Error);
            }
            else
            {
                Run(EUpdateFlow.GetStaticVersion);
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

            public byte[] ReadFileData(DecryptFileInfo fileInfo)
            {
                throw new NotImplementedException();
            }

            public string ReadFileText(DecryptFileInfo fileInfo)
            {
                throw new NotImplementedException();
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

            public byte[] ReadFileData(DecryptFileInfo fileInfo)
            {
                throw new NotImplementedException();
            }

            public string ReadFileText(DecryptFileInfo fileInfo)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region Update the StaticViersion file.更新静态版本文件
        /// <summary>
        /// Update the StaticViersion file.更新静态版本文件
        /// </summary>
        IEnumerator GetStaticVersion()
        {
            var operation = _package.RequestPackageVersionAsync();
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                //更新成功
                string packageVersion = operation.PackageVersion;
                _packageVersion = packageVersion;
                D.Log($"Updated package Version : {packageVersion}");

                //拿到版本号接下来去获取Manifest信息     GetManifestInfo
                Run(EUpdateFlow.GetManifestInfo);
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
            var operation = _package.UpdatePackageManifestAsync(_packageVersion);
            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                //拿到配置信息接下来去获取热更资源
                Run(EUpdateFlow.CreateDownloader);
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
            _downloader = _package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

            yield return null;

            if (_downloader.TotalDownloadCount == 0)
            {
                Run(EUpdateFlow.Done);
            }
            else
            {
                if (null == _patchUpdater)
                {
                    _patchUpdater = Object.Instantiate(EF.Load.LoadInResources<GameObject>(EF.Projects.AppConst.UIPrefabsPath + "PatchUpdater")).transform;

                    _txtHints = EF.Tool.Find<Text>(_patchUpdater, "Txt_Hints");
                    _rectUpdater = EF.Tool.Find<RectTransform>(_patchUpdater, "Tran_Updater");
                    _rectHintsBox = EF.Tool.Find<RectTransform>(_patchUpdater, "Tran_HintsBox");
                    _tranMessgeBox = EF.Tool.Find<RectTransform>(_patchUpdater, "Tran_MessgeBox");
                    EF.Tool.Find<Button>(_patchUpdater, "Btn_True").RegisterInListAndBindEvent(OnClickBtn_True, ref _allButtons);
                    EF.Tool.Find<Button>(_patchUpdater, "Btn_False").RegisterInListAndBindEvent(UpdateDone, ref _allButtons);
                }

                _txtHints.text = $"一共发现了{_downloader.TotalDownloadCount}个资源，总大小为{(int)(_downloader.TotalDownloadBytes / (1024f * 1024f))}mb需要更新,是否下载。";
            }
        }
        #endregion

        #region Download service pack.下载补丁包
        /// <summary>
        /// Download service pack.下载补丁包
        /// </summary>
        IEnumerator BeginDownload()
        {
            if (null == _txtUpdaterTips)
            {
                _txtUpdaterTips = EF.Tool.Find<Text>(_patchUpdater, "Txt_UpdaterTips");
                _sldUpdaterSlider = EF.Tool.Find<Slider>(_patchUpdater, "Sld_UpdaterSlider");

                EF.Tool.Find<Button>(_patchUpdater, "Btn_Done").RegisterInListAndBindEvent(UpdateDone, ref _allButtons);
            }

            //注册下载回调
            _downloader.OnDownloadErrorCallback = OnDownloadErrorFunction;
            _downloader.OnDownloadProgressCallback = OnDownloadProgressUpdateFunction;
            _downloader.OnDownloadOverCallback = OnDownloadOverFunction;
            _downloader.OnStartDownloadFileCallback = OnStartDownloadFileFunction;

            //开启下载
            _downloader.BeginDownload();
            yield return _downloader;

            // 检测下载结果
            if (_downloader.Status != EOperationStatus.Succeed)
                yield break;

            Run(EUpdateFlow.Done);
        }

        private void OnDownloadErrorFunction(string fileName, string error)
        {
            D.Error($"Download the file failed. The file name is {fileName} ,  Error info is {error}");
        }
        private void OnDownloadProgressUpdateFunction(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes, long currentDownloadBytes)
        {
            _sldUpdaterSlider.value = (float)currentDownloadBytes / totalDownloadBytes;

            string currentSizeMB = (currentDownloadBytes / 1048576f).ToString("f1");
            string totalSizeMB = (totalDownloadBytes / 1048576f).ToString("f1");
            _txtUpdaterTips.text = $"{currentDownloadCount}/{totalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB";
        }
        private void OnStartDownloadFileFunction(string fileName, long sizeBytes)
        {
            D.Error("当前下载：" + fileName + "   大小为： " + sizeBytes);
        }
        private void OnDownloadOverFunction(bool isSucceed)
        {
            D.Log("下载完成，结果为：" + isSucceed);
        }
        #endregion

        void OnClickBtn_True()
        {
            _rectUpdater.gameObject.SetActive(true);
            _rectHintsBox.gameObject.SetActive(false);
            Run(EUpdateFlow.BeginDownload);
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
