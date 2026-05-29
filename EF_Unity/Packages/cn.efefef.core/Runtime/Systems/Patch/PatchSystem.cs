/*
 * ================================================
 * Describe:        This script is used to Update the StaticViersion file.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2022-10-19 10:31:31
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-05-25 17:39:33
 * ScriptVersion:   0.1
 * ===============================================
 */

using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using UnityEngine;
using YooAsset;

namespace EasyFramework.Systems.Patch
{
    /// <summary>
    /// 资源更新
    /// </summary>
    [Manager(Order = 99300)]
    public class PatchSystem : MonoSingleton<PatchSystem>, ISingleton
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
        public EPlayMode CurrentPlayMode { get; set; } = EPlayMode.EditorSimulateMode;

        private bool _isChecked;
        private bool _isNeedUpdate;
        private string _packageVersion;
        private ResourcePackage _package;

        void ISingleton.Init()
        {
            YooAssets.Initialize();
            IsUse = true;
        }

        void ISingleton.Quit()
        {
            IsUse = false;
            _package = null;
            YooAssets.Destroy();
        }

        /// <summary>
        /// 检查是否需要更新补丁
        /// <para>Check if a patch update is required</para>
        /// </summary>
        /// <param name="mode">Refresh scheme.<para>更新模式</para></param>
        /// <param name="packageName">The name of the package to update<para>要更新的包名</para></param>
        public async UniTask<bool> CheckForUpdatePatches(EPlayMode mode, string packageName = "DefaultPackage")
        {
            CurrentPlayMode = mode;
            PackageName = packageName;
            _package = YooAssets.TryGetPackage(packageName);
            if (_package == null)
            {
                _package = YooAssets.CreatePackage(packageName);
                YooAssets.SetDefaultPackage(_package);
            }

            _isChecked = true;
            if (_package.InitializeStatus == EOperationStatus.Succeed) return false;
            await CreateInitializeParameters();
            await GetRemotePackageVersionAsync();
            return await UpdatePackageManifestAsync() && _isNeedUpdate;
        }

        /// <summary>
        /// 开始更新补丁, 开始前请检查 "CurrentPlayMode" 字段是否设置正确, 当然你也可以先调用CheckForUpdatePatches函数
        /// <para>Start updating the patches. Before starting, please check whether the "CurrentPlayMode" field is set correctly.
        /// Of course, you can also call the CheckForUpdatePatches function first.</para>
        /// </summary>
        /// <param name="onStartDownloadFile">下载开始<para>Start download files.</para></param>
        /// <param name="onDownloadProgressUpdate">下载进度更新<para>Downloading Progress Update</para></param>
        /// <param name="onDownloadOver">下载结束<para>Download completed</para></param>
        /// <param name="onDownloadError">下载错误<para>Download Error</para></param>
        /// <returns></returns>
        public async UniTask<bool> StartUpdatePatches(
            DownloaderOperation.DownloadFileBegin onStartDownloadFile = null,
            DownloaderOperation.DownloadUpdate onDownloadProgressUpdate = null,
            DownloaderOperation.DownloaderFinish onDownloadOver = null,
            DownloaderOperation.DownloadError onDownloadError = null
        )
        {
            if (!_isChecked)
            {
                if (!await CheckForUpdatePatches(CurrentPlayMode)) return true;
            }

            var downloader = await CreateDownloader();

            if (downloader == null || downloader.TotalDownloadCount == 0)
                return true;

            downloader.DownloadFileBeginCallback = onStartDownloadFile;
            downloader.DownloadUpdateCallback = onDownloadProgressUpdate;
            downloader.DownloadFinishCallback = onDownloadOver;
            downloader.DownloadErrorCallback = onDownloadError;

            return await StartDownloader(downloader);
        }

        #region Setting config Initialize.初始化更新设置

        /// <summary>
        /// 根据 EPlayMode 创建初始化参数
        /// </summary>
        private async UniTask CreateInitializeParameters()
        {
            InitializeParameters initParameters = null;
            switch (CurrentPlayMode)
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

            if (initializationOperation.Status == EOperationStatus.Succeed) return;
            Error($"[YooAssetsManager] 资源包初始化失败: {initializationOperation.Error}");
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
                //获取成功
                string packageVersion = operation.PackageVersion;
                string key = $"{Application.productName}_RemotePackageVersion";
                string currentVersion = PlayerPrefs.GetString(key, string.Empty);
                _isNeedUpdate = !currentVersion.Equals(packageVersion);
                _packageVersion = packageVersion;
                PlayerPrefs.SetString(key, _packageVersion);
                //拿到版本号接下来去获取Manifest信息     GetManifestInfo
                return true;
            }

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
        public async UniTask<ResourceDownloaderOperation> CreateDownloader(string[] tags = null,
            int downloadMaxCount = 10, int failedTryAgain = 3)
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

            downloader.BeginDownload();
            await downloader.ToUniTask();

            return downloader.Status == EOperationStatus.Succeed;
        }

        #endregion

        private static void Error(string msg) => D.Error($"[ PatchSystem ] {msg}");

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