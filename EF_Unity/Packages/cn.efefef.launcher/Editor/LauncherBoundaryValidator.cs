/*
 * ================================================
 * Describe:        This script is used to validate the AOT boundary of EF.Launcher.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 16:38:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-16 11:35:00
 * ScriptVersion:   0.4
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
    /// AOT 边界校验：保证 <c>EF.Launcher</c> 永远不会（直接或传递）引用任何热更程序集。
    /// <para>Boundary guard: the launcher runtime must never reference any hot update assembly, directly or transitively.</para>
    /// <para>唯一例外：<c>EF.Core</c> 常驻 AOT，作为框架底座由精确白名单放行。</para>
    /// <para>Exception: EF.Core stays in AOT and is allowed via an exact allowlist.</para>
    /// </summary>
    /// <remarks>
    /// 判定真源是 <c>HybridCLRSettings.asset</c> 的热更列表（会被 il2cpp 从包体剔除的程序集），
    /// 不是 <c>Packages/cn.efefef.*</c> 路径：EF 包把 UniTask / YooAsset 等第三方 vendor 在自己的
    /// Plugins 目录下，按路径一刀切会把这些常驻 AOT 的程序集误判成热更代码。
    /// <para>The authoritative source is the HybridCLR hot update list, not package paths: EF packages vendor
    /// third-party assemblies (UniTask / YooAsset) under their own Plugins folders.</para>
    /// 为什么不用 Roslyn 分析器当主闸门：分析器只能看到 <c>Compilation.References</c>，这和 asmdef 是同一份信息，
    /// 没有额外准确度；而且分析器可以被关闭、也可能没随包安装。asmdef 才是 Unity 里程序集引用的唯一真源。
    /// </remarks>
    public static class LauncherBoundaryValidator
    {
        private const string LauncherPackageRoot = "Packages/cn.efefef.launcher";
        private const string RuntimeAsmdefPath = LauncherPackageRoot + "/Runtime/EF.Launcher.asmdef";
        private const string EditorAsmdefPath = LauncherPackageRoot + "/Editor/EF.Launcher.Editor.asmdef";
        private const string HybridClrSettingsPath = "ProjectSettings/HybridCLRSettings.asset";
        private const string MenuPath = MenuItemToolkit.Launcher + "🔍 Validate AOT Boundary";

        // 唯一允许 launcher 直接引用的 EF 自有程序集：EF.Core 常驻 AOT，是框架底座
        private const string AllowedAotBaseName = "EF.Core";
        private const string AllowedAotBasePath = "Packages/cn.efefef.core/Runtime/EF.Core.asmdef";
        private const string AllowedAotBaseGuid = "396fd9bc7a7442941a71f32479ef054d";
        private const string RuntimeSourceDir = LauncherPackageRoot + "/Runtime";
        private const string CoreBridgeFileName = "LauncherCoreBridge.cs";

        private const string DefinitionsSection = "hotUpdateAssemblyDefinitions";
        private const string AssembliesSection = "hotUpdateAssemblies";
        private const string PreserveSection = "preserveHotUpdateAssemblies";
        private const string GuidMarker = "guid:";
        private const int GuidLength = 32;

        /// <summary>
        /// 名字像 EF、但已核实「只依赖 BCL + UnityEngine」的插件 DLL，允许 launcher 直接引用。
        /// <para>EasyFrameworkTools 里是 D（日志门面）与 TStatic（字符串校验工具），不引用任何 cn.efefef.*。</para>
        /// </summary>
        private static readonly string[] AllowedPluginDlls = { "EasyFrameworkTools" };

        #region 热更列表索引

        /// <summary>
        /// HybridCLR 热更列表快照，AOT 边界判定的唯一真源。
        /// <para>Snapshot of the HybridCLR hot update list, the authoritative source for AOT boundary checks.</para>
        /// </summary>
        private sealed class HotUpdateIndex
        {
            public readonly HashSet<string> Guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> Paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 段里出现了列表项却解析不出 GUID：格式可能变了，门禁不能静默失效
            public bool HasUnparsedDefinitionItems;

            /// <summary>
            /// 目标程序集是否属于热更列表（按路径 / 名字 / GUID 三重匹配）。
            /// <para>Whether the given assembly belongs to the hot update list.</para>
            /// </summary>
            public bool Contains(AsmdefInfo info)
            {
                if (Paths.Contains(info.Path) || Names.Contains(info.Name))
                    return true;

                string guid = AssetDatabase.AssetPathToGUID(info.Path);
                return !string.IsNullOrEmpty(guid) && Guids.Contains(guid);
            }

            /// <summary>
            /// 按 YAML 段解析 <c>HybridCLRSettings.asset</c>，只采集热更相关三段。
            /// <para>Parses the three hot update sections of HybridCLRSettings.asset as YAML lines.</para>
            /// </summary>
            public static HotUpdateIndex Build()
            {
                var index = new HotUpdateIndex();
                if (!File.Exists(HybridClrSettingsPath))
                    return index;

                string section = string.Empty;
                foreach (string rawLine in File.ReadAllLines(HybridClrSettingsPath))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)
                        || line.StartsWith("%", StringComparison.Ordinal)
                        || line.StartsWith("---", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (line.StartsWith("- ", StringComparison.Ordinal) || string.Equals(line, "-", StringComparison.Ordinal))
                    {
                        string item = line.Substring(1).Trim();
                        if (item.Length == 0)
                            continue;

                        if (string.Equals(section, DefinitionsSection, StringComparison.Ordinal))
                        {
                            string guid = ExtractGuid(item);
                            if (guid == null)
                            {
                                index.HasUnparsedDefinitionItems = true;
                                continue;
                            }

                            index.Guids.Add(guid);
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            if (!string.IsNullOrEmpty(path))
                                index.Paths.Add(path);
                        }
                        else if (string.Equals(section, AssembliesSection, StringComparison.Ordinal)
                                 || string.Equals(section, PreserveSection, StringComparison.Ordinal))
                        {
                            index.Names.Add(NormalizeAssemblyName(item));
                        }

                        continue;
                    }

                    // 字段行：切换当前段。m_Script 那一行带 guid 但不是列表项，不会被误采集
                    int colon = line.IndexOf(':');
                    if (colon > 0)
                        section = line.Substring(0, colon).Trim();
                }

                return index;
            }

            // 从 "{fileID: ..., guid: xxxx, type: 3}" 里取 32 位十六进制 GUID
            private static string ExtractGuid(string item)
            {
                int at = item.IndexOf(GuidMarker, StringComparison.OrdinalIgnoreCase);
                if (at < 0)
                    return null;

                int start = at + GuidMarker.Length;
                while (start < item.Length && item[start] == ' ')
                    start++;

                if (start + GuidLength > item.Length)
                    return null;

                for (int i = 0; i < GuidLength; i++)
                {
                    char c = item[start + i];
                    bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                    if (!hex)
                        return null;
                }

                return item.Substring(start, GuidLength);
            }
        }

        #endregion

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
                    Debug.LogWarning(LC.Format(Lc.Cs_LauncherBoundaryException, e.Message));
                }
            };
        }

        [MenuItem(MenuPath, false, MenuItemToolkit.LauncherPriority + 50)]
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
            var hot = HotUpdateIndex.Build();

            if (hot.HasUnparsedDefinitionItems)
            {
                Debug.LogWarning(LC.Tr(
                    "HybridCLRSettings.hotUpdateAssemblyDefinitions 里有无法解析的列表项，边界门禁可能失效，请检查 YAML 格式。",
                    "Unparsable items in HybridCLRSettings.hotUpdateAssemblyDefinitions; the boundary guard may be ineffective, please check the YAML."));
            }

            if (!index.TryGetByPath(RuntimeAsmdefPath, out var runtime))
            {
                Debug.LogWarning(LC.Format(Lc.Cs_LauncherSkipBoundaryCheck, RuntimeAsmdefPath));
                return true;
            }

            CheckDirectReferences(runtime, index, hot, violations);
            CheckTransitiveReferences(runtime, index, hot, violations);
            CheckNotInHotUpdateList(hot, violations);
            CheckAllowedBaseIsAot(hot, violations);
            CheckRuntimeSourceBoundary(violations);

            if (violations.Count == 0)
            {
                if (logWhenClean)
                {
                    Debug.Log(LC.Combine(Lc.Cs_LauncherBoundaryOk));
                }

                return true;
            }

            var sb = new StringBuilder();
            sb.AppendLine(LC.Combine(Lc.Cs_LauncherBoundaryBroken));
            for (int i = 0; i < violations.Count; i++)
                sb.AppendLine($"  {i + 1}. {violations[i]}");
            sb.AppendLine(LC.Combine(Lc.Cs_LauncherBoundaryRule));

            Debug.LogError(sb.ToString());
            return false;
        }

        private static void CheckDirectReferences(AsmdefInfo runtime, AsmdefDatabase index, HotUpdateIndex hot, List<string> violations)
        {
            foreach (string reference in runtime.References)
            {
                if (string.IsNullOrEmpty(reference))
                    continue;

                var target = index.Resolve(reference);

                if (target != null)
                {
                    if (IsForbiddenReference(target, hot))
                    {
                        violations.Add(LC.Format(Lc.Cs_LauncherDirectRef, target.Name, target.Path));
                    }

                    continue;
                }

                // 解析不到 asmdef 的（引用了不存在的程序集 / 外部包），按名字或 GUID 兜底
                if (IsForbiddenUnresolvedReference(reference, hot))
                {
                    violations.Add(LC.Format(Lc.Cs_LauncherDirectRefByName, reference));
                }
            }

            foreach (string precompiled in runtime.PrecompiledReferences)
            {
                if (IsForbiddenUnresolvedReference(precompiled, hot))
                {
                    violations.Add(LC.Format(Lc.Cs_LauncherPrecompiledRef, precompiled));
                }
            }
        }

        private static void CheckTransitiveReferences(AsmdefInfo runtime, AsmdefDatabase index, HotUpdateIndex hot, List<string> violations)
        {
            // BFS：Launcher → A → 热更程序集 这种间接依赖，Unity 会把闭包一起塞进编译引用，同样要检查
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

                    if (IsForbiddenReference(target, hot))
                    {
                        violations.Add(LC.Format(Lc.Cs_LauncherTransitiveRef, chain, target.Name, target.Path));
                        continue;
                    }

                    queue.Enqueue((target, chain + " -> " + target.Name));
                }
            }
        }

        private static void CheckNotInHotUpdateList(HotUpdateIndex hot, List<string> violations)
        {
            foreach (string asmdefPath in new[] { RuntimeAsmdefPath, EditorAsmdefPath })
            {
                string guid = AssetDatabase.AssetPathToGUID(asmdefPath);
                if (!string.IsNullOrEmpty(guid) && hot.Guids.Contains(guid))
                {
                    violations.Add(LC.Format(Lc.Cs_LauncherSelfBootstrapPath, asmdefPath));
                }
            }

            foreach (string name in hot.Names)
            {
                if (name.StartsWith("EF.Launcher", StringComparison.Ordinal))
                {
                    violations.Add(LC.Format(Lc.Cs_LauncherSelfBootstrapName, "- " + name));
                }
            }
        }

        // 白名单的前提：EF.Core 必须真的常驻 AOT。有人手滑把它加进热更列表时，放行就变成断链
        private static void CheckAllowedBaseIsAot(HotUpdateIndex hot, List<string> violations)
        {
            if (hot.Guids.Contains(AllowedAotBaseGuid)
                || hot.Paths.Contains(AllowedAotBasePath)
                || hot.Names.Contains(AllowedAotBaseName))
            {
                violations.Add(LC.Tr(
                    "白名单程序集 EF.Core 出现在 HybridCLR 热更列表，Launcher 引用它会在 il2cpp 阶段断链。",
                    "Allowed AOT base EF.Core is in the HybridCLR hot update list; the launcher reference will break the IL2CPP build."));
            }
        }

        // 启动段源码静态检查：对 Core 的访问只准收口在桥接文件里，且全段禁止碰框架管理器单例
        private static void CheckRuntimeSourceBoundary(List<string> violations)
        {
            if (!Directory.Exists(RuntimeSourceDir))
                return;

            foreach (string file in Directory.GetFiles(RuntimeSourceDir, "*.cs", SearchOption.TopDirectoryOnly))
            {
                string source = File.ReadAllText(file);
                string name = Path.GetFileName(file);
                bool isBridge = string.Equals(name, CoreBridgeFileName, StringComparison.OrdinalIgnoreCase);

                if (source.IndexOf("EasyFramework.Managers", StringComparison.Ordinal) >= 0
                    || source.IndexOf("Singleton<", StringComparison.Ordinal) >= 0)
                {
                    violations.Add(LC.Tr(
                        "启动段禁止访问框架管理器单例：" + name + "（热更 DLL 尚未加载，Init 会拿到未注入的门面）。",
                        "Boot-stage code must not touch framework manager singletons: " + name + " (hot update DLLs are not loaded yet)."));
                }

                if (!isBridge && source.IndexOf("EFC.", StringComparison.Ordinal) >= 0)
                {
                    violations.Add(LC.Tr(
                        "启动段对 EFC 的访问必须收口到 LauncherCoreBridge.cs：" + name,
                        "All EFC access in boot-stage code must be confined to LauncherCoreBridge.cs: " + name));
                }
            }
        }

        #endregion

        #region 判定规则

        /// <summary>
        /// 一条 asmdef 引用是否违规：热更列表命中，或属于未白名单的 EF 自有程序集。
        /// <para>Whether an asmdef reference is forbidden: listed as hot update, or an EF-owned assembly outside the allowlist.</para>
        /// </summary>
        /// <remarks>
        /// 这里刻意<b>不按包路径判定</b>：EF 包把 UniTask / YooAsset vendor 在 <c>cn.efefef.*/Plugins/</c> 下，
        /// 它们的路径属于 EF 包，程序集却是常驻 AOT 的第三方；<c>EF.Core</c> 作为 AOT 底座必然引用它们。
        /// </remarks>
        private static bool IsForbiddenReference(AsmdefInfo info, HotUpdateIndex hot)
        {
            // Launcher 自身（Runtime / Editor / Sample）永远合法
            if (info.Path.StartsWith(LauncherPackageRoot, StringComparison.OrdinalIgnoreCase))
                return false;

            // 唯一放行的 EF 自有程序集，路径 + 名字双重匹配，避免同名程序集蒙混过关
            if (IsAllowedAotBase(info.Path, info.Name))
                return false;

            // 真约束：引用闭包里出现会被 il2cpp 从包体剔除的程序集
            if (hot.Contains(info))
                return true;

            // 架构约束：其余 EF 自有程序集一律不许 Launcher 直接依赖。
            // 按【程序集名】识别，不再按 Packages/cn.efefef.* 路径。
            return LooksLikeEfAssemblyName(info.Name);
        }

        // 引用解析不到时的保守兜底：只认热更 GUID 与「名字像 EF 自有程序集」
        private static bool IsForbiddenUnresolvedReference(string reference, HotUpdateIndex hot)
        {
            if (string.IsNullOrEmpty(reference))
                return false;

            if (reference.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
                return hot.Guids.Contains(reference.Substring(5));

            return LooksLikeEfAssemblyName(reference);
        }

        // 精确白名单：路径 + 名字双重匹配
        private static bool IsAllowedAotBase(string path, string name)
        {
            return string.Equals(path, AllowedAotBasePath, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(name, AllowedAotBaseName, StringComparison.Ordinal);
        }

        private static string NormalizeAssemblyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string normalized = name.Trim();
            return normalized.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(0, normalized.Length - 4)
                : normalized;
        }

        private static bool LooksLikeEfAssemblyName(string name)
        {
            string normalized = NormalizeAssemblyName(name);
            if (normalized.Length == 0)
                return false;

            // 白名单：名字像 EF，但已核实不依赖任何 cn.efefef.* 的插件 DLL
            for (int i = 0; i < AllowedPluginDlls.Length; i++)
            {
                if (string.Equals(normalized, AllowedPluginDlls[i], StringComparison.Ordinal))
                    return false;
            }

            // 常驻 AOT 的框架底座，按名字引用时同样放行
            if (string.Equals(normalized, AllowedAotBaseName, StringComparison.Ordinal))
                return false;

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
                throw new BuildFailedException(LC.Combine(Lc.Cs_LauncherBuildAborted));
            }
        }
    }
}
