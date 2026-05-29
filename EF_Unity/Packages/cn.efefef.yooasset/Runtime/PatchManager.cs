/*
 * ================================================
 * Describe:        用来处理补丁热更
 * Author:          Alvin8412
 * CreationTime:    2026-05-29 15:03:04
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-05-29 15:03:04
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace EasyFramework.Managers.Assets
{
    /// <summary>
    /// 补丁资源更新管理器
    /// <para>Patch resource update manager</para>
    /// </summary>
    [Manager(Order = 99300)]
    public class PatchManager : MonoSingleton<PatchManager>, ISingleton
    {
        /// <summary>
        /// 是否使用Yoo
        /// <para>Yoo is in use.</para>
        /// </summary>
        public bool IsUse { get; private set; }

        /// <summary>
        /// 当前被更新的包名
        /// <para>The name of the package currently being updated</para>
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// 启用可寻址资源定位
        /// <para>Enable addressable resource location</para>
        /// </summary>
        public bool EnableAddressable => _package != null
            ? _package.GetPackageDetails().EnableAddressable
            : GetPackageEnableAddressable("DefaultPackage");

        /// <summary>
        /// 当前运行模式
        /// <para>Current runtime play mode</para>
        /// </summary>
        public EPlayMode CurrentPlayMode { get; set; } = EPlayMode.EditorSimulateMode;

        private bool _isChecked;            // 是否已完成更新检查
        private bool _isNeedUpdate;         // 是否需要更新
        private string _packageVersion;     // 远程资源版本号
        private ResourcePackage _package;   // 单包模式下的 ResourcePackage 实例

        private List<SubPackageConfig> _registeredConfigs;          // 已注册的子包配置列表
        private Dictionary<string, SubPackageInfo> _subPackages;    // 已注册的子包信息字典（key = 包名）
        private string VersionDir => Path.Combine(Application.persistentDataPath, "yoo_versions"); // 本地版本文件存储目录

        void ISingleton.Init()
        {
            YooAssets.Initialize();
            _subPackages = new Dictionary<string, SubPackageInfo>();
            IsUse = true;
        }

        void ISingleton.Quit()
        {
            IsUse = false;
            _package = null;
            _subPackages.Clear();
            _registeredConfigs = null;
            YooAssets.Destroy();
        }

        #region [分包] 子包注册 & 查询

        /// <summary>
        /// [分包] 注册所有子包<br/>
        /// 在启动流程最前面调用一次，定义要管理的所有包
        /// <para>[Sub-package] Register all sub-packages.<br/>
        /// Called once at the beginning of the startup flow to define all packages to manage.</para>
        /// </summary>
        /// <param name="configs">子包配置列表，至少包含一个核心包<para>Sub-package config list, must contain at least one essential package</para></param>
        public void RegisterSubPackages(List<SubPackageConfig> configs)
        {
            _registeredConfigs = configs;
            _subPackages.Clear();

            foreach (var cfg in configs)
            {
                var pkg = YooAssets.TryGetPackage(cfg.PackageName) ?? YooAssets.CreatePackage(cfg.PackageName);
                _subPackages[cfg.PackageName] = new SubPackageInfo
                {
                    Config = cfg,
                    Package = pkg,
                    LocalVersion = LoadLocalVersion(cfg.PackageName)
                };
            }

            if (_subPackages.Count != 1) return;
            ResourcePackage defaultPkg = null;
            foreach (var kv in _subPackages)
            {
                defaultPkg = kv.Value.Package;
                break;
            }

            YooAssets.SetDefaultPackage(defaultPkg);
        }

        /// <summary>
        /// [分包] 获取已注册的子包信息
        /// <para>[Sub-package] Get the registered sub-package info by name</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <returns>子包信息，未找到则返回 null<para>Sub-package info, or null if not found</para></returns>
        public SubPackageInfo GetSubPackage(string packageName)
        {
            if (_subPackages == null) return null;
            _subPackages.TryGetValue(packageName, out var info);
            return info;
        }

        /// <summary>
        /// [分包] 查询指定包的寻址开关
        /// <para>[Sub-package] Check whether the specified package has addressable mode enabled</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name</para></param>
        public bool GetPackageEnableAddressable(string packageName)
        {
            var info = GetSubPackage(packageName);
            return info?.Package != null && info.Package.GetPackageDetails().EnableAddressable;
        }

        /// <summary>
        /// [分包] 获取指定包的 ResourcePackage 实例
        /// <para>[Sub-package] Get the ResourcePackage instance by name</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <returns>ResourcePackage 实例，未注册时回退到单包 _package<para>ResourcePackage instance, falls back to the single-package instance if not registered</para></returns>
        public ResourcePackage GetPackage(string packageName)
        {
            var info = GetSubPackage(packageName);
            return info?.Package ?? _package;
        }

        /// <summary>
        /// [分包] 获取所有已注册的子包配置列表（只读）
        /// <para>[Sub-package] Get all registered sub-package configs (read-only)</para>
        /// </summary>
        public List<SubPackageConfig> GetRegisteredConfigs() => _registeredConfigs;

        #endregion

        #region [分包] 子包更新

        /// <summary>
        /// [分包] 批量检查所有已注册包的更新<br/>
        /// 只请求版本号、更新 Manifest，不下发下载
        /// <para>[Sub-package] Check all registered packages for updates.<br/>
        /// Only requests version numbers and updates manifests, does not trigger downloads.</para>
        /// </summary>
        /// <param name="mode">运行模式<para>Play mode</para></param>
        /// <returns>是否有任何一个包需要更新<para>Whether any package needs an update</para></returns>
        public async UniTask<bool> CheckAllPackagesForUpdate(EPlayMode mode)
        {
            CurrentPlayMode = mode;

            if (_registeredConfigs == null || _registeredConfigs.Count == 0)
            {
                Error("未注册任何子包，请先调用 RegisterSubPackages()");
                return false;
            }

            bool anyNeedUpdate = false;

            foreach (var cfg in _registeredConfigs)
            {
                var result = await CheckSubPackageUpdate(cfg);
                if (result) anyNeedUpdate = true;
            }

            return anyNeedUpdate;
        }

        /// <summary>
        /// [分包] 检查单个子包的更新<br/>
        /// 依次执行：初始化 → 请求远端版本 → 版本对比 → 更新 Manifest
        /// </summary>
        private async UniTask<bool> CheckSubPackageUpdate(SubPackageConfig cfg)
        {
            var info = _subPackages[cfg.PackageName];
            var pkg = info.Package;

            if (pkg.InitializeStatus == EOperationStatus.Succeed)
            {
                info.Checked = true;
                info.NeedUpdate = false;
                return false;
            }

            await InitializeSubPackage(pkg, cfg);
            var versionOp = pkg.RequestPackageVersionAsync(appendTimeTicks: true);
            await versionOp.ToUniTask();

            if (versionOp.Status != EOperationStatus.Succeed)
            {
                Error($"[{cfg.PackageName}] 获取远程版本失败: {versionOp.Error}");
                return false;
            }

            info.RemoteVersion = versionOp.PackageVersion;
            info.Checked = true;
            info.NeedUpdate = info.LocalVersion != info.RemoteVersion;

            if (!info.NeedUpdate)
                return false;

            var manifestOp = pkg.UpdatePackageManifestAsync(info.RemoteVersion);
            await manifestOp.ToUniTask();

            if (manifestOp.Status != EOperationStatus.Succeed)
            {
                Error($"[{cfg.PackageName}] 更新 Manifest 失败: {manifestOp.Error}");
                info.NeedUpdate = false;
                return false;
            }

            D.Log($"[PatchSystem] {cfg.PackageName} 检测到更新: {info.LocalVersion} → {info.RemoteVersion}");
            return true;
        }

        /// <summary>
        /// [分包] 根据运行模式 + 子包配置，创建初始化参数并初始化包
        /// </summary>
        private async UniTask InitializeSubPackage(ResourcePackage pkg, SubPackageConfig cfg)
        {
            InitializeParameters initParams = null;

            // 解析远程地址：优先子包配置，否则回落 ProjectConfig 全局地址
            string mainUrl = string.IsNullOrEmpty(cfg.RemoteMainUrl)
                ? EF.Projects.ResourcesArea.InnerUrl
                : cfg.RemoteMainUrl;
            string fallbackUrl = string.IsNullOrEmpty(cfg.RemoteFallbackUrl)
                ? EF.Projects.ResourcesArea.StandbyUrl
                : cfg.RemoteFallbackUrl;

            switch (CurrentPlayMode)
            {
                case EPlayMode.EditorSimulateMode:
                    var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(pkg.PackageName);
                    initParams = new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters =
                            FileSystemParameters.CreateDefaultEditorFileSystemParameters(
                                simulateBuildResult.PackageRootDirectory)
                    };
                    break;

                case EPlayMode.OfflinePlayMode:
                    initParams = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters =
                            FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    break;

                case EPlayMode.HostPlayMode:
                    initParams = new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters =
                            FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                        CacheFileSystemParameters =
                            FileSystemParameters.CreateDefaultCacheFileSystemParameters(
                                new RemoteServices(mainUrl, fallbackUrl))
                    };
                    break;

                case EPlayMode.WebPlayMode:
                    initParams = new WebPlayModeParameters
                    {
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
                        WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(
                            $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE",
                            new RemoteServices(mainUrl, fallbackUrl));
#else
                        WebServerFileSystemParameters =
                            FileSystemParameters.CreateDefaultWebServerFileSystemParameters()
#endif
                    };
                    break;
            }

            var initOp = pkg.InitializeAsync(initParams);
            await initOp.ToUniTask();

            if (initOp.Status != EOperationStatus.Succeed)
                Error($"[{cfg.PackageName}] 初始化失败: {initOp.Error}");
        }

        #endregion

        #region [分包] 下载核心包

        /// <summary>
        /// [分包] 下载所有核心包（IsEssential = true）<br/>
        /// 核心包在主流程中按序下载，非核心包用 DownloadPackageOnDemand 按需触发
        /// <para>[Sub-package] Download all essential packages (IsEssential = true).<br/>
        /// Essential packages are downloaded sequentially in the main flow;
        /// non-essential packages use DownloadPackageOnDemand for on-demand triggering.</para>
        /// </summary>
        /// <param name="onStart">下载开始回调<para>Download start callback</para></param>
        /// <param name="onProgress">下载进度回调<para>Download progress callback</para></param>
        /// <param name="onFinish">下载完成回调<para>Download finish callback</para></param>
        /// <param name="onError">下载错误回调<para>Download error callback</para></param>
        /// <returns>所有核心包是否下载成功<para>Whether all essential packages downloaded successfully</para></returns>
        public async UniTask<bool> DownloadEssentialPackages(
            DownloaderOperation.DownloadFileBegin onStart = null,
            DownloaderOperation.DownloadUpdate onProgress = null,
            DownloaderOperation.DownloaderFinish onFinish = null,
            DownloaderOperation.DownloadError onError = null)
        {
            if (_registeredConfigs == null)
            {
                Error("未注册子包");
                return false;
            }

            foreach (var cfg in _registeredConfigs)
            {
                if (!cfg.IsEssential) continue;

                var info = _subPackages[cfg.PackageName];
                if (!info.Checked)
                {
                    // 还没有检查过，先检查
                    await CheckSubPackageUpdate(cfg);
                }

                if (!info.NeedUpdate) continue;

                bool success = await DownloadSubPackage(info, onStart, onProgress, onFinish, onError);
                if (!success)
                    return false;

                // 下载成功后保存版本
                SaveLocalVersion(cfg.PackageName, info.RemoteVersion);
                info.LocalVersion = info.RemoteVersion;
            }

            return true;
        }

        /// <summary>
        /// [分包] 按需下载指定包<br/>
        /// 用于非核心包、DLC、关卡等动态触发的下载
        /// <para>[Sub-package] Download a specific package on demand.<br/>
        /// Used for dynamically triggered downloads such as non-essential packages, DLCs, and levels.</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <param name="tags">资源标签过滤（null=该包全部资源）<para>Resource tag filter (null = all resources in this package)</para></param>
        /// <param name="onStart">下载开始回调<para>Download start callback</para></param>
        /// <param name="onProgress">下载进度回调<para>Download progress callback</para></param>
        /// <param name="onFinish">下载完成回调<para>Download finish callback</para></param>
        /// <param name="onError">下载错误回调<para>Download error callback</para></param>
        /// <returns>是否下载成功<para>Whether the download succeeded</para></returns>
        public async UniTask<bool> DownloadPackageOnDemand(
            string packageName,
            string[] tags = null,
            DownloaderOperation.DownloadFileBegin onStart = null,
            DownloaderOperation.DownloadUpdate onProgress = null,
            DownloaderOperation.DownloaderFinish onFinish = null,
            DownloaderOperation.DownloadError onError = null)
        {
            var info = GetSubPackage(packageName);
            if (info == null)
            {
                Error($"未找到子包: {packageName}");
                return false;
            }

            if (!info.Checked)
                await CheckSubPackageUpdate(info.Config);

            if (!info.NeedUpdate) return true;

            bool success = await DownloadSubPackage(info, onStart, onProgress, onFinish, onError, tags);
            if (success)
            {
                SaveLocalVersion(packageName, info.RemoteVersion);
                info.LocalVersion = info.RemoteVersion;
            }

            return success;
        }

        /// <summary>
        /// [分包] 创建下载器并开始下载单个子包
        /// </summary>
        private async UniTask<bool> DownloadSubPackage(
            SubPackageInfo info,
            DownloaderOperation.DownloadFileBegin onStart,
            DownloaderOperation.DownloadUpdate onProgress,
            DownloaderOperation.DownloaderFinish onFinish,
            DownloaderOperation.DownloadError onError,
            string[] tags = null)
        {
            var pkg = info.Package;
            // 按标签过滤创建下载器，无标签则下载全部
            var downloader = tags != null
                ? pkg.CreateResourceDownloader(tags, 10, 3)
                : pkg.CreateResourceDownloader(10, 3);

            if (downloader.TotalDownloadCount == 0)
            {
                // 没有需要下载的文件，版本由外层调用方保存
                return true;
            }

            // 绑定下载回调
            downloader.DownloadFileBeginCallback = onStart;
            downloader.DownloadUpdateCallback = onProgress;
            downloader.DownloadFinishCallback = onFinish;
            downloader.DownloadErrorCallback = onError;

            downloader.BeginDownload();
            await downloader.ToUniTask();

            return downloader.Status == EOperationStatus.Succeed;
        }

        #endregion

        #region [分包] 版本持久化

        /// <summary>
        /// 读取本地缓存的包版本
        /// <para>Read the locally cached package version</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <returns>版本字符串，未缓存则返回空<para>Version string, or empty if not cached</para></returns>
        public string LoadLocalVersion(string packageName)
        {
            var path = GetVersionPath(packageName);
            try
            {
                if (File.Exists(path))
                    return File.ReadAllText(path).Trim();
            }
            catch (Exception e)
            {
                D.Warning($"[PatchSystem] 读取版本文件失败: {path}, {e.Message}");
            }

            return string.Empty;
        }

        /// <summary>
        /// 保存包版本到本地
        /// <para>Save the package version to local storage</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name</para></param>
        /// <param name="version">版本号<para>Version string</para></param>
        public void SaveLocalVersion(string packageName, string version)
        {
            try
            {
                if (!Directory.Exists(VersionDir))
                    Directory.CreateDirectory(VersionDir);
                File.WriteAllText(GetVersionPath(packageName), version);
            }
            catch (Exception e)
            {
                D.Warning($"[PatchSystem] 保存版本文件失败: {GetVersionPath(packageName)}, {e.Message}");
            }
        }

        /// <summary>
        /// 清除某个包的版本缓存（下次启动强制重新下载）
        /// <para>Clear a package's version cache (forces re-download on next launch)</para>
        /// </summary>
        /// <param name="packageName">包名<para>Package name</para></param>
        public void ClearLocalVersion(string packageName)
        {
            var path = GetVersionPath(packageName);
            if (File.Exists(path))
                File.Delete(path);
        }

        // 获取版本文件路径（persistentDataPath/yoo_versions/{packageName}_version.txt）
        private string GetVersionPath(string packageName) => Path.Combine(VersionDir, $"{packageName}_version.txt");

        #endregion

        #region 单包模式

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
            if (_package.InitializeStatus != EOperationStatus.Succeed)
                await CreateInitializeParameters(_package);
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

        #endregion

        #region Setting config Initialize.初始化更新设置

        /// <summary>
        /// 单包模式：根据 EPlayMode 创建初始化参数并执行包初始化
        /// </summary>
        private async UniTask CreateInitializeParameters(ResourcePackage targetPackage = null)
        {
            // 优先使用传入的 package，否则回退到单包 _package
            var pkg = targetPackage ?? _package;
            InitializeParameters initParameters = null;
            switch (CurrentPlayMode)
            {
                // 编辑器模拟模式
                case EPlayMode.EditorSimulateMode:
                    var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(pkg.PackageName);
                    initParameters = new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters =
                            FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult
                                .PackageRootDirectory)
                    };
                    break;
                // 单机离线模式
                case EPlayMode.OfflinePlayMode:
                    initParameters = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    break;
                // 联机热更模式
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
                // WebGL 模式
                case EPlayMode.WebPlayMode:
                    initParameters = new WebPlayModeParameters
                    {
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
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

            InitializationOperation initializationOperation = pkg.InitializeAsync(initParameters);
            await initializationOperation.ToUniTask();

            if (initializationOperation.Status == EOperationStatus.Succeed) return;
            Error($"[YooAssetsManager] 资源包初始化失败: {initializationOperation.Error}");
        }

        #endregion

        #region Update the StaticViersion file.更新静态版本文件

        /// <summary>
        /// 单包模式：请求远程版本号并与本地缓存对比
        /// </summary>
        private async UniTask<bool> GetRemotePackageVersionAsync()
        {
            var operation = _package.RequestPackageVersionAsync(false);
            await operation.ToUniTask();
            if (operation.Status == EOperationStatus.Succeed)
            {
                // 获取远程版本号并与本地文件缓存的版本对比
                string packageVersion = operation.PackageVersion;
                string localVersion = LoadLocalVersion(PackageName);
                _isNeedUpdate = localVersion != packageVersion;
                _packageVersion = packageVersion;
                return true;
            }

            Error($"Get the StaticVersion file error: {operation.Error}");
            return false;
        }

        #endregion

        #region Update the GetManifest file.更新配置文件清单

        /// <summary>
        /// 单包模式：下载并更新 Manifest，成功后保存版本到本地
        /// </summary>
        private async UniTask<bool> UpdatePackageManifestAsync()
        {
            var operation = _package.UpdatePackageManifestAsync(_packageVersion);
            await operation.ToUniTask();
            if (operation.Status == EOperationStatus.Succeed)
            {
                // Manifest 更新成功后保存版本
                SaveLocalVersion(PackageName, _packageVersion);
                return true;
            }

            Error($"Get the Manifest file error: {operation.Error}");
            return false;
        }

        #endregion

        #region Create one downloader.创建一个下载器

        /// <summary>
        /// Create a downloader. 创建一个下载器
        /// <para>Create a resource downloader for the current package</para>
        /// </summary>
        /// <param name="tags">资源标签列表<para>Resource tag list</para></param>
        /// <param name="downloadMaxCount">同时最大下载数<para>Max concurrent download count</para></param>
        /// <param name="failedTryAgain">失败后的再次下载尝试次数<para>Retry count on failure</para></param>
        /// <returns>下载器<para>Resource downloader operation</para></returns>
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
        /// Download service pack. 下载补丁包
        /// <para>Start downloading patch resources using the given downloader</para>
        /// </summary>
        /// <param name="downloader">下载器<para>Resource downloader operation</para></param>
        /// <returns>下载完成结果通知<para>Whether the download succeeded</para></returns>
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

        // 输出错误日志，统一前缀 [ PatchSystem ]
        private static void Error(string msg) => D.Error($"[ PatchSystem ] {msg}");
    }
}