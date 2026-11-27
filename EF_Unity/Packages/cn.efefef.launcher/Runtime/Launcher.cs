/*
 * ================================================
 * Describe:        This script is used to run the AOT boot kernel.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 15:58:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-19 10:50:00
 * ScriptVersion:   0.5
 * ================================================
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using YooAsset;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// AOT 启动内核：YooAsset 初始化 → 版本检查 → 更新清单 → 下载 → 补 AOT 元数据 → 加载热更程序集 → 调热更入口。
    /// <para>AOT boot kernel. Order matters: AOT metadata must be loaded before the hot update assemblies.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Launcher : MonoBehaviour
    {
        private const string LogTag = "[ Launcher ]";

        #region 辅助类型

        // 协程方法不能带 out 参数，用一个小载体把失败原因带回主流程
        private sealed class StepResult
        {
            public bool Failed;
            public LauncherErrorCode Code;
            public string Error;
        }

        #endregion

        #region 字段

        [HeaderPro("★ 配置 ★", "★ Config ★", HeaderProType.Title)] [SerializeField]
        private LauncherConfig _config;

        [HeaderPro("", "", HeaderProType.Separator), Space]
        [HeaderPro("以 < ILauncher > 开头的接口注入（可选）",
            "Interface injection starting with < ILauncher > (optional)", HeaderProType.Title)]
        [HeaderPro("展示界面状态的 < ILauncherView >",
            "The < ILauncherView > that displays the interface status")]
        [SerializeField]
        private MonoBehaviour _viewRef;

        [HeaderPro("架构启动前的 < ILauncherHook >",
            "Before the architecture is activated, < ILauncherHook >")]
        [SerializeField]
        private MonoBehaviour _hookRef;

        [HeaderPro("进入热更前的 < ILauncherEnterConfirm >",
            "Before enter the hotfix logic conduction, < ILauncherEnterConfirm >")]
        [SerializeField]
        private MonoBehaviour _enterConfirmRef;

        [HeaderPro("", "", HeaderProType.Separator), Space]
        [HeaderPro("启动后的设置", "Settings after startup", HeaderProType.Title)]
        [HeaderPro("立刻运行，也可以通过调用 Run() 运行", "Run immediately, can also by calling the Run()")]
        [SerializeField]
        private bool _runOnStart = true;

        [HeaderPro("加载后跨场景常驻", "Keep alive across scenes")] [SerializeField]
        private bool _dontDestroyOnLoad = true;

        private const int DefaultMaxRetryCount = 3;

        private const float DefaultRetryDelaySeconds = 2f;

        // 进度按 10Hz 广播：界面每帧拼字符串的代价远大于肉眼可见差异
        private const float ProgressIntervalSeconds = 0.1f;

        // 重试耗尽时给界面的空回调，避免界面拿到 null 后空引用
        private static readonly Action NoopRetry = () => { };

        private ILauncherView _view;
        private ILauncherHook _hook;
        private ILauncherEnterConfirm _enterConfirm;
        private ResourcePackage _package;
        private bool _running;

        private int _retryCount;

        // 退避等待是否已排上；UniTask 没有 Coroutine 句柄，用标记位防止重复排程
        private bool _retryScheduled;

        // 随 GameObject 销毁自动取消，异步流程不会在已销毁的对象上继续跑
        private CancellationToken _destroyToken;
        private float _startTime;

        // 当前状态由状态机托管
        private readonly StateMachine _fsm = new StateMachine(nameof(Launcher));

        private readonly List<Assembly> _hotfixAssemblies = new List<Assembly>();

        // Assembly.Load(byte[]) 每次都会创建新程序集；重试时重复加载同名 dll 会产生第二份类型身份
        private readonly HashSet<string> _loadedAssemblyNames = new HashSet<string>();

        #endregion

        #region Unity 生命周期

        private void Reset()
        {
            name = "GameLauncher";
        }

        private void Awake()
        {
            ConfigureStateMachine();
            _view = ResolveRef<ILauncherView>(_viewRef, nameof(_viewRef));
            _hook = ResolveRef<ILauncherHook>(_hookRef, nameof(_hookRef));
            _enterConfirm = ResolveRef<ILauncherEnterConfirm>(_enterConfirmRef, nameof(_enterConfirmRef));
            _destroyToken = this.GetCancellationTokenOnDestroy();

            if (_view == null)
                Warn("未找到 ILauncherView 实现，启动过程将没有界面反馈。",
                    "No ILauncherView found; the boot sequence will have no UI feedback.");

            if (_dontDestroyOnLoad && transform.parent == null)
                DontDestroyOnLoad(gameObject);
        }

        // 注册启动阶段与合法转移，FSM 只记录阶段，不承载业务逻辑
        private void ConfigureStateMachine()
        {
            foreach (LauncherState state in Enum.GetValues(typeof(LauncherState)))
                _fsm.Register((int)state, state.ToString());

            _fsm.Allow((int)LauncherState.Idle, (int)LauncherState.Booting);
            _fsm.Allow((int)LauncherState.Booting, (int)LauncherState.Initializing);
            _fsm.Allow((int)LauncherState.Initializing, (int)LauncherState.CheckingVersion);
            _fsm.Allow((int)LauncherState.CheckingVersion, (int)LauncherState.Downloading);
            _fsm.Allow((int)LauncherState.Downloading, (int)LauncherState.LoadingMetadata);
            _fsm.Allow((int)LauncherState.LoadingMetadata, (int)LauncherState.LoadingHotfix);
            _fsm.Allow((int)LauncherState.LoadingHotfix, (int)LauncherState.WaitingForConfirm);
            _fsm.Allow((int)LauncherState.LoadingHotfix, (int)LauncherState.Invoking);
            _fsm.Allow((int)LauncherState.WaitingForConfirm, (int)LauncherState.Invoking);
            _fsm.Allow((int)LauncherState.Invoking, (int)LauncherState.Completed);

            foreach (LauncherState state in Enum.GetValues(typeof(LauncherState)))
            {
                if (state != LauncherState.Failed)
                    _fsm.Allow((int)state, (int)LauncherState.Failed);
            }

            _fsm.Allow((int)LauncherState.Failed, (int)LauncherState.Retrying);
            _fsm.Allow((int)LauncherState.Retrying, (int)LauncherState.Idle);
            _fsm.SetInitial((int)LauncherState.Idle);
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
            // 退避等待期间 _running 已是 false，不拦 _retryScheduled 会并发跑起第二条管线
            if (_running || _retryScheduled || _fsm.Current == (int)LauncherState.Completed)
                return;

            RunPipelineAsync(_destroyToken).Forget();
        }

        /// <summary>
        /// 运行时注入配置（供代码动态 AddComponent 使用）
        /// <para>Injects the config at runtime, for launchers created via AddComponent.</para>
        /// </summary>
        public void Configure(LauncherConfig config)
        {
            if (_running)
            {
                Warn("启动流程运行中，忽略 Configure（配置只可在启动前注入）。",
                    "Boot is running; Configure ignored (inject config before boot only).");
                return;
            }

            if (config != null)
                _config = config;
        }

        /// <summary>
        /// 失败后按退避重试，次数受 <see cref="LauncherConfig.MaxRetryCount"/> 限制
        /// <para>Retry after a failure with backoff, bounded by <see cref="LauncherConfig.MaxRetryCount"/>.</para>
        /// </summary>
        public void Retry()
        {
            if (_running || _fsm.Current == (int)LauncherState.Completed || _retryScheduled)
                return;

            int max = _config != null && _config.MaxRetryCount > 0
                ? _config.MaxRetryCount
                : DefaultMaxRetryCount;

            if (_retryCount >= max)
            {
                string message = L($"重试次数已用尽（{_retryCount}/{max}）",
                    $"Retry attempts exhausted ({_retryCount}/{max})");
                D.Error($"{LogTag} {L("启动失败", "Boot failed")} [{LauncherErrorCode.RetryExhausted}] {message}");
                SetState(LauncherState.Failed);
                BroadcastError(LauncherErrorCode.RetryExhausted, NoopRetry);
                return;
            }

            _retryCount++;
            float delay = _config != null && _config.RetryDelaySeconds > 0f
                ? _config.RetryDelaySeconds
                : DefaultRetryDelaySeconds;

            SetState(LauncherState.Retrying);
            _retryScheduled = true;
            RetryAfterDelayAsync(delay, _destroyToken).Forget();
        }

        #endregion

        #region 重试实现

        // 退避只作用于重试，首次启动不等待；用 realtime 避免 timeScale 为 0 时卡死
        private async UniTask RetryAfterDelayAsync(float delay, CancellationToken token)
        {
            if (delay > 0f)
            {
                bool canceled = await UniTask.Delay(TimeSpan.FromSeconds(delay), DelayType.Realtime,
                    cancellationToken: token).SuppressCancellationThrow();

                if (canceled)
                {
                    _retryScheduled = false;
                    return;
                }
            }

            _retryScheduled = false;
            SetState(LauncherState.Idle);
            Run();
        }

        #endregion

        #region 主流程

        private async UniTask RunPipelineAsync(CancellationToken token)
        {
            _running = true;
            try
            {
                await RunPipelineInternalAsync(token);
            }
            catch (OperationCanceledException)
            {
                // GameObject 已销毁：静默收场，不再触碰已失效的 view
            }
            catch (Exception e)
            {
                // 协程时代这里会被 Unity 吞掉（表现为流程静默中断）；UniTask 下必须自己收口
                D.Exception(e);
                Fail(LauncherErrorCode.Unexpected, L($"启动流程异常：{e.Message}", $"Unexpected boot error: {e.Message}"));
            }
            finally
            {
                // 正常结束、失败、异常、取消都必须复位，否则 Retry()/Run() 会永久失效
                _running = false;
            }
        }

        // 八个阶段的主体：取消与异常一律向上抛给 RunPipelineAsync
        private async UniTask RunPipelineInternalAsync(CancellationToken token)
        {
            _startTime = Time.realtimeSinceStartup;

            if (!ValidateConfig(out string configError))
            {
                Fail(LauncherErrorCode.ConfigInvalid, configError);
                return;
            }

            #region 1. 项目钩子：隐私协议 / 渠道 SDK / 登录

            SetState(LauncherState.Booting);
            if (_hook != null)
            {
                bool finished = false;
                try
                {
                    _hook.OnBoot(() => finished = true);
                }
                catch (Exception e)
                {
                    Fail(LauncherErrorCode.HookFailed,
                        L($"ILauncherHook.OnBoot 抛出异常：{e.Message}", $"ILauncherHook.OnBoot threw: {e.Message}"));
                    return;
                }

                // 外部钩子不回调时不能永久挂起：按配置超时兜底（TimeoutSeconds<=0 表示不启用超时，保持兼容）
                float waited = 0f;
                float timeout = _config?.TimeoutSeconds ?? 0f;
                while (!finished)
                {
                    if (timeout > 0f)
                    {
                        waited += Time.unscaledDeltaTime;
                        if (waited >= timeout)
                        {
                            Fail(LauncherErrorCode.HookFailed,
                                L($"ILauncherHook.OnBoot 超时（{timeout}s），未收到完成回调",
                                  $"ILauncherHook.OnBoot timed out ({timeout}s), completion callback not received"));
                            return;
                        }
                    }
                    await UniTask.NextFrame(token);
                }
            }

            #endregion

            #region 2. 资源系统

            SetState(LauncherState.Initializing);

            if (!TryCreatePackage(out string packageError))
            {
                Fail(LauncherErrorCode.PackageInitFailed, packageError);
                return;
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
                    return;
                }

                var initOp = _package.InitializeAsync(parameters);
                await initOp.Task;
                token.ThrowIfCancellationRequested();
                if (initOp.Status != EOperationStatus.Succeed)
                {
                    Fail(LauncherErrorCode.PackageInitFailed,
                        L($"资源包初始化失败：{initOp.Error}", $"Package init failed: {initOp.Error}"));
                    return;
                }
            }

            #endregion

            #region 3. 版本 + 清单

            SetState(LauncherState.CheckingVersion);
            var versionOp = _package.RequestPackageVersionAsync(timeout: _config.TimeoutSeconds);
            await versionOp.Task;
            token.ThrowIfCancellationRequested();
            if (versionOp.Status != EOperationStatus.Succeed)
            {
                Fail(LauncherErrorCode.VersionRequestFailed,
                    L($"获取资源版本失败：{versionOp.Error}", $"Failed to request package version: {versionOp.Error}"));
                return;
            }

            var manifestOp = _package.UpdatePackageManifestAsync(versionOp.PackageVersion, _config.TimeoutSeconds);
            await manifestOp.Task;
            token.ThrowIfCancellationRequested();
            if (manifestOp.Status != EOperationStatus.Succeed)
            {
                Fail(LauncherErrorCode.ManifestUpdateFailed,
                    L($"更新资源清单失败：{manifestOp.Error}", $"Failed to update manifest: {manifestOp.Error}"));
                return;
            }

            #endregion

            #region 4. 下载

            SetState(LauncherState.Downloading);
            ReportProgress(0, 0, 0, 0);
            var downloader = _package.CreateResourceDownloader(_config.DownloadingMaxNumber, _config.FailedTryAgain);
            if (downloader.TotalDownloadCount > 0)
            {
                downloader.BeginDownload();
                float nextReport = 0f;
                while (!downloader.IsDone)
                {
                    // 节流：下载可能跑几分钟，每帧广播会让界面每帧拼一次字符串（数十 KB/s 的 GC）
                    if (Time.unscaledTime >= nextReport)
                    {
                        ReportProgress(downloader.CurrentDownloadCount, downloader.TotalDownloadCount,
                            downloader.CurrentDownloadBytes, downloader.TotalDownloadBytes);
                        nextReport = Time.unscaledTime + ProgressIntervalSeconds;
                    }

                    await UniTask.NextFrame(token);
                }

                ReportProgress(downloader.CurrentDownloadCount, downloader.TotalDownloadCount,
                    downloader.CurrentDownloadBytes, downloader.TotalDownloadBytes);

                if (downloader.Status != EOperationStatus.Succeed)
                {
                    Fail(LauncherErrorCode.DownloadFailed,
                        L($"资源下载失败：{downloader.Error}", $"Resource download failed: {downloader.Error}"));
                    return;
                }
            }

            #endregion

            #region 5. AOT 补充元数据（必须在热更程序集之前）

            SetState(LauncherState.LoadingMetadata);
            if (!TryLoadAotMetadata(out string metadataError))
            {
                Fail(LauncherErrorCode.MetadataLoadFailed, metadataError);
                return;
            }

            #endregion

            #region 6. 热更程序集（按配置顺序，即拓扑序）

            SetState(LauncherState.LoadingHotfix);
            var entries = _config.HotfixDlls;
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    var entry = entries[i];
                    if (entry is not { Enabled: true })
                        continue;

                    string entryName = string.IsNullOrEmpty(entry.Name) ? $"#{i}" : entry.Name;
                    string assemblyName = ResolveAssemblyName(entry, entryName);

                    if (assemblyName != null && _loadedAssemblyNames.Contains(assemblyName))
                        continue;

#if UNITY_EDITOR
                    // 编辑器模拟模式：热更 asmdef 本来就跟着工程编译进 ScriptAssemblies，
                    // 再从 bytes 加载一份会分裂出第二套类型身份（热更代码里 as/强转即 InvalidCastException）
                    if (_config.PlayMode == EPlayMode.EditorSimulateMode && assemblyName != null)
                    {
                        var compiled = FindLoadedAssembly(assemblyName);
                        if (compiled != null)
                        {
                            _hotfixAssemblies.Add(compiled);
                            _loadedAssemblyNames.Add(compiled.GetName().Name);
                            _loadedAssemblyNames.Add(assemblyName);
                            continue;
                        }

                        // 编辑器里没有同名编译产物（例如只随包发布的外部业务 dll）→ 退回 bytes 加载
                    }
#endif

                    byte[] dllBytes = null;

                    if (!string.IsNullOrEmpty(entry.Location))
                    {
                        var handle = _package.LoadAssetAsync(entry.Location, typeof(TextAsset));
                        EOperationStatus status;
                        string lastError;
                        try
                        {
                            // 直接 await Provider 的 Task（完成即续，零帧延迟）；句柄失效时它返回 null，用轮询兜底
                            if (handle.Task is { } loadTask)
                                await loadTask;
                            else
                                await UniTask.WaitUntil(() => handle.IsDone, cancellationToken: token);

                            token.ThrowIfCancellationRequested();

                            status = handle.Status;
                            lastError = handle.LastError;
                            if (status == EOperationStatus.Succeed)
                            {
                                var textAsset = handle.GetAssetObject<TextAsset>();
                                dllBytes = textAsset != null ? textAsset.bytes : null;
                            }
                        }
                        finally
                        {
                            handle.Release();
                        }

                        if (status != EOperationStatus.Succeed)
                        {
                            Fail(LauncherErrorCode.AssemblyLoadFailed,
                                L($"热更程序集加载失败：{entryName}（{lastError}）",
                                    $"Hotfix assembly load failed: {entryName} ({lastError})"));
                            return;
                        }
                    }
                    else if (entry.FallbackAsset != null)
                    {
                        dllBytes = entry.FallbackAsset.bytes;
                    }

                    if (dllBytes == null || dllBytes.Length == 0)
                    {
                        Fail(LauncherErrorCode.AssemblyLoadFailed,
                            L($"热更程序集内容为空：{entryName}", $"Hotfix assembly is empty: {entryName}"));
                        return;
                    }

                    try
                    {
                        var assembly = Assembly.Load(dllBytes);
                        _hotfixAssemblies.Add(assembly);
                        _loadedAssemblyNames.Add(assembly.GetName().Name);
                        if (assemblyName != null)
                            _loadedAssemblyNames.Add(assemblyName);
                    }
                    catch (Exception e)
                    {
                        Fail(LauncherErrorCode.AssemblyLoadFailed,
                            L($"热更程序集加载异常：{entryName} -> {e.Message}",
                                $"Hotfix assembly load threw: {entryName} -> {e.Message}"));
                        return;
                    }
                }
            }

            if (_hotfixAssemblies.Count == 0 && entries is { Length: > 0 })
            {
                Fail(LauncherErrorCode.AssemblyLoadFailed,
                    L("没有任何热更程序集被加载，请检查配置",
                        "No hotfix assembly was loaded; check the configuration"));
                return;
            }

            #endregion

            #region 6.5 进入游戏前确认（可选）

            if (_enterConfirm != null)
            {
                SetState(LauncherState.WaitingForConfirm);
                bool confirmed = false;
                try
                {
                    _enterConfirm.OnReadyToEnter(() => confirmed = true);
                }
                catch (Exception e)
                {
                    Fail(LauncherErrorCode.ConfirmFailed,
                        L($"ILauncherEnterConfirm.OnReadyToEnter 抛出异常：{e.Message}",
                            $"ILauncherEnterConfirm.OnReadyToEnter threw: {e.Message}"));
                    return;
                }

                // 与第 1 步 OnBoot 同一套轮询模式，并同样带超时兜底：
                // 用户永不点击确认或渠道 SDK 卡住时，Launcher 常驻不销毁、token 不会取消，无超时会永久停在等待态。
                float waited = 0f;
                float timeout = _config?.TimeoutSeconds ?? 0f;
                while (!confirmed)
                {
                    if (timeout > 0f)
                    {
                        waited += Time.unscaledDeltaTime;
                        if (waited >= timeout)
                        {
                            Fail(LauncherErrorCode.ConfirmFailed,
                                L($"ILauncherEnterConfirm 超时（{timeout}s），未收到进入确认",
                                  $"ILauncherEnterConfirm timed out ({timeout}s), no enter confirmation received"));
                            return;
                        }
                    }
                    await UniTask.NextFrame(token);
                }
            }

            #endregion

            #region 7. 调热更入口

            SetState(LauncherState.Invoking);
            var entryResult = new StepResult();
            await InvokeEntryStepAsync(entryResult);
            token.ThrowIfCancellationRequested();
            if (entryResult.Failed)
            {
                Fail(entryResult.Code, entryResult.Error);
                return;
            }

            #endregion

            #region 8. 完成

            SetState(LauncherState.Completed);

            #endregion
        }

        #endregion

        #region 步骤实现

        private bool ValidateConfig(out string error)
        {
            error = null;

            if (_config == null)
            {
                error = L("LauncherConfig 未指定", "LauncherConfig not assigned");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.PackageName))
            {
                error = L("PackageName 为空", "PackageName is empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.EntryType))
            {
                error = L("EntryType 为空（需要热更入口类型的全名）",
                    "EntryType is empty (full type name of the hotfix entry required)");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_config.EntryMethod))
            {
                error = L("EntryMethod 为空", "EntryMethod is empty");
                return false;
            }

            if (_config.PlayMode != EPlayMode.HostPlayMode ||
                !string.IsNullOrWhiteSpace(EFC.Projects.ResourcesArea.InnerUrl))
                return true;

            error = L("HostPlayMode 需要资源地址：请在 ProjectConfig.ResourcesArea 里填内网地址" +
                      "（该配置随包常驻 AOT，EFC 会在 AfterAssembliesLoaded 阶段加载它）",
                "HostPlayMode needs a resource address: set the intranet URL in ProjectConfig.ResourcesArea " +
                "(the config stays in AOT and is loaded by EFC at AfterAssembliesLoaded)");
            return false;
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
                    error = L($"YooAssets 初始化失败：{e.Message}", $"YooAssets init failed: {e.Message}");
                    return false;
                }
            }

            _package = YooAssets.TryGetPackage(_config.PackageName);
            if (_package != null)
                return true;

            _package = YooAssets.CreatePackage(_config.PackageName);
            if (_package == null)
            {
                error = L($"创建资源包失败：{_config.PackageName}", $"Failed to create package: {_config.PackageName}");
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
                            new LauncherRemoteServices(EFC.Projects.ResourcesArea.InnerUrl,
                                EFC.Projects.ResourcesArea.StandbyUrl))
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
                        L($"当前构建不支持运行模式 {_config.PlayMode}，请检查 LauncherConfig",
                            $"Play mode {_config.PlayMode} is not supported in this build; check LauncherConfig"));
            }
        }

        private bool TryLoadAotMetadata(out string error)
        {
            error = null;

            var assets = _config.AotMetadataDlls;
            if (assets == null || assets.Length == 0)
                return true;

            int missing = 0;
            int loaded = 0;

            foreach (var asset in assets)
            {
                if (asset == null)
                {
                    missing++;
                    continue;
                }

                var code = RuntimeApi.LoadMetadataForAOTAssembly(asset.bytes, HomologousImageMode.SuperSet);
                if (code != LoadImageErrorCode.OK)
                {
                    error = L($"补充元数据失败：{asset.name}（{code}）",
                        $"Failed to load AOT metadata: {asset.name} ({code})");
                    return false;
                }

                loaded++;
            }

            if (missing <= 0) return true;

            // 配置了却引用丢失 = 配置错误。静默跳过会让「补元数据从未生效」变成隐形故障，
            // 后续只在热更代码踩到 AOT 泛型时以难以定位的方式崩掉。
            error = L($"AOT 元数据配置了 {assets.Length} 项，其中 {missing} 项引用丢失（常见于 .dll.bytes 重新导入后 .meta guid 变化）。" +
                      $"成功补充 {loaded} 项。请在 LauncherConfig 里重新绑定，或清空 AotMetadataDlls 以显式跳过。",
                $"AOT metadata has {assets.Length} entries, {missing} with a lost reference " +
                $"(often after .dll.bytes re-import changes the .meta guid). {loaded} loaded. " +
                "Rebind them in LauncherConfig, or clear AotMetadataDlls to skip explicitly.");
            return false;
        }

        // 反射调用热更入口。入口返回 IEnumerator / Task 时等它跑完再标 Completed，
        // 否则异步初始化的热更层（例如 OpenView().Forget()）会在启动界面还没退场时就被判为“已完成”。
        private async UniTask InvokeEntryStepAsync(StepResult result)
        {
            Type type = FindType(_config.EntryType);
            if (type == null)
            {
                MarkFailed(result, LauncherErrorCode.EntryNotFound,
                    L($"找不到热更入口类型：{_config.EntryType}（需要命名空间全名，且对应程序集已加载）",
                        $"Hotfix entry type not found: {_config.EntryType} (needs a full namespace-qualified name, and its assembly loaded)"));
                return;
            }

            // 显式指定 Type.EmptyTypes：不指定时同名重载会抛 AmbiguousMatchException，
            // 被下面的 catch 吞成“调用失败”，真实原因（有重载）反而丢失
            var method = type.GetMethod(
                _config.EntryMethod,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);

            if (method == null)
            {
                MarkFailed(result, LauncherErrorCode.EntryNotFound,
                    L($"找不到无参入口方法：{_config.EntryType}.{_config.EntryMethod}()，必须是无参 static 方法",
                        $"Parameterless entry method not found: {_config.EntryType}.{_config.EntryMethod}() (must be a parameterless static method)"));
                return;
            }

            object returned;
            try
            {
                returned = method.Invoke(null, null);
            }
            catch (TargetInvocationException e)
            {
                MarkFailed(result, LauncherErrorCode.EntryThrew,
                    L($"热更入口抛出异常：{e.InnerException ?? e}", $"Hotfix entry threw: {e.InnerException ?? e}"));
                return;
            }
            catch (Exception e)
            {
                MarkFailed(result, LauncherErrorCode.EntryThrew,
                    L($"热更入口调用失败：{e.Message}", $"Failed to invoke hotfix entry: {e.Message}"));
                return;
            }

            if (returned is IEnumerator routine)
            {
                // 交给 Unity 跑（保留 WaitForSeconds 等自定义 yield 的语义）；对象销毁时协程自然停止
                await routine.ToUniTask(this);
                return;
            }

            if (returned is Task task)
            {
                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                    MarkFailed(result, LauncherErrorCode.EntryThrew,
                        L("热更入口异步任务被取消", "Hotfix entry async task was canceled"));
                }
                catch (Exception e)
                {
                    MarkFailed(result, LauncherErrorCode.EntryThrew,
                        L($"热更入口异步任务失败：{e.Message}", $"Hotfix entry async task failed: {e.Message}"));
                }

                return;
            }

            if (returned != null)
            {
                // 启动器是 AOT 程序集，拿不到 UniTask 类型；只能提示，不能替它等
                Warn($"热更入口返回 {returned.GetType().Name}，启动器不会等待该类型。" +
                     "需要「初始化完成后再标启动完成」请返回 IEnumerator 或 Task。",
                    $"Hotfix entry returned {returned.GetType().Name}; the launcher will not await it. " +
                    "Return IEnumerator or Task to defer the Completed state until initialization finishes.");
            }
        }

        private static void MarkFailed(StepResult result, LauncherErrorCode code, string message)
        {
            result.Failed = true;
            result.Code = code;
            result.Error = message;
        }

#if UNITY_EDITOR
        // 编辑器模拟模式按程序集名在当前域里找已编译版本
        private static Assembly FindLoadedAssembly(string assemblyName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                    return assembly;
            }

            return null;
        }
#endif

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

        /// <summary>条目 → 程序集名，用于跨次运行去重（解析口径与编辑器里的拓扑排序工具一致）</summary>
        private static string ResolveAssemblyName(HotUpdateDllEntry entry, string fallback)
        {
            string raw = string.IsNullOrWhiteSpace(entry.Name) ? entry.Location : entry.Name;
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            int slash = raw.LastIndexOfAny(new[] { '/', '\\' });
            if (slash >= 0 && slash + 1 < raw.Length)
                raw = raw[(slash + 1)..];

            if (raw.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase))
                raw = raw[..^6];
            if (raw.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                raw = raw[..^4];

            return raw.Trim();
        }

        #endregion

        #region 状态与反馈

        // Launcher 是通用包，不把展示文案下发给界面；
        // 订阅方按 state 自行解析本地化文本，换包不换文案。
        private void SetState(LauncherState state)
        {
            int id = (int)state;
            // 同状态不重复转移，避免 FSM 告警刷屏；界面仍收到通知
            if (_fsm.Current != id)
                _fsm.Transition(id);

            if (_view == null)
                return;

            try
            {
                _view.OnStateChanged(state);
            }
            catch (Exception e)
            {
                D.Exception(e);
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
                D.Exception(e);
            }
        }

        private void Fail(LauncherErrorCode code, string message)
        {
            _running = false;
            D.Error($"{LogTag} {L("启动失败", "Boot failed")} [{code}] {message}");
            // 状态必须走 SetState：直接赋属性会让界面收不到 Failed，独立加载屏会停在上一阶段
            SetState(LauncherState.Failed);
            Diagnose();

            BroadcastError(code, Retry);

            if (_config != null && _config.AutoRetryOnFailure)
                Retry();
        }

        // 错误码只下发给 ILauncherView，界面抛异常不影响启动流程。
        // 诊断详情只在 D.Error 里输出，不下发给界面（界面按 code 自行解析展示文案）。
        private void BroadcastError(LauncherErrorCode code, Action retry)
        {
            if (_view == null)
                return;

            try
            {
                _view.OnError(code, retry);
            }
            catch (Exception e)
            {
                D.Exception(e);
            }
        }

        // 失败现场一次打全，避免靠猜：版本、状态、已加载程序集、重试次数、耗时、Core 是否就绪
        private void Diagnose()
        {
            float elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _startTime);
            D.Log($"{LogTag} {L("诊断", "Diagnose")} | {L("版本", "Version")}={Application.version} " +
                  $"{L("状态", "State")}={_fsm.CurrentName} " +
                  $"{L("已加载热更程序集", "Hotfix assemblies")}={_hotfixAssemblies.Count} " +
                  $"{L("重试", "Retries")}={_retryCount} {L("耗时", "Elapsed")}={elapsed:0.0}s " +
                  $"CoreReady={EFC.Projects}");
        }

        // 按 EFC.Projects 的语言配置择一：AutoLanguage 开启时跟随系统语言（与 PreferenceManager.IsZh 同口径）。
        // 仅用于日志文案；配置缺失时不当作中文，回落英文，避免因为一句日志把启动流程炸掉。
        private static bool IsZh()
        {
            var language = EFC.Projects != null && !EFC.Projects.AutoLanguage
                ? EFC.Projects.DisplayLanguage
                : Application.systemLanguage;
            return language is SystemLanguage.Chinese
                or SystemLanguage.ChineseSimplified
                or SystemLanguage.ChineseTraditional;
        }

        // 中英双语文案择一输出。
        private static string L(string zh, string en) => IsZh() ? zh : en;

        private void Warn(string zh, string en)
        {
            D.Warning($"{LogTag} {L(zh, en)}");
        }

        /// <summary>
        /// 解析契约实现：Inspector 显式引用优先，未指定时回退到自身/子物体自动查找。
        /// 接口无法被 Unity 序列化，字段用 MonoBehaviour 承载，这里转型成接口。
        /// </summary>
        private T ResolveRef<T>(MonoBehaviour explicitRef, string fieldName) where T : class
        {
            if (explicitRef != null)
            {
                if (explicitRef is T typed)
                    return typed;

                Warn($"{fieldName} 引用的组件 [{explicitRef.GetType().Name}] 未实现 {typeof(T).Name}，已忽略，改用自动查找。",
                    $"{fieldName} references component [{explicitRef.GetType().Name}] that does not implement {typeof(T).Name}; ignored, falling back to auto-discovery.");
            }

            return FindComponent<T>();
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