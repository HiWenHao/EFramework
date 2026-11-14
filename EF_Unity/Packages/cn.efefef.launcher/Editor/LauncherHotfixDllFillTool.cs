/*
 * ================================================
 * Describe:        This script is used to fill LauncherConfig hotfix dll entries.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 22:40:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 22:40:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Launcher.Editor
{
    /// <summary>
    /// 按依赖序铺好 <see cref="LauncherConfig.HotfixDlls"/> 骨架：缺失的 EF 程序集自动补条目，已有的保持原样。
    /// <para>Builds the HotfixDlls skeleton in dependency order. Existing entries are preserved untouched.</para>
    /// </summary>
    /// <remarks>
    /// 只负责「补条目 + 排序」，不猜 YooAsset 地址之外的任何东西：
    /// Location 默认写 <c>HotfixDlls/&lt;程序集名&gt;.dll</c>，是占位路径，用户按自己的收集器规则改。
    /// </remarks>
    public static class LauncherHotfixDllFillTool
    {
        private const string MenuRoot = MenuItemToolkit.Utility;
        private const string DialogTitle = "EF.Launcher";
        private const string LocationFolder = "HotfixDlls";

        [MenuItem(MenuRoot + "📦 Fill LauncherConfig Hotfix Dlls", false, MenuItemToolkit.UtilityPriority + 54)]
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

            int added = Fill(config, out int total, out List<string> unknown);

            var message = LC.Tr(
                $"「{config.name}」已更新：新增 {added} 条，共 {total} 条，已按依赖序排好。",
                $"\"{config.name}\" updated: {added} added, {total} total, ordered by dependency.");

            if (unknown.Count > 0)
            {
                message += "\n\n" + LC.Tr(
                    "以下条目找不到同名 asmdef，已排在最后：",
                    "These entries have no matching asmdef and were placed last:") +
                    "\n  - " + string.Join("\n  - ", unknown);
            }

            message += "\n\n" + LC.Tr(
                "Location 目前是占位路径（HotfixDlls/<程序集名>.dll），请按你的 YooAsset 收集器规则改。",
                "Location currently holds a placeholder path (HotfixDlls/<assembly>.dll); adjust it to your YooAsset collector rules.");

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

            return Fill(config, out total, out _);
        }

        private static int Fill(LauncherConfig config, out int total, out List<string> unknown)
        {
            var database = AsmdefDatabase.Build();
            var efAssemblies = LauncherHotUpdateListTool.CollectEfRuntimeAsmdefs(database);

            var merged = new List<HotUpdateDllEntry>();
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (config.HotfixDlls != null)
            {
                foreach (var entry in config.HotfixDlls)
                {
                    if (entry == null)
                        continue;

                    merged.Add(entry);

                    string name = LauncherHotfixDllSortTool.ResolveAssemblyName(entry);
                    if (!string.IsNullOrEmpty(name))
                        existingNames.Add(name);
                }
            }

            int added = 0;
            foreach (var info in efAssemblies)
            {
                if (existingNames.Contains(info.Name))
                    continue;

                merged.Add(new HotUpdateDllEntry
                {
                    Name = info.Name,
                    Location = LocationFolder + "/" + info.Name + ".dll",
                    Enabled = true,
                });

                existingNames.Add(info.Name);
                added++;
            }

            var sorted = LauncherHotfixDllSortTool.Sort(merged.ToArray(), out unknown, out bool changed);

            if (added > 0 || changed || config.HotfixDlls == null || config.HotfixDlls.Length != sorted.Length)
            {
                config.HotfixDlls = sorted;
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            total = sorted.Length;
            return added;
        }
    }
}
