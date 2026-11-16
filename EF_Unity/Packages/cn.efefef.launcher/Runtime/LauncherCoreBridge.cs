/*
 * ================================================
 * Describe:        This script is the only gateway from the boot stage into EF.Core.
 * Author:          Alvin8412
 * CreationTime:    2026-11-16 11:20:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-16 11:20:00
 * ScriptVersion:   0.1
 * ================================================
 */

using UnityEngine;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 启动段访问 EF.Core 的唯一入口：只读取常驻 AOT 的静态数据，不触碰任何框架管理器。
    /// <para>The single gateway from the boot stage into EF.Core. Reads AOT-only static data and never touches framework managers.</para>
    /// </summary>
    /// <remarks>
    /// 为什么需要这一层：EF.Core 常驻 AOT，Launcher 可以引用它；但 Core 里的管理器单例是按需注册的
    /// （取 Instance 才会注册并触发 Init），真正的资源系统实现由热更层包注入。
    /// 热更 DLL 加载前访问这些管理器，只会拿到未注入的默认实现或空引用。
    /// <para>Why: manager singletons resolve lazily and depend on hot-update-side injections,
    /// which do not exist until the hot update assemblies are loaded.</para>
    /// 启动配置仍以 <see cref="LauncherConfig"/> 为唯一真源，本类不提供任何 CDN / 资源包参数。
    /// <para>Boot configuration stays in <see cref="LauncherConfig"/>; this class exposes no CDN or package settings.</para>
    /// </remarks>
    public static class LauncherCoreBridge
    {
        private const string UnknownVersion = "unknown";

        /// <summary>
        /// 显示语言（框架静态配置缺失时回落到系统语言）。
        /// <para>Display language from the framework config; falls back to the system language.</para>
        /// </summary>
        public static SystemLanguage DisplayLanguage
        {
            get
            {
                EasyFramework.ProjectConfig config = ProjectConfigOrNull;
                if (config == null)
                    return Application.systemLanguage;

                return config.AutoLanguage ? Application.systemLanguage : config.DisplayLanguage;
            }
        }

        /// <summary>
        /// 应用版本，仅用于启动失败诊断，不参与启动配置。
        /// <para>App version for failure diagnostics only; never used as boot configuration.</para>
        /// </summary>
        public static string AppVersion
        {
            get
            {
                EasyFramework.ProjectConfig config = ProjectConfigOrNull;
                if (config == null || config.AppConst == null)
                    return UnknownVersion;

                return string.IsNullOrEmpty(config.AppConst.AppVersion)
                    ? UnknownVersion
                    : config.AppConst.AppVersion;
            }
        }

        /// <summary>
        /// 框架静态配置是否就绪（AfterAssembliesLoaded 之后可读）。
        /// <para>Whether the framework static config is available, after AfterAssembliesLoaded.</para>
        /// </summary>
        public static bool IsCoreReady => ProjectConfigOrNull != null;

        // 全启动段唯一一处读取框架静态配置的地方，其余文件由 AOT 边界门禁强制收口
        private static EasyFramework.ProjectConfig ProjectConfigOrNull => EFC.Projects;
    }
}
