/*
 * ================================================
 * Describe:        This script is used to fill LauncherConfig hotfix dll entries.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 22:40:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-15 18:20:00
 * ScriptVersion:   0.2
 * ================================================
 */

using System.Collections.Generic;
using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Launcher.Editor
{
    /// <summary>
    /// 一步铺好 <see cref="LauncherConfig.HotfixDlls"/>：补条目 → 拓扑排序 → 绑定 Location / FallbackAsset。
    /// <para>One-step fill: add missing entries, order by dependency, bind Location / FallbackAsset.</para>
    /// </summary>
    /// <remarks>
    /// 绑定规则（.bytes 目录、Location 生成方式、是否覆盖）在
    /// 「EF 配置面板 → 热更程序集」里配置；这里直接复用保存的设置。
    /// </remarks>
    public static class LauncherHotfixDllFillTool
    {
        private const string DialogTitle = "EF.Launcher";

        [MenuItem(MenuItemToolkit.Launcher + "📦 Fill LauncherConfig Hotfix Dlls", false, MenuItemToolkit.LauncherPriority + 54)]
        public static void FillFromMenu()
        {
            var config = LauncherHotfixDllSortTool.ResolveTargetConfig(createIfMissing: true);
            if (config == null)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                    "没找到唯一的 LauncherConfig。\n\n请先在 Project 窗口选中要填充的配置再执行。",
                    "No single LauncherConfig found.\n\nSelect the config to fill in the Project window first."),
                    LC.Tr("确定", "OK"));
                return;
            }

            var result = LauncherHotfixEntryBinder.Build(config, HotfixBindOptions.Load(), apply: true);
            string message = Describe(config, result);

            Debug.Log("[EF.Launcher] " + message.Replace("\n", "\n[EF.Launcher] "));
            EditorUtility.DisplayDialog(DialogTitle, message, LC.Tr("好", "OK"));
        }

        /// <summary>
        /// 给「加入热更列表」流程用：不创建配置，找不到就返回 -1。
        /// </summary>
        /// <returns>新增条目数；-1 表示没有可用配置</returns>
        internal static int FillExistingConfig(out int total)
        {
            total = 0;

            var config = LauncherHotfixDllSortTool.ResolveTargetConfig(createIfMissing: false);
            if (config == null)
                return -1;

            var result = LauncherHotfixEntryBinder.Build(config, HotfixBindOptions.Load(), apply: true);
            total = result.Rows.Count;
            return result.Added;
        }

        private static string Describe(LauncherConfig config, HotfixBindResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
                return result.Error;

            var lines = new List<string>
            {
                LC.Tr(
                    $"「{config.name}」已更新：共 {result.Rows.Count} 条，新增 {result.Added} 条，" +
                    $"绑定 Location {result.LocationBound} 条 / FallbackAsset {result.FallbackBound} 条。",
                    $"\"{config.name}\" updated: {result.Rows.Count} total, {result.Added} added, " +
                    $"{result.LocationBound} locations / {result.FallbackBound} fallbacks bound."),
            };

            if (result.Missing > 0)
            {
                lines.Add(LC.Tr(
                    $"⚠ 有 {result.Missing} 条找不到 .dll.bytes，Location / FallbackAsset 留空：" +
                    "请先执行 HybridCLR/Generate/All 与 BuildAssetsAndCopyToStreamingAssets，" +
                    "或到「EF 配置面板 → 热更程序集」调整 .bytes 目录。",
                    $"⚠ {result.Missing} entries have no matching .dll.bytes and were left blank. " +
                    "Run HybridCLR/Generate/All + BuildAssetsAndCopyToStreamingAssets, " +
                    "or adjust the .bytes folder in the EF config panel."));

                foreach (var row in result.Rows)
                {
                    if (row.AssetPath == null)
                        lines.Add("   - " + row.AssemblyName);
                }
            }

            lines.Add(LC.Tr(
                "绑定规则可在「EF 配置面板 → 热更程序集」修改；只想重跑绑定可用 " +
                "🔗 Auto Bind Hotfix Dll Entries。",
                "Rules live in the EF config panel (Hotfix Assemblies); " +
                "use 🔗 Auto Bind Hotfix Dll Entries to rebind only."));

            return string.Join("\n\n", lines);
        }
    }
}
