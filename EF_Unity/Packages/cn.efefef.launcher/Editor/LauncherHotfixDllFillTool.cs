/*
 * ================================================
 * Describe:        This script is used to sync LauncherConfig hotfix dll entries.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 22:40:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-16 18:36:00
 * ScriptVersion:   0.3
 * ================================================
 */

using System.Collections.Generic;
using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Launcher.Editor
{
    /// <summary>
    /// 热更程序集一键同步：补条目 → 绑定 Location / FallbackAsset → 按依赖拓扑排序。
    /// <para>One-step sync: add missing entries, bind Location / FallbackAsset, then sort by dependency.</para>
    /// </summary>
    /// <remarks>
    /// 这三步本来就是同一条管线（补 → 绑 → 排），原先拆成「Sort / Fill / Auto Bind」三个菜单，
    /// 只会让人猜该点哪个；而且 Auto Bind 与 Fill 调的是同一个 <see cref="LauncherHotfixEntryBinder.Build"/>。
    /// 绑定规则（.bytes 目录、Location 生成方式、是否覆盖）在「EF 配置面板 → 热更程序集」里配置。
    /// </remarks>
    public static class LauncherHotfixDllFillTool
    {
        private const string DialogTitle = "EF.Launcher";

        [MenuItem(MenuItemToolkit.Launcher + "🔄 Sync Hotfix Dll Entries", false, MenuItemToolkit.LauncherPriority + 53)]
        public static void SyncFromMenu()
        {
            var options = HotfixBindOptions.Load();
            var config = ResolveConfig(options);
            if (config == null)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Combine("Launcher.SelectConfigToFill"),
                    LC.Combine("Launcher.OkConfirm"));
                return;
            }

            var result = LauncherHotfixEntryBinder.Build(config, options, apply: true);
            var aot = LauncherAotMetadataBinder.Bind(config, options, apply: true);
            string message = Describe(config, result, aot);

            Debug.Log("[EF.Launcher] " + message.Replace("\n", "\n[EF.Launcher] "));
            EditorUtility.DisplayDialog(DialogTitle, message, LC.Combine("Launcher.Ok"));
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

        // 面板里记过的配置优先，其次交给「选中 / 唯一 / 新建」的通用解析
        private static LauncherConfig ResolveConfig(HotfixBindOptions options)
        {
            if (!string.IsNullOrEmpty(options.LastConfigGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(options.LastConfigGuid);
                if (!string.IsNullOrEmpty(path))
                {
                    var remembered = AssetDatabase.LoadAssetAtPath<LauncherConfig>(path);
                    if (remembered != null)
                        return remembered;
                }
            }

            return LauncherHotfixDllSortTool.ResolveTargetConfig(createIfMissing: true);
        }

        private static string Describe(LauncherConfig config, HotfixBindResult result, AotBindResult aot)
        {
            if (!string.IsNullOrEmpty(result.Error))
                return result.Error;

            var lines = new List<string>
            {
                LC.Format("Launcher.FillUpdated", config.name, result.Rows.Count,
                    result.Added, result.LocationBound, result.FallbackBound),
            };

            AppendAot(lines, aot);

            if (result.Missing > 0)
            {
                lines.Add(LC.Format("Launcher.FillMissing", result.Missing));

                foreach (var row in result.Rows)
                {
                    if (row.AssetPath == null)
                        lines.Add("   - " + row.AssemblyName);
                }
            }

            AppendOrder(lines, config);
            AppendStale(lines, result);

            lines.Add(LC.Combine("Launcher.RulesHint"));

            return string.Join("\n\n", lines);
        }

        // AOT 元数据单独一段：它是每次 Generate/All 之后最容易漏、且漏了必卡启动的一项
        private static void AppendAot(List<string> lines, AotBindResult aot)
        {
            if (aot == null)
                return;

            if (!string.IsNullOrEmpty(aot.Error))
            {
                lines.Add(aot.Error);
                return;
            }

            lines.Add(LC.Format("Launcher.AotSummary", aot.Bound, aot.Missing));

            if (aot.Missing == 0)
                return;

            lines.Add(LC.Combine("Launcher.AotMissingHeader"));
            foreach (string name in aot.MissingNames)
                lines.Add("   - " + name);
        }

        // 写回后的数组就是运行时真实加载顺序，直接念出来，省得再去 Inspector 里数
        private static void AppendOrder(List<string> lines, LauncherConfig config)
        {
            var entries = config.HotfixDlls;
            if (entries == null || entries.Length == 0)
                return;

            lines.Add(LC.Combine("Launcher.Reordered"));
            for (int i = 0; i < entries.Length; i++)
            {
                string name = string.IsNullOrEmpty(entries[i].Name)
                    ? LC.Combine("Launcher.UnnamedEntry")
                    : entries[i].Name;
                string mark = entries[i].Enabled ? string.Empty : LC.Combine("Launcher.Disabled");
                lines.Add($"  {i + 1}. {name}{mark}");
            }
        }

        // 条目名匹配不到热更列表里的 asmdef：多半是改名或拼错，原样保留但必须点名
        private static void AppendStale(List<string> lines, HotfixBindResult result)
        {
            if (result.Stale.Count == 0)
                return;

            lines.Add(LC.Combine("Launcher.UnknownAsmdef"));
            foreach (string name in result.Stale)
                lines.Add("  - " + name);
        }
    }
}
