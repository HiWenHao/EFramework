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

using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using EasyFramework.Systems.Ui;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;
using Object = UnityEngine.Object;

namespace EasyFramework.Systems.Patch
{
    /// <summary>
    /// 资源更新
    /// </summary>
    [Manager]
    public class PatchSystem : Singleton<PatchSystem>, ISingleton
    {
        /// <summary>
        /// 是否使用Yoo
        /// </summary>
        public bool IsUse { get; private set; }
        
        /// <summary>
        /// 当前被更新的包名
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// 启用可寻址资源定位
        /// </summary>
        public bool EnableAddressable => _package.GetPackageDetails().EnableAddressable;
        
        /// 当前运行模式
        private EPlayMode _playMode = EPlayMode.EditorSimulateMode;

        private bool _openDebug;
        private bool _needDownloading;
        private bool _waitDownloading;
        private Text _txtUpdaterTips;
        private Slider _sldUpdaterSlider;

        private string _packageVersion;
        private ResourcePackage _package;

        void ISingleton.Init()
        {
            YooAssets.Initialize();
            IsUse = true;
            _openDebug = false;
        }

        void ISingleton.Quit()
        {
            IsUse = false;
            _package = null;
            YooAssets.Destroy();
        }

        /// <summary>
        /// Start update patch.
        /// <para>开始更新补丁</para>
        /// </summary>
        /// <param name="mode">Refresh scheme.<para>更新模式</para></param>
        /// <param name="packageName">The name of the package to update<para>要更新的包名</para></param>
        /// <param name="autoStart">Auto start the downloading.<para>自动开始下载</para></param>
        public async UniTask StartUpdatePatch(EPlayMode mode, string packageName = "DefaultPackage", bool autoStart = true)
        {
            Log($"资源系统运行模式：{mode}");
            _playMode = mode;
            PackageName = packageName;
            _package = YooAssets.TryGetPackage(packageName);
            if (_package == null)
            {
                _package = YooAssets.CreatePackage(packageName);
                YooAssets.SetDefaultPackage(_package);
            }

            if (_package.InitializeStatus != EOperationStatus.Succeed)
            {
                await CreateInitializeParameters();
                await GetRemotePackageVersionAsync();
                await UpdatePackageManifestAsync();
            }

            if (!autoStart)
                return;
            
            var downloader = await CreateDownloader();
            await BeforeDownloading(downloader);
            while (_waitDownloading)
            {
                await UniTask.Yield();
            }
            if (_needDownloading)
                await StartDownloader(downloader);
            UpdateDone();
        }

        #region Setting config Initialize.初始化更新设置

        /// <summary>
        /// 根据 EPlayMode 创建初始化参数
        /// </summary>
        private async UniTask<bool> CreateInitializeParameters()
        {
            InitializeParameters initParameters = null;
            switch (_playMode)
            {
                // 编辑器下的模拟模式
                case EPlayMode.EditorSimulateMode:
                    var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(_package.PackageName);
                    initParameters = new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters =
                            FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult
                                .PackageRootDirectory)
                    };
                    break;
                // 单机运行模式
                case EPlayMode.OfflinePlayMode:
                    initParameters = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    break;
                // 联机运行模式
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
                // WebGL运行模式
                case EPlayMode.WebPlayMode:
                    initParameters = new WebPlayModeParameters
                    {
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
                        //注意：如果有子目录，请修改此处！
                        WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(
                            $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE", 
                            new RemoteServices(
                                EF.Projects.ResourcesArea.InnerUrl,
                                EF.Projects.ResourcesArea.StandbyUrl
                            )
                        );
#else
                        WebServerFileSystemParameters =
                            FileSystemParameters.CreateDefaultWebServerFileSystemParameters()
#endif
                    };
                    break;
            }

            InitializationOperation initializationOperation = _package.InitializeAsync(initParameters);
            await initializationOperation.ToUniTask();

            if (initializationOperation.Status == EOperationStatus.Succeed)
            {
                Log($"[YooAssetsManager] 资源包初始化成功，运行模式: {_playMode}");
                return true;
            }

            Error($"[YooAssetsManager] 资源包初始化失败: {initializationOperation.Error}");
            return false;
        }

        #endregion

        #region Update the StaticViersion file.更新静态版本文件

        /// <summary>
        /// Update the StaticVersion file. 获取远程资源版本
        /// </summary>
        private async UniTask<bool> GetRemotePackageVersionAsync()
        {
            var operation = _package.RequestPackageVersionAsync(false);
            await operation.ToUniTask();
            if (operation.Status == EOperationStatus.Succeed)
            {
                //更新成功
                string packageVersion = operation.PackageVersion;
                _packageVersion = packageVersion;
                Log($"Updated package Version : {packageVersion}");

                //拿到版本号接下来去获取Manifest信息     GetManifestInfo
                return true;
            }

            //更新失败
            Error($"Get the StaticVersion file error: {operation.Error}");
            return false;
        }

        #endregion

        #region Update the GetManifest file.更新配置文件清单

        /// <summary>
        /// Update the Manifest file.更新配置文件清单
        /// </summary>
        private async UniTask<bool> UpdatePackageManifestAsync()
        {
            var operation = _package.UpdatePackageManifestAsync(_packageVersion);
            await operation.ToUniTask();
            if (operation.Status == EOperationStatus.Succeed)
            {
                //拿到配置信息接下来去获取热更资源
                Log("Get the Manifest file succeed...");
                return true;
            }

            //更新失败
            Error($"Get the Manifest file error: {operation.Error}");
            return false;
        }

        #endregion

        #region Create one downloader.创建一个下载器
        
        /// <summary>
        /// Create a downloader.创建一个下载器
        /// </summary>
        /// <param name="tags">资源标签列表</param>
        /// <param name="downloadMaxCount">同时最大下载数</param>
        /// <param name="failedTryAgain">失败后的再次下载尝试次数</param>
        /// <returns>下载器</returns>
        public async UniTask<ResourceDownloaderOperation> CreateDownloader(string[] tags = null, int downloadMaxCount = 10, int failedTryAgain = 3)
        {
            await UniTask.CompletedTask;
            
            var downloader = null == tags
                ? _package.CreateResourceDownloader(downloadMaxCount, failedTryAgain)
                : _package.CreateResourceDownloader(tags, downloadMaxCount, failedTryAgain);

            return downloader;
        }
        
        #endregion

        #region Start download.开始下载
        
        /// <summary>
        /// Download service pack.下载补丁包
        /// </summary>
        /// <param name="downloader">下载器</param>
        /// <returns>下载完成结果通知</returns>
        public async UniTask<bool> StartDownloader(ResourceDownloaderOperation downloader)
        {
            await UniTask.CompletedTask;
            
            if (downloader.TotalDownloadCount == 0)
                return false;

            Log($"Start download, this download is for: {downloader.TotalDownloadCount}");
            downloader.BeginDownload();
            await downloader.ToUniTask();

            Log($"Downloading is over, this downloader status is {downloader.Status}");
            return downloader.Status == EOperationStatus.Succeed;
        }
        
        #endregion

        #region Private function - 内部调用

        private async UniTask BeforeDownloading(ResourceDownloaderOperation downloader)
        {
            await UniTask.CompletedTask;
            if (downloader == null || downloader.TotalDownloadCount == 0)
                return;
            
            _waitDownloading = true;
            downloader.DownloadErrorCallback = OnDownloadErrorFunction;
            downloader.DownloadUpdateCallback = OnDownloadProgressUpdateFunction;
            downloader.DownloadFinishCallback = OnDownloadOverFunction;
            downloader.DownloadFileBeginCallback = OnStartDownloadFileFunction;
            
            EF.Ui.ShowTipsView(
                $"一共发现了{downloader.TotalDownloadCount}个资源，总大小为{downloader.TotalDownloadBytes / 1048576f:F1}mb需要更新,是否下载。",
                new TipsViewExtraData()
                {
                    ConfirmName = "下载",
                    CancelName = "取消",
                    ConfirmCallBack = OnClickDownloadBegin,
                    CancelCallBack = UpdateDone
                }).Forget();
        }

        /// <summary>
        /// 当点击开始下载
        /// </summary>
        void OnClickDownloadBegin()
        {
            Transform patchUpdater = Object
                .Instantiate(Resources.Load<GameObject>(EF.Projects.AppConst.UIPrefabsPath + "PatchUpdater")).transform;
            Canvas canvas = patchUpdater.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = EF.Ui.UICamera;
            _txtUpdaterTips = EF.Tool.Find<Text>(patchUpdater, "Txt_UpdaterTips");
            _sldUpdaterSlider = EF.Tool.Find<Slider>(patchUpdater, "Sld_UpdaterSlider");
            _waitDownloading = false;
            _needDownloading = true;
        }
        
        /// <summary>
        /// 更新取消或完成
        /// </summary>
        void UpdateDone()
        {
            if (null != _sldUpdaterSlider)
            {
                Object.Destroy(_sldUpdaterSlider.transform.parent.parent.gameObject);
                _txtUpdaterTips = null;
                _sldUpdaterSlider = null;
            }
            _needDownloading = false;
            _waitDownloading = false;
            
            _package = null;
        }
        
        private void OnDownloadErrorFunction(DownloadErrorData errorData)
        {
            Error(
                $"Download the file failed. The file name is {errorData.FileName} ,  Error info is {errorData.ErrorInfo}");
        }
        private void OnDownloadProgressUpdateFunction(DownloadUpdateData downloadData)
        {
            _sldUpdaterSlider.value = (float)downloadData.CurrentDownloadBytes / downloadData.TotalDownloadBytes;

            string currentSizeMb = (downloadData.CurrentDownloadBytes / 1048576f).ToString("f1");
            string totalSizeMb = (downloadData.TotalDownloadBytes / 1048576f).ToString("f1");
            _txtUpdaterTips.text =
                $"{downloadData.CurrentDownloadCount}/{downloadData.TotalDownloadCount} {currentSizeMb}MB/{totalSizeMb}MB";
        }
        private void OnStartDownloadFileFunction(DownloadFileData fileData)
        {
            Log($"当前下载：{fileData.FileName}, 大小为：{fileData.FileSize}");
        }
        private void OnDownloadOverFunction(DownloaderFinishData finishData)
        {
            Log($"{finishData.PackageName}下载完成，结果为：{finishData.Succeed}");
        }
        
        #endregion
        
        private void Log(string msg)
        {
            if (_openDebug) D.Log(msg);
        }
        private void Error(string msg)
        {
            if (_openDebug) D.Error(msg);
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
    }
}