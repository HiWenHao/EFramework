/*
 * ================================================
 * Describe:        This script is used to store the launcher settings.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 15:58:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-16 11:20:00
 * ScriptVersion:   0.2
 * ================================================
 */

using System;
using UnityEngine;
using YooAsset;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 一个热更程序集的来源
    /// <para>Source of a single hot update assembly</para>
    /// </summary>
    [Serializable]
    public sealed class HotUpdateDllEntry
    {
        [Tooltip("诊断用名称，建议与 dll 同名")]
        public string Name = string.Empty;

        [Tooltip("YooAsset 定位地址；留空则使用 FallbackAsset")]
        public string Location = string.Empty;

        [Tooltip("随包兜底（编辑器模拟模式 / 离线包）。填了 Location 时不会用到")]
        public TextAsset FallbackAsset;

        [Tooltip("取消勾选可临时跳过该程序集")]
        public bool Enabled = true;
    }

    /// <summary>
    /// 启动配置：<b>唯一真源</b>。框架侧 ProjectConfig 虽常驻 AOT，但不作为启动配置来源，
    /// 以免启动段依赖框架初始化顺序；语言、版本等展示信息统一经 <see cref="LauncherCoreBridge"/> 读取。
    /// <para>Launcher configuration. Single source of truth. The AOT-resident framework ProjectConfig is
    /// deliberately not a boot config source; display data such as language and version is read via
    /// <see cref="LauncherCoreBridge"/>.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "LauncherConfig", menuName = "EF/Launcher Config", order = 201)]
    public sealed class LauncherConfig : ScriptableObject
    {
        #region 资源包

        [Header("资源包 / Package")] [Tooltip("编辑器下建议 EditorSimulateMode，出包用 HostPlayMode")]
        public EPlayMode PlayMode = EPlayMode.HostPlayMode;

        [Tooltip("YooAsset 包名，需与资源构建配置一致")] public string PackageName = "DefaultPackage";

        [Tooltip("主 CDN 地址（HostPlayMode 必填）")] public string MainUrl = string.Empty;

        [Tooltip("备用 CDN 地址，主地址失败时回落")] public string FallbackUrl = string.Empty;

        [Min(1), Tooltip("单次网络请求超时（秒）")] public int TimeoutSeconds = 60;

        [Min(1), Tooltip("同时下载的最大文件数")] public int DownloadingMaxNumber = 10;

        [Min(0), Tooltip("单个文件下载失败后的重试次数")] public int FailedTryAgain = 3;

        #endregion

        #region 重试 / Retry

        [Header("重试 / Retry")]
        [Min(0), Tooltip("启动流程最大重试次数；0 表示使用内置默认值，负数按 0 处理")]
        public int MaxRetryCount = 0;

        [Min(0), Tooltip("每次重试前的等待秒数；0 表示使用内置默认值。首次启动不等待")]
        public float RetryDelaySeconds = 0f;

        [Tooltip("失败后自动按退避重试；默认关闭，由界面按钮触发")]
        public bool AutoRetryOnFailure = false;

        #endregion

        #region 程序集

        [Header("AOT 元数据 / AOT metadata")]
        [Tooltip("随包体一起发布的裁剪后 AOT dll（mscorlib / System / System.Core 等），建议与 HybridCLRSettings.patchAOTAssemblies 保持一致")]
        public TextAsset[] AotMetadataDlls = Array.Empty<TextAsset>();

        [Header("热更程序集 / Hot update assemblies")]
        [Tooltip("顺序即加载顺序（拓扑序）：被依赖的排前面。Core-AOT 形态下 EF.Core 已在包体，不应出现在此列表")]
        public HotUpdateDllEntry[] HotfixDlls = Array.Empty<HotUpdateDllEntry>();

        #endregion

        #region 入口

        [Header("热更入口 / Hotfix Entry")] [Tooltip("入口类型的完整名称，例如 EFExample.HotfixTest")]
        public string EntryType = string.Empty;

        [Tooltip("入口方法名，必须是 public/private static 无参方法")]
        public string EntryMethod = "Init";

        #endregion
    }
}