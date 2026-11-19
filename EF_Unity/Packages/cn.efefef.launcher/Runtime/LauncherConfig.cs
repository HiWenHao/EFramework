/*
 * ================================================
 * Describe:        This script is used to store the launcher settings.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 15:58:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-11-19 11:42:00
 * ScriptVersion:   0.4
 * ================================================
 */

using System;
using UnityEngine;
using YooAsset;

namespace EasyFramework.Launcher
{
    /// <summary>
    /// 一个热更程序集的来源
    /// <para>Source of a single hot update assembly.</para>
    /// </summary>
    [Serializable]
    public sealed class HotUpdateDllEntry
    {
        [HeaderPro("名称（建议与 dll 同名）", "Name (match the dll name)", HeaderProType.Note)]
        public string Name = string.Empty;

        [HeaderPro("定位地址（空则用兜底资源）", "Location (empty uses fallback)", HeaderProType.Note)]
        public string Location = string.Empty;

        [HeaderPro("随包兜底资源（编辑器模拟 / 离线包）", "Bundled fallback (simulate / offline)", HeaderProType.Note)]
        public TextAsset FallbackAsset;

        [HeaderPro("启用", "Enabled", HeaderProType.Note)] public bool Enabled = true;
    }

    /// <summary>
    /// 启动配置
    /// <para>Launcher configuration.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "LauncherConfig", menuName = "EF/Launcher Config", order = 201)]
    public sealed class LauncherConfig : ScriptableObject
    {
        #region 资源包 / Package

        // 每个分组起点：横线 + 加粗暖黄标题；组内说明用 Note，关键约束用 BoldNote
        [HeaderPro("", "", HeaderProType.Separator)]
        [HeaderPro("资源包 · 运行模式（编辑器模拟 / 出包主机）", "Package · play mode (simulate / host)", HeaderProType.Title)]
        public EPlayMode PlayMode = EPlayMode.HostPlayMode;

        [HeaderPro("资源包名（与构建配置一致）", "Package name (match build config)", HeaderProType.Note)]
        public string PackageName = "DefaultPackage";

        [HeaderPro("请求超时（秒）", "Request timeout (s)", HeaderProType.Note)]
        public int TimeoutSeconds = 60;

        [HeaderPro("最大并发下载数", "Max concurrent downloads", HeaderProType.Note)]
        public int DownloadingMaxNumber = 10;

        [HeaderPro("单文件下载重试次数", "Per-file retry count", HeaderProType.Note)]
        public int FailedTryAgain = 3;

        #endregion

        #region 重试 / Retry

        [HeaderPro("", "", HeaderProType.Separator)]
        [HeaderPro("重试 · 最大次数（0 = 默认）", "Retry · max count (0 = default)", HeaderProType.Title)]
        public int MaxRetryCount = 0;

        [HeaderPro("重试间隔（秒，0 = 默认；首次不等待）", "Retry delay (s, 0 = default; no wait on first)", HeaderProType.Note)]
        public float RetryDelaySeconds = 0f;

        [HeaderPro("失败后自动退避重试（默认关，通常由界面触发）", "Auto retry with backoff (off by default, UI-triggered)", HeaderProType.BoldNote)]
        public bool AutoRetryOnFailure = false;

        #endregion

        #region 程序集 / Assemblies

        [HeaderPro("", "", HeaderProType.Separator)]
        [HeaderPro("AOT 元数据（与 HybridCLR patchAOTAssemblies 一致）", "AOT metadata (match HybridCLR patchAOTAssemblies)", HeaderProType.Title)]
        public TextAsset[] AotMetadataDlls = Array.Empty<TextAsset>();

        [HeaderPro("热更程序集（顺序 = 拓扑加载序，被依赖者在前）", "Hotfix assemblies (topological load order)", HeaderProType.BoldNote)]
        public HotUpdateDllEntry[] HotfixDlls = Array.Empty<HotUpdateDllEntry>();

        #endregion

        #region 入口 / Entry

        [HeaderPro("", "", HeaderProType.Separator)]
        [HeaderPro("热更入口 · 类型全名（如 EFExample.HotfixTest）", "Entry · type full name (e.g. EFExample.HotfixTest)", HeaderProType.Title)]
        public string EntryType = string.Empty;

        [HeaderPro("入口方法（public/private static 无参）", "Entry method (public/private static, no args)", HeaderProType.BoldNote)]
        public string EntryMethod = "Init";

        #endregion

        #region 配置自检

        // Inspector 里就把能查的查掉：这些错要到运行时才暴露，表现是"启动卡住"或"出包必崩"
        private void OnValidate()
        {
            if (HotfixDlls != null)
            {
                for (int i = 0; i < HotfixDlls.Length; i++)
                {
                    var entry = HotfixDlls[i];
                    if (entry is not { Enabled: true })
                        continue;

                    if (string.IsNullOrWhiteSpace(entry.Name))
                    {
                        D.Warning(
                            $"[ LauncherConfig ] {name}: HotfixDlls[{i}] 的 Name 为空，" +
                            "重试去重、拓扑排序、失效条目检测都会认错它。");
                    }

                    if (string.IsNullOrWhiteSpace(entry.Location) && entry.FallbackAsset == null)
                    {
                        D.Warning(
                            $"[ LauncherConfig ] {name}: HotfixDlls[{i}]（{entry.Name}）既没有 Location " +
                            "也没有 FallbackAsset，启动时必然加载不到。");
                    }
                }
            }

            if (AotMetadataDlls == null) return;

            for (int i = 0; i < AotMetadataDlls.Length; i++)
            {
                if (AotMetadataDlls[i] != null)
                    continue;

                D.Warning(
                    $"[ LauncherConfig ] {name}: AotMetadataDlls[{i}] 引用丢失（重新 Generate 后 .meta GUID 会变），" +
                    "跑一次 🔄 Sync Hotfix Dll Entries 重绑。");
            }
        }

        #endregion
    }
}
