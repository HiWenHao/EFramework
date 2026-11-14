/*
 * ================================================
 * Describe:        This script is used to define launcher states and error codes.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 15:58:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 15:58:00
 * ScriptVersion:   0.1
 * ================================================
 */

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 启动流程状态
    /// <para>Launcher pipeline state</para>
    /// </summary>
    public enum LauncherState
    {
        /// <summary>未启动</summary>
        Idle = 0,

        /// <summary>项目钩子（隐私协议 / 渠道 SDK / 登录）</summary>
        Booting = 1,

        /// <summary>YooAsset 资源包初始化</summary>
        Initializing = 2,

        /// <summary>请求远端版本 + 更新清单</summary>
        CheckingVersion = 3,

        /// <summary>下载资源</summary>
        Downloading = 4,

        /// <summary>加载 AOT 补充元数据</summary>
        LoadingMetadata = 5,

        /// <summary>加载热更程序集</summary>
        LoadingHotfix = 6,

        /// <summary>调用热更入口</summary>
        Invoking = 7,

        /// <summary>启动完成，控制权已交给热更层</summary>
        Completed = 8,

        /// <summary>启动失败，可通过 <see cref="Launcher.Retry"/> 重试</summary>
        Failed = 9,
    }

    /// <summary>
    /// 启动失败原因
    /// <para>Launcher failure reason</para>
    /// </summary>
    public enum LauncherErrorCode
    {
        /// <summary>配置缺失或不合法</summary>
        ConfigInvalid = 0,

        /// <summary>YooAsset 资源包创建 / 初始化失败</summary>
        PackageInitFailed = 1,

        /// <summary>请求远端版本失败</summary>
        VersionRequestFailed = 2,

        /// <summary>更新资源清单失败</summary>
        ManifestUpdateFailed = 3,

        /// <summary>资源下载失败</summary>
        DownloadFailed = 4,

        /// <summary>AOT 补充元数据加载失败</summary>
        MetadataLoadFailed = 5,

        /// <summary>热更程序集加载失败</summary>
        AssemblyLoadFailed = 6,

        /// <summary>找不到热更入口类型或方法</summary>
        EntryNotFound = 7,

        /// <summary>热更入口执行时抛出异常</summary>
        EntryThrew = 8,
    }
}