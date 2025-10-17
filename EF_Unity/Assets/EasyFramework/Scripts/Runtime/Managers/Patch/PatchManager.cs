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
using UnityEngine;
using UnityEngine.UI;
using YooAsset;
using Object = UnityEngine.Object;

namespace EasyFramework.Managers
{
    /// <summary>
    /// 资源更新
    /// </summary>
    public class PatchManager : Singleton<PatchManager>, ISingleton
    {
        /// <summary>
        /// 是否使用Yoo
        /// </summary>
        public bool IsUse { get; private set; }

        /// <summary>
        /// 启用可寻址资源定位
        /// </summary>
        public bool EnableAddressable => _package.GetPackageDetails().EnableAddressable;
        
        /// <summary>
        /// The patch update flow.
        /// <para>补丁更新流程</para>
        /// </summary>
        private enum EUpdateFlow
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

        private EPlayMode _playMode = EPlayMode.EditorSimulateMode;

        private Transform _patchUpdater;
        private RectTransform _rectUpdater;
        private RectTransform _rectHintsBox;
        private RectTransform _tranMessgeBox;
        private Text _txtHints;
        private Text _txtUpdaterTips;
        private Slider _sldUpdaterSlider;
        private List<Button> _allButtons;

        private string _packageVersion;
        private ResourcePackage _package;
        private Dictionary<string, bool> _cacheData;
        private ResourceDownloaderOperation _downloader;

        private Action _callback;
        private IEnumerator _ieCurrentIE;
        private Queue<IEnumerator> _updateStateQueue;

        void ISingleton.Init()
        {
            // 初始化资源系统
            YooAssets.Initialize();
            _updateStateQueue = new Queue<IEnumerator>();
        }

        void ISingleton.Quit()
        {
            IsUse = false;
            
            _package = null;
            _downloader = null;

            _updateStateQueue.Clear();
            _updateStateQueue = null;
            _callback = null;
        }

        /// <summary>
        /// Start update patch.
        /// <para>开始更新补丁</para>
        /// </summary>
        /// <param name="mode">Refresh scheme.<para>更新模式</para></param>
        /// <param name="callback">Update the completion callback.<para>更新完成回调</para></param>
        /// <param name="packageName">The name of the package to update<para>要更新的包名</para></param>
        public void StartUpdatePatch(EPlayMode mode, Action callback = null, string packageName = "DefaultPackage")
        {
            _cacheData = new Dictionary<string, bool>(1000);
            _playMode = mode;
            D.Emphasize($"资源系统运行模式：{mode}");
            _callback = callback;
            _package = YooAssets.TryGetPackage(packageName);
            if (_package == null)
            {
                // 创建默认的资源包
                _package = YooAssets.CreatePackage(packageName);

                //设置该资源包为默认的资源包，可以使用YooAssets相关加载接口加载该资源包内容。
                YooAssets.SetDefaultPackage(_package);
            }
            else
            {
                _package = YooAssets.GetPackage(packageName);
            }
            EF.Load.AddResourcePackage(_package);

            _updateStateQueue.Clear();
            if (_package.InitializeStatus != EOperationStatus.Succeed)
            {
                _updateStateQueue.Enqueue(Initialize());
                _updateStateQueue.Enqueue(GetStaticVersion());
                _updateStateQueue.Enqueue(GetManifest());
            }

            _updateStateQueue.Enqueue(CreateDownloader());
            _updateStateQueue.Enqueue(BeginDownload());

            Run(EUpdateFlow.Initialize);
        }

        #region Run progress. 跑更新流程
        void Run(EUpdateFlow nextFlow)
        {
            //D.Emphasize($"Next state is {nextFlow}       IEnumerator.Count = {_updateStateQueue.Count}");

            if (null != _ieCurrentIE)
                EF.StopCoroutines(_ieCurrentIE);
            if (nextFlow.Equals(EUpdateFlow.Done) || _updateStateQueue.Count <= 0)
            {
                IsUse = true;
                _ieCurrentIE = null;
                if (_patchUpdater)
                    _tranMessgeBox.gameObject.SetActive(true);
                else
                    _callback?.Invoke();
                return;
            }

            _ieCurrentIE = _updateStateQueue.Dequeue();
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
        #endregion

        #region Update the StaticViersion file.更新静态版本文件
        /// <summary>
        /// Update the StaticViersion file.更新静态版本文件
        /// </summary>
        IEnumerator GetStaticVersion()
        {
            var operation = _package.RequestPackageVersionAsync(false);
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
            _downloader = _package.CreateResourceDownloader(10, 3);

            yield return null;

            if (_downloader.TotalDownloadCount == 0)
            {
                Run(EUpdateFlow.Done);
            }
            else
            {
                if (null == _patchUpdater)
                {
                    _patchUpdater = Object
                        .Instantiate(
                            EF.Load.LoadInResources<GameObject>(EF.Projects.AppConst.UIPrefabsPath + "PatchUpdater"))
                        .transform;

                    _txtHints = EF.Tool.Find<Text>(_patchUpdater, "Txt_Hints");
                    _rectUpdater = EF.Tool.Find<RectTransform>(_patchUpdater, "Tran_Updater");
                    _rectHintsBox = EF.Tool.Find<RectTransform>(_patchUpdater, "Tran_HintsBox");
                    _tranMessgeBox = EF.Tool.Find<RectTransform>(_patchUpdater, "Tran_MessgeBox");
                    EF.Tool.Find<Button>(_patchUpdater, "Btn_True")
                        .RegisterInListAndBindEvent(OnClickDownloadBegin, ref _allButtons);
                    EF.Tool.Find<Button>(_patchUpdater, "Btn_False")
                        .RegisterInListAndBindEvent(UpdateDone, ref _allButtons);
                }

                _txtHints.text =
                    $"一共发现了{_downloader.TotalDownloadCount}个资源，总大小为{(int)(_downloader.TotalDownloadBytes / (1024f * 1024f))}mb需要更新,是否下载。";
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
            _downloader.DownloadErrorCallback = OnDownloadErrorFunction;
            _downloader.DownloadUpdateCallback = OnDownloadProgressUpdateFunction;
            _downloader.DownloadFinishCallback = OnDownloadOverFunction;
            _downloader.DownloadFileBeginCallback = OnStartDownloadFileFunction;

            //开启下载
            _downloader.BeginDownload();
            yield return _downloader;

            // 检测下载结果
            if (_downloader.Status != EOperationStatus.Succeed)
                yield break;

            Run(EUpdateFlow.Done);
        }

        private void OnDownloadErrorFunction(DownloadErrorData errorData)
        {
            D.Error($"Download the file failed. The file name is {errorData.FileName} ,  Error info is {errorData.ErrorInfo}");
        }

        private void OnDownloadProgressUpdateFunction(DownloadUpdateData downloadData)
        {
            _sldUpdaterSlider.value = (float)downloadData.CurrentDownloadBytes / downloadData.TotalDownloadBytes;

            string currentSizeMB = (downloadData.CurrentDownloadBytes / 1048576f).ToString("f1");
            string totalSizeMB = (downloadData.TotalDownloadBytes / 1048576f).ToString("f1");
            _txtUpdaterTips.text = $"{downloadData.CurrentDownloadCount}/{downloadData.TotalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB";
        }

        private void OnStartDownloadFileFunction(DownloadFileData fileData)
        {
            D.Error($"当前下载：{fileData.FileName}, 大小为：{fileData.FileSize}");
        }

        private void OnDownloadOverFunction(DownloaderFinishData finishData)
        {
            D.Log($"{finishData.PackageName}下载完成，结果为：{finishData.Succeed}");
        }

        #endregion

        /// <summary>
        /// 当点击开始下载
        /// </summary>
        void OnClickDownloadBegin()
        {
            _rectUpdater.gameObject.SetActive(true);
            _rectHintsBox.gameObject.SetActive(false);
            Run(EUpdateFlow.BeginDownload);
        }
    }
}