/*
 * ================================================
 * Describe:        This script is used to sort hotfix dlls by asmdef dependency.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 22:30:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 22:30:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.Text;
using EasyFramework.Edit;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Launcher.Editor
{
    /// <summary>
    /// 按 asmdef 依赖图对 <see cref="LauncherConfig.HotfixDlls"/> 做拓扑排序：被依赖的排前面。
    /// <para>Topologically sorts LauncherConfig.HotfixDlls using the asmdef graph, so nobody has to order it by hand.</para>
    /// </summary>
    /// <remarks>
    /// 依赖关系的唯一可靠来源是 asmdef 之间的引用（热更 dll 就是从这些 asmdef 编译出来的），
    /// 所以这里不去解析 dll 元数据，而是按「条目名 → asmdef 名」匹配后拓扑排序。
    /// 匹配不到的条目（业务 dll、名字写错）会原样排到末尾，并在日志里点名，方便自查拼写。
    /// </remarks>
    public static class LauncherHotfixDllSortTool
    {
        private const string MenuRoot = MenuItemToolkit.Utility;
        private const string DialogTitle = "EF.Launcher";

        [MenuItem(MenuRoot + "🔀 Sort Hotfix Dlls By Dependency", false, MenuItemToolkit.UtilityPriority + 53)]
        public static void SortSelectedConfig()
        {
            var config = ResolveTargetConfig(createIfMissing: false);
            if (config == null)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                    "没找到 LauncherConfig。\n\n请先在 Project 窗口选中一个 LauncherConfig 资产再执行；" +
                    "工程里有多个时无法自动判断用哪个。",
                    "No LauncherConfig found.\n\nSelect one in the Project window first; " +
                    "when several exist the tool cannot guess which one to use."), LC.Tr("确定", "OK"));
                return;
            }

            var entries = config.HotfixDlls;
            if (entries == null || entries.Length == 0)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                    $"「{config.name}」里还没有热更程序集条目。",
                    $"\"{config.name}\" has no hotfix dll entries yet."), LC.Tr("确定", "OK"));
                return;
            }

            var result = Sort(entries, out var unknown, out var changed);
            if (!changed)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                    "当前顺序已经是依赖序，不需要调整。",
                    "The current order already matches the dependency order."), LC.Tr("确定", "OK"));
                return;
            }

            config.HotfixDlls = result;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            var sb = new StringBuilder();
            sb.AppendLine(LC.Tr("已按依赖序重排：", "Reordered by dependency:"));
            for (int i = 0; i < result.Length; i++)
            {
                string name = string.IsNullOrEmpty(result[i].Name) ? "(未命名)" : result[i].Name;
                string mark = result[i].Enabled ? string.Empty : LC.Tr("  [已禁用]", "  [disabled]");
                sb.AppendLine($"  {i + 1}. {name}{mark}");
            }

            if (unknown.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(LC.Tr(
                    "以下条目在工程里找不到同名 asmdef，无法判断依赖，已排在最后：",
                    "These entries have no asmdef with the same name, so their dependencies are unknown; they were placed last:"));
                foreach (string name in unknown)
                    sb.AppendLine("  - " + name);
            }

            Debug.Log("[EF.Launcher] " + sb.ToString().Replace("\n", "\n[EF.Launcher] "));
            EditorUtility.DisplayDialog(DialogTitle, sb.ToString(), LC.Tr("好", "OK"));
        }

        #region 排序

        /// <summary>
        /// 拓扑排序：被依赖的排前面；无法识别的条目原序追加到末尾
        /// </summary>
        internal static HotUpdateDllEntry[] Sort(
            HotUpdateDllEntry[] entries,
            out List<string> unknown,
            out bool changed)
        {
            unknown = new List<string>();
            var database = AsmdefDatabase.Build();

            // 条目程序集名 -> 条目下标（同名条目只排序第一个，其余原样跟随）
            var known = new List<int>();
            var unknownIndexes = new List<int>();
            var entryNames = new string[entries.Length];

            for (int i = 0; i < entries.Length; i++)
            {
                string name = ResolveAssemblyName(entries[i]);
                entryNames[i] = name;

                if (!string.IsNullOrEmpty(name) && database.Resolve(name) != null)
                    known.Add(i);
                else
                    unknownIndexes.Add(i);
            }

            // 候选集合（只考虑本次要排序的条目）
            var candidateNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (int index in known)
                candidateNames.Add(entryNames[index]);

            // 依赖图：只保留指向候选集合的边
            var dependencies = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (int index in known)
            {
                string name = entryNames[index];
                var list = new List<string>();
                var info = database.Resolve(name);

                foreach (string reference in info.References)
                {
                    string target = database.Resolve(reference)?.Name;
                    if (!string.IsNullOrEmpty(target) && candidateNames.Contains(target) &&
                        !string.Equals(target, name, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(target);
                    }
                }

                dependencies[name] = list;
            }

            // DFS 拓扑排序
            var orderedNames = new List<string>();
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (int index in known)
                Visit(entryNames[index], dependencies, visiting, done, orderedNames);

            // 按程序集名重排（同名条目保持原相对顺序）
            var buckets = new Dictionary<string, Queue<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (int index in known)
            {
                string name = entryNames[index];
                if (!buckets.TryGetValue(name, out var queue))
                {
                    queue = new Queue<int>();
                    buckets[name] = queue;
                }

                queue.Enqueue(index);
            }

            var result = new List<HotUpdateDllEntry>(entries.Length);
            foreach (string name in orderedNames)
                result.Add(entries[buckets[name].Dequeue()]);

            foreach (int index in unknownIndexes)
            {
                result.Add(entries[index]);

                string name = entryNames[index];
                unknown.Add(string.IsNullOrEmpty(name) ? LC.Tr("(未命名条目)", "(unnamed entry)") : name);
            }

            changed = !SameOrder(result, entries);
            return result.ToArray();
        }

        private static void Visit(
            string name,
            Dictionary<string, List<string>> dependencies,
            HashSet<string> visiting,
            HashSet<string> done,
            List<string> ordered)
        {
            if (done.Contains(name) || !visiting.Add(name))
                return;

            if (dependencies.TryGetValue(name, out var deps))
            {
                foreach (string dep in deps)
                    Visit(dep, dependencies, visiting, done, ordered);
            }

            visiting.Remove(name);
            done.Add(name);
            ordered.Add(name);
        }

        private static bool SameOrder(List<HotUpdateDllEntry> result, HotUpdateDllEntry[] original)
        {
            for (int i = 0; i < original.Length; i++)
            {
                if (!ReferenceEquals(result[i], original[i]))
                    return false;
            }

            return true;
        }

        #endregion

        #region 名称解析

        /// <summary>条目 → 程序集名：优先 Name，其次 Location 的文件名</summary>
        internal static string ResolveAssemblyName(HotUpdateDllEntry entry)
        {
            if (entry == null)
                return null;

            string raw = string.IsNullOrWhiteSpace(entry.Name) ? entry.Location : entry.Name;
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            return Normalize(raw);
        }

        private static string Normalize(string raw)
        {
            int slash = raw.LastIndexOfAny(new[] { '/', '\\' });
            if (slash >= 0 && slash + 1 < raw.Length)
                raw = raw.Substring(slash + 1);

            raw = raw.Trim();

            if (raw.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(0, raw.Length - 6);
            if (raw.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                raw = raw.Substring(0, raw.Length - 4);

            return raw;
        }

        internal static LauncherConfig ResolveTargetConfig(bool createIfMissing)
        {
            if (Selection.activeObject is LauncherConfig selected)
                return selected;

            string[] guids = AssetDatabase.FindAssets("t:LauncherConfig");
            if (guids.Length == 1)
                return AssetDatabase.LoadAssetAtPath<LauncherConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));

            // 工程里有多个时无法自动判断，交给用户选中；也不新增第三个
            if (!createIfMissing || guids.Length > 1)
                return null;

            const string folder = "Assets/Launcher";
            const string assetPath = folder + "/LauncherConfig.asset";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetFullPath(folder));
                AssetDatabase.Refresh();
            }

            var config = ScriptableObject.CreateInstance<LauncherConfig>();
            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log(LC.Tr($"[EF.Launcher] 已创建启动配置：{assetPath}",
                $"[EF.Launcher] Created launcher config: {assetPath}"));

            Selection.activeObject = config;
            return config;
        }

        #endregion
    }
}
