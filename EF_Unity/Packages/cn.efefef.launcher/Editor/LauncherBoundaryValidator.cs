/*
 * ================================================
 * Describe:        This script is used to validate the AOT boundary of EF.Launcher.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 16:38:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 16:38:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        private const string MenuPath = "EF/Launcher/校验 AOT 边界";

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
                    Debug.LogWarning($"[EF.Launcher] AOT 边界校验自身异常（不影响编译）：{e.Message}");
                }
            };
        }

        [MenuItem(MenuPath)]
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
            var index = AsmdefIndex.Build();

            if (!index.ByPath.TryGetValue(RuntimeAsmdefPath, out var runtime))
            {
                Debug.LogWarning($"[EF.Launcher] 找不到 {RuntimeAsmdefPath}，跳过 AOT 边界校验");
                return true;
            }

            CheckDirectReferences(runtime, index, violations);
            CheckTransitiveReferences(runtime, index, violations);
            CheckNotInHotUpdateList(violations);

            if (violations.Count == 0)
            {
                if (logWhenClean)
                    Debug.Log("[EF.Launcher] AOT 边界校验通过：EF.Launcher.Runtime 没有任何 cn.efefef.* 引用");
                return true;
            }

            var sb = new StringBuilder();
            sb.AppendLine("[EF.Launcher] AOT 边界被破坏 —— 这会让「全量热更」形态在打包时直接失败：");
            for (int i = 0; i < violations.Count; i++)
                sb.AppendLine($"  {i + 1}. {violations[i]}");
            sb.AppendLine("规则：EF.Launcher.Runtime 只能引用 YooAsset / HybridCLR.Runtime，且自身不能进热更列表。");

            Debug.LogError(sb.ToString());
            return false;
        }

        private static void CheckDirectReferences(AsmdefInfo runtime, AsmdefIndex index, List<string> violations)
        {
            foreach (string reference in runtime.References)
            {
                if (string.IsNullOrEmpty(reference))
                    continue;

                var target = index.Resolve(reference);

                if (target != null)
                {
                    if (IsEfCoreAssembly(target))
                        violations.Add($"直接引用 EF 程序集：{target.Name}（{target.Path}）");
                    continue;
                }

                // 解析不到 asmdef 的（比如引用了不存在的程序集），按名字兜底
                if (LooksLikeEfAssemblyName(reference))
                    violations.Add($"直接引用 EF 程序集：{reference}（未找到对应 asmdef，按名字判定）");
            }

            foreach (string precompiled in runtime.PrecompiledReferences)
            {
                if (!string.IsNullOrEmpty(precompiled) && LooksLikeEfAssemblyName(precompiled))
                    violations.Add($"precompiledReferences 里出现 EF 程序集：{precompiled}");
            }
        }

        private static void CheckTransitiveReferences(AsmdefInfo runtime, AsmdefIndex index, List<string> violations)
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
                        violations.Add($"传递引用 EF 程序集：{chain} → {target.Name}（{target.Path}）");
                        continue;
                    }

                    queue.Enqueue((target, chain + " → " + target.Name));
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
                    violations.Add($"Launcher 程序集被写进了热更列表（会自举失败）：{asmdefPath}");
            }

            foreach (string rawLine in yaml.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.StartsWith("- EF.Launcher", StringComparison.Ordinal))
                    violations.Add($"热更列表里出现 Launcher 程序集名（会自举失败）：{line}");
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

            if (name.StartsWith("EF.Launcher", StringComparison.Ordinal))
                return false;

            return name.StartsWith("EF.", StringComparison.Ordinal)
                   || name.StartsWith("EasyFramework", StringComparison.Ordinal)
                   || name.StartsWith("cn.efefef.", StringComparison.Ordinal);
        }

        #endregion

        #region 数据

        [Serializable]
        private sealed class AsmdefData
        {
            public string name = null;
            public string[] references = null;
            public string[] precompiledReferences = null;
        }

        private sealed class AsmdefInfo
        {
            public string Path;
            public string Name;
            public string[] References = Array.Empty<string>();
            public string[] PrecompiledReferences = Array.Empty<string>();

            public static AsmdefInfo Load(string path)
            {
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase))
                    return null;

                // 直接读文件，不依赖 AssemblyDefinitionAsset（不同 Unity 版本所在程序集不一样）
                string fullPath = System.IO.Path.GetFullPath(path);
                if (!File.Exists(fullPath))
                    return null;

                var data = JsonUtility.FromJson<AsmdefData>(File.ReadAllText(fullPath));
                if (data == null)
                    return null;

                return new AsmdefInfo
                {
                    Path = path,
                    Name = string.IsNullOrEmpty(data.name) ? System.IO.Path.GetFileNameWithoutExtension(path) : data.name,
                    References = data.references ?? Array.Empty<string>(),
                    PrecompiledReferences = data.precompiledReferences ?? Array.Empty<string>(),
                };
            }
        }

        /// <summary>
        /// 全工程 asmdef 索引，支持按路径（GUID 引用）和按名字（name 引用）两种解析
        /// </summary>
        private sealed class AsmdefIndex
        {
            public readonly Dictionary<string, AsmdefInfo> ByPath =
                new Dictionary<string, AsmdefInfo>(StringComparer.OrdinalIgnoreCase);

            private readonly Dictionary<string, AsmdefInfo> _byName =
                new Dictionary<string, AsmdefInfo>(StringComparer.Ordinal);

            public static AsmdefIndex Build()
            {
                var index = new AsmdefIndex();

                foreach (string guid in AssetDatabase.FindAssets("t:AssemblyDefinitionAsset"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var info = AsmdefInfo.Load(path);
                    if (info == null)
                        continue;

                    index.ByPath[path] = info;
                    index._byName[info.Name] = info;
                }

                return index;
            }

            public AsmdefInfo Resolve(string reference)
            {
                if (string.IsNullOrEmpty(reference))
                    return null;

                if (reference.StartsWith("GUID:", StringComparison.OrdinalIgnoreCase))
                {
                    string path = AssetDatabase.GUIDToAssetPath(reference.Substring(5));
                    if (string.IsNullOrEmpty(path))
                        return null;

                    if (ByPath.TryGetValue(path, out var byPath))
                        return byPath;

                    return AsmdefInfo.Load(path);
                }

                return _byName.TryGetValue(reference, out var byName) ? byName : null;
            }
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
                throw new BuildFailedException(
                    "[EF.Launcher] AOT 边界被破坏，已中止打包。详情见上一条 Error 日志" +
                    "（菜单 EF/Launcher/校验 AOT 边界 可复现）。");
            }
        }
    }
}
