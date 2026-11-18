/*
 * ================================================
 * Describe:        This script is used to define launcher states and error codes.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 15:58:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-18 22:05:00
 * ScriptVersion:   0.5
 * ================================================
 */

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 启动流程状态。
    /// <para>Launcher pipeline state.</para>
    /// </summary>
    public enum LauncherState
    {
        /// <summary>
        /// 未启动
        /// <para>Not started.</para>
        /// </summary>
        Idle = 0,

        /// <summary>
        /// 项目钩子：隐私协议 / 渠道 SDK / 登录
        /// <para>Project hook: privacy consent, channel SDK, login.</para>
        /// </summary>
        Booting = 1,

        /// <summary>
        /// YooAsset 资源包初始化
        /// <para>Initializing the YooAsset resource package.</para>
        /// </summary>
        Initializing = 2,

        /// <summary>
        /// 请求远端版本 + 更新清单
        /// <para>Requesting the remote version and updating the manifest.</para>
        /// </summary>
        CheckingVersion = 3,

        /// <summary>
        /// 下载资源
        /// <para>Downloading resources.</para>
        /// </summary>
        Downloading = 4,

        /// <summary>
        /// 加载 AOT 补充元数据
        /// <para>Loading AOT supplement metadata.</para>
        /// </summary>
        LoadingMetadata = 5,

        /// <summary>
        /// 加载热更程序集
        /// <para>Loading hot update assemblies.</para>
        /// </summary>
        LoadingHotfix = 6,

        /// <summary>
        /// 调用热更入口
        /// <para>Invoking the hot update entry point.</para>
        /// </summary>
        Invoking = 7,

        /// <summary>
        /// 启动完成，控制权已交给热更层
        /// <para>Boot completed; control has been handed to the hot update layer.</para>
        /// </summary>
        Completed = 8,

        /// <summary>
        /// 启动失败，可通过 <see cref="Launcher.Retry"/> 重试
        /// <para>Boot failed; retry via <see cref="Launcher.Retry"/>.</para>
        /// </summary>
        Failed = 9,

        /// <summary>
        /// 失败后等待退避，即将重试
        /// <para>Waiting for backoff before the next retry.</para>
        /// </summary>
        Retrying = 10,

        /// <summary>
        /// 资源与热更程序集已就绪，等待用户确认后进入游戏（可选闸门）
        /// <para>Resources and hot update assemblies are ready; waiting for user confirmation before entering the game (optional gate).</para>
        /// </summary>
        WaitingForConfirm = 11,
    }

    /// <summary>
    /// 启动失败原因
    /// <para>Launcher failure reason.</para>
    /// </summary>
    public enum LauncherErrorCode
    {
        /// <summary>
        /// 配置缺失或不合法
        /// <para>Configuration is missing or invalid.</para>
        /// </summary>
        ConfigInvalid = 0,

        /// <summary>
        /// YooAsset 资源包创建 / 初始化失败
        /// <para>Failed to create or initialize the YooAsset package.</para>
        /// </summary>
        PackageInitFailed = 1,

        /// <summary>
        /// 请求资源版本失败
        /// <para>Failed to request the resource version.</para>
        /// </summary>
        VersionRequestFailed = 2,

        /// <summary>
        /// 更新资源清单失败
        /// <para>Failed to update the resource manifest.</para>
        /// </summary>
        ManifestUpdateFailed = 3,

        /// <summary>
        /// 资源下载失败
        /// <para>Failed to download resources.</para>
        /// </summary>
        DownloadFailed = 4,

        /// <summary>
        /// AOT 补充元数据加载失败
        /// <para>Failed to load AOT supplement metadata.</para>
        /// </summary>
        MetadataLoadFailed = 5,

        /// <summary>
        /// 热更程序集加载失败
        /// <para>Failed to load hot update assemblies.</para>
        /// </summary>
        AssemblyLoadFailed = 6,

        /// <summary>
        /// 找不到热更入口类型或方法
        /// <para>Hot update entry type or method was not found.</para>
        /// </summary>
        EntryNotFound = 7,

        /// <summary>
        /// 热更入口执行时抛出异常
        /// <para>The hot update entry threw an exception.</para>
        /// </summary>
        EntryThrew = 8,

        /// <summary>
        /// 项目启动钩子抛出异常
        /// <para>The project boot hook threw an exception.</para>
        /// </summary>
        HookFailed = 9,

        /// <summary>
        /// 重试次数已用尽
        /// <para>Retry attempts exhausted.</para>
        /// </summary>
        RetryExhausted = 10,

        /// <summary>
        /// 启动流程抛出未预期的异常（协程时代会被 Unity 吞掉）
        /// <para>An unexpected exception escaped the boot pipeline (Unity would swallow it in the coroutine era).</para>
        /// </summary>
        Unexpected = 11,

        /// <summary>
        /// 进入游戏前的确认钩子抛出异常
        /// <para>The pre-entry confirmation hook threw an exception.</para>
        /// </summary>
        ConfirmFailed = 12,
    }
}