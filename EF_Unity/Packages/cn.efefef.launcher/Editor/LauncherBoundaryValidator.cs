/*
 * ================================================
 * Describe:        This script is used to validate the AOT boundary of EF.Launcher.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 16:38:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 21:35:00
 * ScriptVersion:   0.2
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EasyFramework.Edit;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EasyFramework.Launcher.Editor
{
    /// <summary>
    /// AOT 边界校验：保证 <c>EF.Launcher.Runtime</c> 永远不会（直接或传递）引用任何 <c>cn.efefef.*</c> 程序集。
    /// <para>Boundary guard: the launcher runtime must never reference any cn.efefef.* assembly, directly or transitively.</para>
    /// </summary>
    /// <remarks>
    /// 为什么不用 Roslyn 分析器当主闸门：
    /// 分析器只能看到 <c>Compilation.References</c>，这和 asmdef 是同一份信息，没有额外准确度；
    /// 而且分析器可以被关闭、也可能没随包安装。asmdef 才是 Unity 里程序集引用的唯一真源。
    /// 分析器的价值在「编辑期红波浪线」，门禁应该在这里。
    /// </remarks>
    public static class LauncherBoundaryValidator
    {
        private const string LauncherPackageRoot = "Packages/cn.efefef.launcher";
        private const string RuntimeAsmdefPath = LauncherPackageRoot + "/Runtime/EF.Launcher.Runtime.asmdef";
        private const string EditorAsmdefPath = LauncherPackageRoot + "/Editor/EF.Launcher.Editor.asmdef";
        private const string HybridClrSettingsPath = "ProjectSettings/HybridCLRSettings.asset";
        private const string MenuPath = MenuItemToolkit.Utility + "🔍 Validate AOT Boundary";

        /// <summary>
        /// 名字像 EF、但已核实「只依赖 BCL + UnityEngine」的插件 DLL，允许 launcher 直接引用。
        /// <para>EasyFrameworkTools 里是 D（日志门面）与 TStatic（字符串校验工具），不引用任何 cn.efefef.*。</para>
        /// </summary>
        private static readonly string[] AllowedPluginDlls = { "EasyFrameworkTools" };

        #region 入口

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            // 延迟一帧：首次导入时 AssetDatabase 可能还没就绪
            EditorApplication.delayCall += () =>
            {
                try
                {
                    Validate(logWhenClean: false);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(LC.Tr(
                        $"[EF.Launcher] AOT 边界校验自身异常（不影响编译）：{e.Message}",
                        $"[EF.Launcher] Boundary check threw (compilation is unaffected): {e.Message}"));
                }
            };
        }

        [MenuItem(MenuPath, false, MenuItemToolkit.UtilityPriority + 50)]
        private static void ValidateFromMenu()
        {
            Validate(logWhenClean: true);
        }

        /// <summary>
        /// 命令行入口：<c>Unity -batchmode -quit -projectPath &lt;proj&gt; -executeMethod EasyFramework.Launcher.Editor.LauncherBoundaryValidator.ValidateFromCommandLine</c>
        /// <para>CI entry point. Exits with code 1 when the boundary is broken.</para>
        /// </summary>
        public static void ValidateFromCommandLine()
        {
            bool ok = Validate(logWhenClean: true);
            if (!ok)
                EditorApplication.Exit(1);
        }

        #endregion

        #region 校验主体

        internal static bool Validate(bool logWhenClean)
        {
            var violations = new List<string>();
            var index = AsmdefDatabase.Build();

            if (!index.TryGetByPath(RuntimeAsmdefPath, out var runtime))
            {
                Debug.LogWarning(LC.Tr(
                    $"[EF.Launcher] 找不到 {RuntimeAsmdefPath}，跳过 AOT 边界校验",
                    $"[EF.Launcher] {RuntimeAsmdefPath} not found, skipping the AOT boundary check."));
                return true;
            }

            CheckDirectReferences(runtime, index, violations);
            CheckTransitiveReferences(runtime, index, violations);
            CheckNotInHotUpdateList(violations);

            if (violations.Count == 0)
            {
                if (logWhenClean)
                {
                    Debug.Log(LC.Tr(
                        "[EF.Launcher] AOT 边界校验通过：EF.Launcher.Runtime 没有任何 cn.efefef.* 引用",
                        "[EF.Launcher] AOT boundary OK: EF.Launcher.Runtime references no cn.efefef.* assembly."));
                }

                return true;
            }

            var sb = new StringBuilder();
            sb.AppendLine(LC.Tr(
                "[EF.Launcher] AOT 边界被破坏 —— 这会让「全量热更」形态在打包时直接失败：",
                "[EF.Launcher] AOT boundary broken - the full hot-update setup will fail at build time:"));
            for (int i = 0; i < violations.Count; i++)
                sb.AppendLine($"  {i + 1}. {violations[i]}");
            sb.AppendLine(LC.Tr(
                "规则：EF.Launcher.Runtime 只能引用 YooAsset / HybridCLR.Runtime，且自身不能进热更列表。",
                "Rule: EF.Launcher.Runtime may only reference YooAsset / HybridCLR.Runtime, and must never be in the hot update list."));

            Debug.LogError(sb.ToString());
            return false;
        }

        private static void CheckDirectReferences(AsmdefInfo runtime, AsmdefDatabase index, List<string> violations)
        {
            foreach (string reference in runtime.References)
            {
                if (string.IsNullOrEmpty(reference))
                    continue;

                var target = index.Resolve(reference);

                if (target != null)
                {
                    if (IsEfCoreAssembly(target))
                    {
                        violations.Add(LC.Tr(
                            $"直接引用 EF 程序集：{target.Name}（{target.Path}）",
                            $"Direct reference to an EF assembly: {target.Name} ({target.Path})"));
                    }

                    continue;
                }

                // 解析不到 asmdef 的（比如引用了不存在的程序集），按名字兜底
                if (LooksLikeEfAssemblyName(reference))
                {
                    violations.Add(LC.Tr(
                        $"直接引用 EF 程序集：{reference}（未找到对应 asmdef，按名字判定）",
                        $"Direct reference to an EF assembly: {reference} (asmdef not found, judged by name)"));
                }
            }

            foreach (string precompiled in runtime.PrecompiledReferences)
            {
                if (!string.IsNullOrEmpty(precompiled) && LooksLikeEfAssemblyName(precompiled))
                {
                    violations.Add(LC.Tr(
                        $"precompiledReferences 里出现 EF 程序集：{precompiled}",
                        $"EF assembly found in precompiledReferences: {precompiled}"));
                }
            }
        }

        private static void CheckTransitiveReferences(AsmdefInfo runtime, AsmdefDatabase index, List<string> violations)
        {
            // BFS：Launcher → A → EF.Core 这种间接依赖，Unity 会把 EF.Core 一起塞进编译引用，同样会炸
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { runtime.Path };
            var queue = new Queue<(AsmdefInfo node, string chain)>();
            queue.Enqueue((runtime, runtime.Name));

            while (queue.Count > 0)
            {
                var (node, chain) = queue.Dequeue();

                foreach (string reference in node.References)
                {
                    var target = index.Resolve(reference);
                    if (target == null || !visited.Add(target.Path))
                        continue;

                    if (IsEfCoreAssembly(target))
                    {
                        violations.Add(LC.Tr(
                            $"传递引用 EF 程序集：{chain} → {target.Name}（{target.Path}）",
                            $"Transitive reference to an EF assembly: {chain} -> {target.Name} ({target.Path})"));
                        continue;
                    }

                    queue.Enqueue((target, chain + " -> " + target.Name));
                }
            }
        }

        private static void CheckNotInHotUpdateList(List<string> violations)
        {
            if (!File.Exists(HybridClrSettingsPath))
                return;

            // ProjectSettings/ 下的资产不在 AssetDatabase 里，直接读 YAML 最稳
            string yaml = File.ReadAllText(HybridClrSettingsPath);

            foreach (string asmdefPath in new[] { RuntimeAsmdefPath, EditorAsmdefPath })
            {
                string guid = AssetDatabase.AssetPathToGUID(asmdefPath);
                if (!string.IsNullOrEmpty(guid) && yaml.Contains(guid))
                {
                    violations.Add(LC.Tr(
                        $"Launcher 程序集被写进了热更列表（会自举失败）：{asmdefPath}",
                        $"Launcher assembly is listed as a hot update assembly (self-bootstrap will fail): {asmdefPath}"));
                }
            }

            foreach (string rawLine in yaml.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.StartsWith("- EF.Launcher", StringComparison.Ordinal))
                {
                    violations.Add(LC.Tr(
                        $"热更列表里出现 Launcher 程序集名（会自举失败）：{line}",
                        $"Launcher assembly name found in the hot update list (self-bootstrap will fail): {line}"));
                }
            }
        }

        #endregion

        #region 判定规则

        private static bool IsEfCoreAssembly(AsmdefInfo info)
        {
            if (info.Path.StartsWith(LauncherPackageRoot, StringComparison.OrdinalIgnoreCase))
                return false;

            return info.Path.StartsWith("Packages/cn.efefef.", StringComparison.OrdinalIgnoreCase)
                   || LooksLikeEfAssemblyName(info.Name);
        }

        private static bool LooksLikeEfAssemblyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            string normalized = name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - 4)
                : name;

            // 白名单：名字像 EF，但已核实不依赖任何 cn.efefef.* 的插件 DLL
            for (int i = 0; i < AllowedPluginDlls.Length; i++)
            {
                if (string.Equals(normalized, AllowedPluginDlls[i], StringComparison.Ordinal))
                    return false;
            }

            if (normalized.StartsWith("EF.Launcher", StringComparison.Ordinal))
                return false;

            return normalized.StartsWith("EF.", StringComparison.Ordinal)
                   || normalized.StartsWith("EasyFramework", StringComparison.Ordinal)
                   || normalized.StartsWith("cn.efefef.", StringComparison.Ordinal);
        }

        #endregion
    }

    /// <summary>
    /// 打包前硬门禁：边界破了就别出包
    /// <para>Build-time gate: abort the build when the AOT boundary is broken.</para>
    /// </summary>
    internal sealed class LauncherBoundaryBuildGate : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!LauncherBoundaryValidator.Validate(logWhenClean: false))
            {
                throw new BuildFailedException(LC.Tr(
                    "[EF.Launcher] AOT 边界被破坏，已中止打包。详情见上一条 Error 日志" +
                    "（菜单 EFTools/🎈 Utility/🔍 Validate AOT Boundary 可复现）。",
                    "[EF.Launcher] AOT boundary broken, build aborted. See the error log above " +
                    "(reproduce via EFTools/🎈 Utility/🔍 Validate AOT Boundary)."));
            }
        }
    }
}
