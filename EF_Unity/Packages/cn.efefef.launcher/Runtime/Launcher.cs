/*
 * ================================================
 * Describe:        This script is used to run the AOT boot kernel.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 15:58:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 15:58:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HybridCLR;
using UnityEngine;
using YooAsset;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// AOT 启动内核：YooAsset 初始化 → 版本检查 → 更新清单 → 下载 → 补 AOT 元数据 → 加载热更程序集 → 调热更入口。
    /// <para>AOT boot kernel. Order matters: AOT metadata must be loaded before the hot update assemblies.</para>
    /// </summary>
    /// <remarks>
    /// <b>铁律：本程序集不得引用任何 cn.efefef.* 包。</b>
    /// EF.Core / EF.UI / EF.YooAsset 都会进热更层，而 HybridCLR 会把热更程序集从包体剔除，
    /// 一旦这里引用了它们，il2cpp 打包阶段就会断。
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Launcher : MonoBehaviour
    {
        private const string LogTag = "[ Launcher ]";

        #region 字段

        [Header("配置 / Config")] [SerializeField]
        private LauncherConfig _config;

        [Header("运行 / Runtime")] [SerializeField]
        private bool _runOnStart = true;

        [SerializeField] private bool _dontDestroyOnLoad = true;
        [SerializeField] private bool _verbose = true;

        private ILauncherView _view;
        private ILauncherHook _hook;
        private ResourcePackage _package;
        private bool _running;
        private readonly List<Assembly> _hotfixAssemblies = new List<Assembly>();

        #endregion

        #region 属性

        /// <summary>当前状态</summary>
        public LauncherState State { get; private set; } = LauncherState.Idle;

        /// <summary>启动配置</summary>
        public LauncherConfig Config => _config;

        /// <summary>已加载的热更程序集（按配置顺序）</summary>
        public IReadOnlyList<Assembly> HotfixAssemblies => _hotfixAssemblies;

        #endregion

        #region Unity 生命周期

        private void Reset()
        {
            name = "GameLauncher";
        }

        private void Awake()
        {
            _view = FindComponent<ILauncherView>();
            _hook = FindComponent<ILauncherHook>();

            if (_view == null)
                D.Warning($"{LogTag} 未找到 ILauncherView 实现，启动过程将没有界面反馈。");

            if (_dontDestroyOnLoad && transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_runOnStart)
                Run();
        }

        #endregion

        #region 对外接口

        /// <summary>
        /// 启动。重复调用会被忽略
        /// <para>Start the pipeline. Repeated calls are ignored.</para>
        /// </summary>
        [ContextMenu("Run Launcher")]
        public void Run()
        {
            if (_running || State == LauncherState.Completed)
                return;

            StartCoroutine(RunPipeline());
        }

        /// <summary>
        /// 失败后重试
        /// <para>Retry after a failure.</para>
        /// </summary>
        public void Retry()
        {
            if (_running)
                return;

            State = LauncherState.Idle;
            Run();
        }

        #endregion

        #region 主流程

        private IEnumerator RunPipeline()
        {
            _running = true;
            _hotfixAssemblies.Clear();

            if (!ValidateConfig(out string configError))
            {
                Fail(LauncherErrorCode.ConfigInvalid, configError);
                yield break;
            }

            #region 1. 项目钩子：隐私协议 / 渠道 SDK / 登录

            SetState(LauncherState.Booting, "初始化");
            if (_hook != null)
            {
                bool finished = false;
                try
                {
                    _hook.OnBoot(() => finished = true);
                }
                catch (Exception e)
                {
                    Fail(LauncherErrorCode.ConfigInvalid, $"ILauncherHook.OnBoot 抛出异常：{e.Message}");
                    yield break;
                }

                while (!finished)
                    yield return null;
            }

            #endregion

            #region 2. 资源系统

            SetState(LauncherState.Initializing, "初始化资源系统");
            if (!TryCreatePackage(out string packageError))
            {
                Fail(LauncherErrorCode.PackageInitFailed, packageError);
                yield break;
            }

            if (_package.InitializeStatus != EOperationStatus.Succeed)
            {
                InitializeParameters parameters;
                try
                {
                    parameters = CreateInitializeParameters();
                }
                catch (Exception e)
                {
                    Fail(LauncherErrorCode.PackageInitFailed, e.Message);
                    yield break;
                }

                var initOp = _package.InitializeAsync(parameters);
                yield return initOp;
                if (initOp.Status != EOperationStatus.Succeed)
                {
                    Fail(LauncherErrorCode.PackageInitFailed, $"资源包初始化失败：{initOp.Error}");
                    yield break;
                }
            }

            #endregion

            #region 3. 版本 + 清单

            SetState(LauncherState.CheckingVersion, "检查资源版本");
            var versionOp = _package.RequestPackageVersionAsync(timeout: _config.TimeoutSeconds);
            yield return versionOp;
            if (versionOp.Status != EOperationStatus.Succeed)
            {
                Fail(LauncherErrorCode.VersionRequestFailed, $"获取资源版本失败：{versionOp.Error}");
                yield break;
            }

            var manifestOp = _package.UpdatePackageManifestAsync(versionOp.PackageVersion, _config.TimeoutSeconds);
            yield return manifestOp;
            if (manifestOp.Status != EOperationStatus.Succeed)
            {
                Fail(LauncherErrorCode.ManifestUpdateFailed, $"更新资源清单失败：{manifestOp.Error}");
                yield break;
            }

            #endregion

            #region 4. 下载

            SetState(LauncherState.Downloading, "更新资源");
            ReportProgress(0, 0, 0, 0);
            var downloader = _package.CreateResourceDownloader(_config.DownloadingMaxNumber, _config.FailedTryAgain);
            if (downloader.TotalDownloadCount > 0)
            {
                downloader.BeginDownload();
                while (!downloader.IsDone)
                {
                    ReportProgress(downloader.CurrentDownloadCount, downloader.TotalDownloadCount,
                        downloader.CurrentDownloadBytes, downloader.TotalDownloadBytes);
                    yield return null;
                }

                ReportProgress(downloader.CurrentDownloadCount, downloader.TotalDownloadCount,
                    downloader.CurrentDownloadBytes, downloader.TotalDownloadBytes);

                if (downloader.Status != EOperationStatus.Succeed)
                {
                    Fail(LauncherErrorCode.DownloadFailed, $"资源下载失败：{downloader.Error}");
                    yield break;
                }
            }

            #endregion

            #region 5. AOT 补充元数据（必须在热更程序集之前）

            SetState(LauncherState.LoadingMetadata, "加载 AOT 元数据");
            if (!TryLoadAotMetadata(out string metadataError))
            {
                Fail(LauncherErrorCode.MetadataLoadFailed, metadataError);
                yield break;
            }

            #endregion

            #region 6. 热更程序集（按配置顺序，即拓扑序）

            SetState(LauncherState.LoadingHotfix, "加载热更程序集");
            var entries = _config.HotfixDlls;
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry is not { Enabled: true })
                        continue;

                    string entryName = string.IsNullOrEmpty(entry.Name) ? $"#{i}" : entry.Name;
                    byte[] dllBytes = null;

                    if (!string.IsNullOrEmpty(entry.Location))
                    {
                        var handle = _package.LoadAssetAsync(entry.Location, typeof(TextAsset));
                        yield return handle;

                        if (handle.Status != EOperationStatus.Succeed)
                        {
                            Fail(LauncherErrorCode.AssemblyLoadFailed,
                                $"热更程序集加载失败：{entryName}（{handle.LastError}）");
                            yield break;
                        }

                        var textAsset = handle.GetAssetObject<TextAsset>();
                        dllBytes = textAsset != null ? textAsset.bytes : null;
                        handle.Release();
                    }
                    else if (entry.FallbackAsset != null)
                    {
                        dllBytes = entry.FallbackAsset.bytes;
                    }

                    if (dllBytes == null || dllBytes.Length == 0)
                    {
                        Fail(LauncherErrorCode.AssemblyLoadFailed, $"热更程序集内容为空：{entryName}");
                        yield break;
                    }

                    try
                    {
                        var assembly = Assembly.Load(dllBytes);
                        _hotfixAssemblies.Add(assembly);
                        Log($"已加载热更程序集：{entryName}（{assembly.GetName().Name}）");
                    }
                    catch (Exception e)
                    {
                        Fail(LauncherErrorCode.AssemblyLoadFailed, $"热更程序集加载异常：{entryName} -> {e.Message}");
                        yield break;
                    }
                }
            }

            if (_hotfixAssemblies.Count == 0 && entries is { Length: > 0 })
            {
                Fail(LauncherErrorCode.AssemblyLoadFailed, "没有任何热更程序集被加载，请检查配置");
                yield break;
            }

            #endregion

            #region 7. 调热更入口

            SetState(LauncherState.Invoking, "启动游戏");
            if (!TryInvokeEntry(out LauncherErrorCode entryCode, out string entryError))
            {
                Fail(entryCode, entryError);
                yield break;
            }

            #endregion

            #region 8. 完成

            SetState(LauncherState.Completed, "进入游戏");
            _running = false;

            #endregion
        }

        #endregion

        #region 步骤实现

        private bool ValidateConfig(out string error)
        {
            error = null;

            if (_config == null)
            {
                error = "LauncherConfig 未指定";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.PackageName))
            {
                error = "PackageName 为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.EntryType))
            {
                error = "EntryType 为空（需要热更入口类型的全名）";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.EntryMethod))
            {
                error = "EntryMethod 为空";
                return false;
            }

            if (_config.PlayMode == EPlayMode.HostPlayMode && string.IsNullOrWhiteSpace(_config.MainUrl))
            {
                error = "HostPlayMode 下 MainUrl 不能为空";
                return false;
            }

            return true;
        }

        private bool TryCreatePackage(out string error)
        {
            error = null;

            if (!YooAssets.Initialized)
            {
                try
                {
                    YooAssets.Initialize();
                }
                catch (Exception e)
                {
                    error = $"YooAssets 初始化失败：{e.Message}";
                    return false;
                }
            }

            _package = YooAssets.TryGetPackage(_config.PackageName);
            if (_package != null)
                return true;

            _package = YooAssets.CreatePackage(_config.PackageName);
            if (_package == null)
            {
                error = $"创建资源包失败：{_config.PackageName}";
                return false;
            }

            YooAssets.SetDefaultPackage(_package);
            return true;
        }

        private InitializeParameters CreateInitializeParameters()
        {
            switch (_config.PlayMode)
            {
#if UNITY_EDITOR
                case EPlayMode.EditorSimulateMode:
                {
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(_config.PackageName);
                    return new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters =
                            FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult
                                .PackageRootDirectory)
                    };
                }
#endif
                case EPlayMode.OfflinePlayMode:
                    return new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };

                case EPlayMode.HostPlayMode:
                    return new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(
                            new LauncherRemoteServices(_config.MainUrl, _config.FallbackUrl))
                    };

                case EPlayMode.WebPlayMode:
                    return new WebPlayModeParameters
                    {
                        WebServerFileSystemParameters =
                            FileSystemParameters.CreateDefaultWebServerFileSystemParameters()
                    };

                case EPlayMode.CustomPlayMode:
                default:
                    throw new NotSupportedException(
                        $"当前构建不支持运行模式 {_config.PlayMode}，请检查 LauncherConfig");
            }
        }

        private bool TryLoadAotMetadata(out string error)
        {
            error = null;

            var assets = _config.AotMetadataDlls;
            if (assets == null || assets.Length == 0)
            {
                Log("未配置 AOT 元数据，跳过补元数据（热更代码里出现 AOT 泛型实例化时会踩空）");
                return true;
            }

            foreach (var asset in assets)
            {
                if (asset == null)
                    continue;

                var code = RuntimeApi.LoadMetadataForAOTAssembly(asset.bytes, HomologousImageMode.SuperSet);
                if (code != LoadImageErrorCode.OK)
                {
                    error = $"补充元数据失败：{asset.name}（{code}）";
                    return false;
                }

                Log($"已补充 AOT 元数据：{asset.name}");
            }

            return true;
        }

        private bool TryInvokeEntry(out LauncherErrorCode code, out string error)
        {
            code = LauncherErrorCode.EntryNotFound;
            error = null;

            Type type = FindType(_config.EntryType);
            if (type == null)
            {
                error = $"找不到热更入口类型：{_config.EntryType}（需要命名空间全名，且对应程序集已加载）";
                return false;
            }

            var method = type.GetMethod(
                _config.EntryMethod,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (method == null)
            {
                error = $"找不到入口方法：{_config.EntryType}.{_config.EntryMethod}()，必须是 static 无参方法";
                return false;
            }

            try
            {
                method.Invoke(null, null);
                Log($"热更入口已执行：{_config.EntryType}.{_config.EntryMethod}()");
                return true;
            }
            catch (TargetInvocationException e)
            {
                code = LauncherErrorCode.EntryThrew;
                error = $"热更入口抛出异常：{e.InnerException ?? e}";
                return false;
            }
            catch (Exception e)
            {
                code = LauncherErrorCode.EntryThrew;
                error = $"热更入口调用失败：{e.Message}";
                return false;
            }
        }

        private Type FindType(string fullName)
        {
            foreach (var assembly in _hotfixAssemblies)
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }

            // 编辑器模拟模式：热更程序集本来就随工程编译，直接在当前域里找
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }

            return null;
        }

        #endregion

        #region 状态与反馈

        private void SetState(LauncherState state, string tip)
        {
            State = state;
            Log($"{state} | {tip}");

            if (_view == null)
                return;

            try
            {
                _view.OnStateChanged(state, tip);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void ReportProgress(long currentCount, long totalCount, long currentBytes, long totalBytes)
        {
            if (_view == null)
                return;

            try
            {
                _view.OnProgress(currentCount, totalCount, currentBytes, totalBytes);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void Fail(LauncherErrorCode code, string message)
        {
            _running = false;
            State = LauncherState.Failed;
            D.Error($"{LogTag} 启动失败 [{code}] {message}");

            if (_view == null)
                return;

            try
            {
                _view.OnError(code, message, Retry);
            }
            catch (Exception e)
            {
                D.Exception(e);
            }
        }

        private void Log(string message)
        {
            if (_verbose)
                D.Log($"{LogTag} {message}");
        }

        private T FindComponent<T>() where T : class
        {
            var behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mono in behaviours)
            {
                if (mono is T target)
                    return target;
            }

            return null;
        }

        #endregion
    }
}