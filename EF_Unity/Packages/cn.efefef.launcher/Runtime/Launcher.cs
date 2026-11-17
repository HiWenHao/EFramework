/*
 * ================================================
 * Describe:        This script is used to run the AOT boot kernel.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 15:58:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-17 11:20:00
 * ScriptVersion:   0.4
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

        #region 字段

        [Header("配置 / Config")] [SerializeField]
        private LauncherConfig _config;

        [Header("运行 / Runtime")] [SerializeField]
        private bool _runOnStart = true;

        [SerializeField] private bool _dontDestroyOnLoad = true;
        [SerializeField] private bool _verbose = true;

        [Header("事件通道 / Event channels（可选）")] [SerializeField]
        private LauncherStateEvent _stateEvent;

        [SerializeField] private LauncherProgressEvent _progressEvent;
        [SerializeField] private LauncherErrorEvent _errorEvent;

        private const int DefaultMaxRetryCount = 3;

        private const float DefaultRetryDelaySeconds = 2f;

        // 进度按 10Hz 广播：界面每帧拼字符串的代价远大于肉眼可见差异
        private const float ProgressIntervalSeconds = 0.1f;

        // 重试耗尽时给界面的空回调，避免界面拿到 null 后空引用
        private static readonly Action NoopRetry = () => { };

        private ILauncherView _view;
        private ILauncherHook _hook;
        private ResourcePackage _package;
        private bool _running;

        private int _retryCount;

        // 退避等待是否已排上；UniTask 没有 Coroutine 句柄，用标记位防止重复排程
        private bool _retryScheduled;

        // 随 GameObject 销毁自动取消，异步流程不会在已销毁的对象上继续跑
        private CancellationToken _destroyToken;
        private float _startTime;

        private readonly List<Assembly> _hotfixAssemblies = new List<Assembly>();

        // Assembly.Load(byte[]) 每次都会创建新程序集；重试时重复加载同名 dll 会产生第二份类型身份
        private readonly HashSet<string> _loadedAssemblyNames = new HashSet<string>();

        #endregion

        #region 属性

        /// <summary>当前状态</summary>
        public LauncherState State { get; private set; } = LauncherState.Idle;

        /// <summary>启动配置</summary>
        public LauncherConfig Config => _config;

        /// <summary>已加载的热更程序集（按配置顺序）</summary>
        public IReadOnlyList<Assembly> HotfixAssemblies => _hotfixAssemblies;

        /// <summary>
        /// 详细日志开关。只挡 <see cref="LauncherState"/> 流转这类信息日志，
        /// <b>警告与错误永远输出</b>；失败时的 <c>Diagnose</c> 现场也不受它控制。
        /// 运行时（例如 <c>AddComponent</c> 之后）可直接赋值，替代只在 Inspector 上能改的序列化字段。
        /// <para>Verbose switch for lifecycle logs. Warnings, errors and the failure diagnostic are never suppressed.</para>
        /// </summary>
        public bool Verbose
        {
            get => _verbose;
            set => _verbose = value;
        }

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
            _destroyToken = this.GetCancellationTokenOnDestroy();

            if (_view == null)
                Warn("未找到 ILauncherView 实现，启动过程将没有界面反馈。");

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
            // 退避等待期间 _running 已是 false，不拦 _retryScheduled 会并发跑起第二条管线
            if (_running || _retryScheduled || State == LauncherState.Completed)
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
                Warn("启动流程运行中，忽略 Configure（配置只可在启动前注入）。");
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
            if (_running || State == LauncherState.Completed || _retryScheduled)
                return;

            int max = _config != null && _config.MaxRetryCount > 0
                ? _config.MaxRetryCount
                : DefaultMaxRetryCount;

            if (_retryCount >= max)
            {
                string message = $"重试次数已用尽（{_retryCount}/{max}）";
                D.Error($"{LogTag} 启动失败 [{LauncherErrorCode.RetryExhausted}] {message}");
                SetState(LauncherState.Failed, message);
                BroadcastError(LauncherErrorCode.RetryExhausted, message, NoopRetry);
                return;
            }

            _retryCount++;
            float delay = _config != null && _config.RetryDelaySeconds > 0f
                ? _config.RetryDelaySeconds
                : DefaultRetryDelaySeconds;

            SetState(LauncherState.Retrying, "准备重试");
            _retryScheduled = true;
            RetryAfterDelayAsync(delay, _destroyToken).Forget();
        }

        #endregion

        #region 重试实现

        // 退避只作用于重试，首次启动不等待；用 realtime 避免 timeScale 为 0 时卡死
        private async UniTask RetryAfterDelayAsync(float delay, CancellationToken token)
        {
            Log($"第 {_retryCount} 次重试将在 {delay:0.0}s 后开始");

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
            SetState(LauncherState.Idle, "重试");
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
                // GameObject 已销毁：静默收场，不再触碰已失效的 view / 事件通道
            }
            catch (Exception e)
            {
                // 协程时代这里会被 Unity 吞掉（表现为流程静默中断）；UniTask 下必须自己收口
                D.Exception(e);
                Fail(LauncherErrorCode.Unexpected, $"启动流程异常：{e.Message}");
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
            Log($"启动开始 | 版本={Application.version} 语言={DescribeLanguage(EFC.Projects)}");

            if (!ValidateConfig(out string configError))
            {
                Fail(LauncherErrorCode.ConfigInvalid, configError);
                return;
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
                    Fail(LauncherErrorCode.HookFailed, $"ILauncherHook.OnBoot 抛出异常：{e.Message}");
                    return;
                }

                while (!finished)
                    await UniTask.NextFrame(token);
            }

            #endregion

            #region 2. 资源系统

            SetState(LauncherState.Initializing, "初始化资源系统");

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
                    Fail(LauncherErrorCode.PackageInitFailed, $"资源包初始化失败：{initOp.Error}");
                    return;
                }
            }

            #endregion

            #region 3. 版本 + 清单

            SetState(LauncherState.CheckingVersion, "检查资源版本");
            var versionOp = _package.RequestPackageVersionAsync(timeout: _config.TimeoutSeconds);
            await versionOp.Task;
            token.ThrowIfCancellationRequested();
            if (versionOp.Status != EOperationStatus.Succeed)
            {
                Fail(LauncherErrorCode.VersionRequestFailed, $"获取资源版本失败：{versionOp.Error}");
                return;
            }

            var manifestOp = _package.UpdatePackageManifestAsync(versionOp.PackageVersion, _config.TimeoutSeconds);
            await manifestOp.Task;
            token.ThrowIfCancellationRequested();
            if (manifestOp.Status != EOperationStatus.Succeed)
            {
                Fail(LauncherErrorCode.ManifestUpdateFailed, $"更新资源清单失败：{manifestOp.Error}");
                return;
            }

            #endregion

            #region 4. 下载

            SetState(LauncherState.Downloading, "更新资源");
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
                    Fail(LauncherErrorCode.DownloadFailed, $"资源下载失败：{downloader.Error}");
                    return;
                }
            }

            #endregion

            #region 5. AOT 补充元数据（必须在热更程序集之前）

            SetState(LauncherState.LoadingMetadata, "加载 AOT 元数据");
            if (!TryLoadAotMetadata(out string metadataError))
            {
                Fail(LauncherErrorCode.MetadataLoadFailed, metadataError);
                return;
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
                    string assemblyName = ResolveAssemblyName(entry, entryName);

                    if (assemblyName != null && _loadedAssemblyNames.Contains(assemblyName))
                    {
                        Log($"跳过已加载的热更程序集：{assemblyName}");
                        continue;
                    }

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
                            Log($"复用已编译程序集（编辑器模拟模式）：{compiled.GetName().Name}");
                            continue;
                        }

                        // 编辑器里没有同名编译产物（例如只随包发布的外部业务 dll）→ 退回 bytes 加载
                        Log($"编辑器里没有 {assemblyName} 的编译产物，改为从 bytes 加载");
                    }
#endif

                    byte[] dllBytes = null;

                    if (!string.IsNullOrEmpty(entry.Location))
                    {
                        var handle = _package.LoadAssetAsync(entry.Location, typeof(TextAsset));
                        // 直接 await Provider 的 Task（完成即续，零帧延迟）；句柄失效时它返回 null，用轮询兜底
                        if (handle.Task is { } loadTask)
                            await loadTask;
                        else
                            await UniTask.WaitUntil(() => handle.IsDone, cancellationToken: token);

                        token.ThrowIfCancellationRequested();

                        EOperationStatus status = handle.Status;
                        string lastError = handle.LastError;
                        if (status == EOperationStatus.Succeed)
                        {
                            var textAsset = handle.GetAssetObject<TextAsset>();
                            dllBytes = textAsset != null ? textAsset.bytes : null;
                        }

                        // 无论成功失败都释放句柄，失败分支不能泄漏
                        handle.Release();

                        if (status != EOperationStatus.Succeed)
                        {
                            Fail(LauncherErrorCode.AssemblyLoadFailed,
                                $"热更程序集加载失败：{entryName}（{lastError}）");
                            return;
                        }
                    }
                    else if (entry.FallbackAsset != null)
                    {
                        dllBytes = entry.FallbackAsset.bytes;
                    }

                    if (dllBytes == null || dllBytes.Length == 0)
                    {
                        Fail(LauncherErrorCode.AssemblyLoadFailed, $"热更程序集内容为空：{entryName}");
                        return;
                    }

                    try
                    {
                        var assembly = Assembly.Load(dllBytes);
                        _hotfixAssemblies.Add(assembly);
                        _loadedAssemblyNames.Add(assembly.GetName().Name);
                        if (assemblyName != null)
                            _loadedAssemblyNames.Add(assemblyName);
                        Log($"已加载热更程序集：{entryName}（{assembly.GetName().Name}）");
                    }
                    catch (Exception e)
                    {
                        Fail(LauncherErrorCode.AssemblyLoadFailed, $"热更程序集加载异常：{entryName} -> {e.Message}");
                        return;
                    }
                }
            }

            if (_hotfixAssemblies.Count == 0 && entries is { Length: > 0 })
            {
                Fail(LauncherErrorCode.AssemblyLoadFailed, "没有任何热更程序集被加载，请检查配置");
                return;
            }

            #endregion

            #region 7. 调热更入口

            SetState(LauncherState.Invoking, "启动游戏");
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

            SetState(LauncherState.Completed, "进入游戏");

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

            if (_config.PlayMode == EPlayMode.HostPlayMode &&
                string.IsNullOrWhiteSpace(EFC.Projects.ResourcesArea.InnerUrl))
            {
                error = "HostPlayMode 需要资源地址：请在 ProjectConfig.ResourcesArea 里填内网地址" +
                        "（该配置随包常驻 AOT，EFC 会在 AfterAssembliesLoaded 阶段加载它）";
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
                    error = $"补充元数据失败：{asset.name}（{code}）";
                    return false;
                }

                loaded++;
                Log($"已补充 AOT 元数据：{asset.name}");
            }

            // 配置了却引用丢失 = 配置错误。静默跳过会让「补元数据从未生效」变成隐形故障，
            // 后续只在热更代码踩到 AOT 泛型时以难以定位的方式崩掉。
            if (missing > 0)
            {
                error = $"AOT 元数据配置了 {assets.Length} 项，其中 {missing} 项引用丢失（常见于 .dll.bytes 重新导入后 .meta guid 变化）。" +
                        $"成功补充 {loaded} 项。请在 LauncherConfig 里重新绑定，或清空 AotMetadataDlls 以显式跳过。";
                return false;
            }

            return true;
        }

        // 反射调用热更入口。入口返回 IEnumerator / Task 时等它跑完再标 Completed，
        // 否则异步初始化的热更层（例如 OpenView().Forget()）会在启动界面还没退场时就被判为“已完成”。
        private async UniTask InvokeEntryStepAsync(StepResult result)
        {
            Type type = FindType(_config.EntryType);
            if (type == null)
            {
                MarkFailed(result, LauncherErrorCode.EntryNotFound,
                    $"找不到热更入口类型：{_config.EntryType}（需要命名空间全名，且对应程序集已加载）");
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
                    $"找不到无参入口方法：{_config.EntryType}.{_config.EntryMethod}()，必须是无参 static 方法");
                return;
            }

            object returned;
            try
            {
                returned = method.Invoke(null, null);
            }
            catch (TargetInvocationException e)
            {
                MarkFailed(result, LauncherErrorCode.EntryThrew, $"热更入口抛出异常：{e.InnerException ?? e}");
                return;
            }
            catch (Exception e)
            {
                MarkFailed(result, LauncherErrorCode.EntryThrew, $"热更入口调用失败：{e.Message}");
                return;
            }

            Log($"热更入口已执行：{_config.EntryType}.{_config.EntryMethod}()");

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
                    MarkFailed(result, LauncherErrorCode.EntryThrew, "热更入口异步任务被取消");
                }
                catch (Exception e)
                {
                    MarkFailed(result, LauncherErrorCode.EntryThrew, $"热更入口异步任务失败：{e.Message}");
                }

                return;
            }

            if (returned != null)
            {
                // 启动器是 AOT 程序集，拿不到 UniTask 类型；只能提示，不能替它等
                Warn($"热更入口返回 {returned.GetType().Name}，启动器不会等待该类型。" +
                     "需要「初始化完成后再标启动完成」请返回 IEnumerator 或 Task。");
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

        private void SetState(LauncherState state, string tip)
        {
            State = state;
            Log($"{state} | {tip}");

            if (_stateEvent != null)
            {
                // 事件通道与界面彼此隔离：任一订阅者抛异常都不能打断启动流程
                try
                {
                    _stateEvent.Raise(new LauncherStatePayload(state, tip));
                }
                catch (Exception e)
                {
                    D.Exception(e);
                }
            }

            if (_view == null)
                return;

            try
            {
                _view.OnStateChanged(state, tip);
            }
            catch (Exception e)
            {
                D.Exception(e);
            }
        }

        private void ReportProgress(long currentCount, long totalCount, long currentBytes, long totalBytes)
        {
            if (_progressEvent != null)
            {
                try
                {
                    _progressEvent.Raise(
                        new LauncherProgressPayload(currentCount, totalCount, currentBytes, totalBytes));
                }
                catch (Exception e)
                {
                    D.Exception(e);
                }
            }

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
            D.Error($"{LogTag} 启动失败 [{code}] {message}");
            // 状态必须走 SetState：直接赋属性会让事件通道永远收不到 Failed，埋点与独立加载屏会停在上一阶段
            SetState(LauncherState.Failed, message);
            Diagnose();

            BroadcastError(code, message, Retry);

            if (_config != null && _config.AutoRetryOnFailure)
                Retry();
        }

        // 事件通道与界面收到同一份错误，任一订阅者抛异常都不影响另一方
        private void BroadcastError(LauncherErrorCode code, string message, Action retry)
        {
            if (_errorEvent != null)
            {
                try
                {
                    _errorEvent.Raise(new LauncherErrorPayload(code, message, retry));
                }
                catch (Exception e)
                {
                    D.Exception(e);
                }
            }

            if (_view == null)
                return;

            try
            {
                _view.OnError(code, message, retry);
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
            D.Log($"{LogTag} 诊断 | 版本={Application.version} 状态={State} " +
                  $"已加载热更程序集={_hotfixAssemblies.Count} 重试={_retryCount} 耗时={elapsed:0.0}s " +
                  $"CoreReady={EFC.Projects != null}");
        }



        // 仅用于启动日志：配置缺失时不要因为一句日志把整条流程炸掉
        private static string DescribeLanguage(ProjectConfig project)
        {
            if (project == null)
                return "未知（ProjectConfig 未加载）";

            return project.AutoLanguage ? Application.systemLanguage.ToString() : project.DisplayLanguage.ToString();
        }

        private void Log(string message)
        {
            if (_verbose)
                D.Log($"{LogTag} {message}");
        }

        private void Warn(string message)
        {
            D.Warning($"{LogTag} {message}");
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

        #region 辅助类型

        // 协程方法不能带 out 参数，用一个小载体把失败原因带回主流程
        private sealed class StepResult
        {
            public bool Failed;
            public LauncherErrorCode Code;
            public string Error;
        }

        #endregion
    }
}