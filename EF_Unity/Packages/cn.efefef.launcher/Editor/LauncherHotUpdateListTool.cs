/*
 * ================================================
 * Describe:        This script is used to toggle EF assemblies in the hot update list.
 * Author:          Alvin8412
 * CreationTime:    2026-11-14 21:25:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-14 21:40:00
 * ScriptVersion:   0.2
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.Text;
using EasyFramework.Edit;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Launcher.Editor
{
    /// <summary>
    /// 一键在「Classic」和「Full」两种形态之间切换热更列表。
    /// <para>One-click toggle between Classic (EF stays AOT) and Full (EF goes hot update).</para>
    /// </summary>
    /// <remarks>
    /// 加入前会检查「还有哪些 AOT 程序集在引用待加入的 EF 程序集」——那些引用会让打包直接失败，
    /// 所以在对话框里先列出来，避免用户点完按钮就炸。
    /// <para>EF.Core 常驻 AOT 且被 Launcher 引用，默认不在候选列表里；由 LauncherBoundaryValidator
    /// 单独校验它没有被误加进热更列表。</para>
    /// </remarks>
    public static class LauncherHotUpdateListTool
    {
        private const string LauncherPackageRoot = "Packages/cn.efefef.launcher";
        private const string EfPackageRoot = "Packages/cn.efefef.";
        private const string DefinitionsProperty = "hotUpdateAssemblyDefinitions";
        private const string MenuRoot = MenuItemToolkit.Launcher;
        private const string DialogTitle = "EF.Launcher";

        #region 菜单

        [MenuItem(MenuRoot + "➕ Add All EF Assemblies To Hot Update", false, MenuItemToolkit.LauncherPriority + 51)]
        public static void AddEfAssembliesToHotUpdateList()
        {
            var database = AsmdefDatabase.Build();
            var candidates = CollectEfRuntimeAsmdefs(database);

            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Combine(Lc.Cs_LauncherNoEfAssembly), LC.Combine(Lc.Cs_OkConfirm));
                return;
            }

            if (!TryGetDefinitionsProperty(out var serialized, out var list))
                return;

            var existing = CollectExistingPaths(list);
            var toAdd = new List<AsmdefInfo>();
            var candidatePaths = new List<string>(candidates.Count);

            foreach (var info in candidates)
            {
                candidatePaths.Add(info.Path);
                if (!existing.Contains(info.Path))
                    toAdd.Add(info);
            }

            var candidateSet = new HashSet<string>(candidatePaths, StringComparer.OrdinalIgnoreCase);
            var scan = ScanConsumers(database, candidateSet, existing);

            if (toAdd.Count == 0 && scan.IsEmpty)
            {
                // 列表已经是全的，但骨架可能还没生成，顺便补一次
                int filledAlready = LauncherHotfixDllFillTool.FillExistingConfig(out int totalAlready);
                EditorUtility.DisplayDialog(DialogTitle, LC.Format(Lc.Cs_LauncherAllAlreadyAdded, DescribeFillResult(filledAlready, totalAlready)),
                    LC.Combine(Lc.Cs_OkConfirm));
                return;
            }

            string message = BuildAddMessage(toAdd.Count, existing.Count, scan);

            if (scan.AotConsumers.Count > 0)
            {
                // 有 AOT 程序集还在引用 EF 时，这一步必然导致打包失败；
                // 把「取消」放在第一个按钮，避免手快回车直接踩坑
                if (EditorUtility.DisplayDialog(DialogTitle, message,
                        LC.Combine(Lc.Cancel), LC.Combine(Lc.Cs_LauncherAddAnyway)))
                {
                    return;
                }
            }
            else if (!EditorUtility.DisplayDialog(DialogTitle, message, LC.Combine(Lc.AddEntry), LC.Combine(Lc.Cancel)))
            {
                return;
            }

            serialized.Update();
            int added = 0;
            var addedPaths = new List<string>(toAdd.Count);
            foreach (var info in toAdd)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(info.Path);
                if (asset == null)
                    continue;

                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).objectReferenceValue = asset;
                added++;
                addedPaths.Add(info.Path);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            HybridCLRSettings.Save();

            Debug.Log(LC.Format(Lc.Cs_LauncherAddedLog, added, string.Join("\n  ", addedPaths)));

            // 顺手把 LauncherConfig.HotfixDlls 的骨架也铺好（按依赖序）
            int filled = LauncherHotfixDllFillTool.FillExistingConfig(out int total);

            EditorUtility.DisplayDialog(DialogTitle, LC.Format(Lc.Cs_LauncherAddedDialog, added, DescribeFillResult(filled, total)),
                LC.Combine(Lc.Cs_Ok));
        }

        /// <summary>把填充结果翻译成一句人话（filled = -1 表示没有可用的 LauncherConfig）</summary>
        private static string DescribeFillResult(int filled, int total)
        {
            if (filled < 0)
            {
                return LC.Combine(Lc.Cs_LauncherFillSkipped);
            }

            return LC.Format(Lc.Cs_LauncherFillSummary, filled, total);
        }

        [MenuItem(MenuRoot + "🗑️ Remove All EF Assemblies From Hot Update", false, MenuItemToolkit.LauncherPriority + 52)]
        public static void RemoveEfAssembliesFromHotUpdateList()
        {
            if (!TryGetDefinitionsProperty(out var serialized, out var list))
                return;

            var toRemove = new List<string>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var asset = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (asset == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(path))
                    continue;

                // 含 launcher 自己（可能是误加进去的，一并清掉）
                if (path.StartsWith(EfPackageRoot, StringComparison.OrdinalIgnoreCase))
                    toRemove.Add(path);
            }

            if (toRemove.Count == 0)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Combine(Lc.Cs_LauncherAlreadyClassic), LC.Combine(Lc.Cs_OkConfirm));
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(LC.Format(Lc.Cs_LauncherAboutToRemove, toRemove.Count));
            foreach (string path in toRemove)
                sb.AppendLine("  - " + System.IO.Path.GetFileNameWithoutExtension(path));
            sb.AppendLine();
            sb.AppendLine(LC.Combine(Lc.Cs_LauncherRemoveWarning));

            if (!EditorUtility.DisplayDialog(DialogTitle, sb.ToString(), LC.Combine(Lc.RemoveEntry), LC.Combine(Lc.Cancel)))
                return;

            serialized.Update();
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                var asset = list.GetArrayElementAtIndex(i).objectReferenceValue;

                // 顺手清掉空条目
                if (asset == null)
                {
                    list.DeleteArrayElementAtIndex(i);
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(path) ||
                    !path.StartsWith(EfPackageRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 对象引用数组：第一次 Delete 只是置空，要再删一次才会真的移除
                list.DeleteArrayElementAtIndex(i);
                if (i < list.arraySize && list.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    list.DeleteArrayElementAtIndex(i);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            HybridCLRSettings.Save();

            Debug.Log(LC.Format(Lc.Cs_LauncherRemovedCount, toRemove.Count));

            EditorUtility.DisplayDialog(DialogTitle, LC.Format(Lc.Cs_LauncherRemovedNext, toRemove.Count),
                LC.Combine(Lc.Cs_Ok));
        }

        #endregion

        #region 收集与判定

        /// <summary>
        /// 收集所有 EF 运行时 asmdef（排除 launcher 自己、编辑器程序集、Plugins 下的第三方）。
        /// <para>当前形态 Core 留 AOT，默认排除 EF.Core；只有回到「含 Core 的全量热更」实验形态才传 includeCore: true。</para>
        /// </summary>
        internal static List<AsmdefInfo> CollectEfRuntimeAsmdefs(AsmdefDatabase database, bool includeCore = false)
        {
            var result = new List<AsmdefInfo>();

            foreach (var info in database.All)
            {
                if (!info.Path.StartsWith(EfPackageRoot, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Core 是 AOT 底座：热更程序集单向引用它，它自己绝不能进热更列表。
                // 同一程序集既在包体又被 Assembly.Load，会分裂出第二份类型身份（InvalidCastException）。
                if (!includeCore && string.Equals(info.Name, "EF.Core", StringComparison.OrdinalIgnoreCase))
                    continue;

                // launcher 是加载器，永远不能进热更列表
                if (info.Path.StartsWith(LauncherPackageRoot, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (info.IsEditorOnly)
                    continue;

                // Plugins 下是第三方（UniTask）与预编译 DLL，保持 AOT：重泛型代码热更后性能会明显下降
                if (info.Path.IndexOf("/Plugins/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                // 兜底：有些 Editor 程序集没锁平台（例如 EF.UI.Editor），靠名字和路径再挡一层
                if (info.Name.EndsWith(".Editor", StringComparison.Ordinal))
                    continue;

                if (info.Path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                result.Add(info);
            }

            result.Sort(CompareForDisplay);
            return result;
        }

        /// <summary>EF.Core 排最前，其余按名字</summary>
        private static int CompareForDisplay(AsmdefInfo left, AsmdefInfo right)
        {
            string a = left.Name;
            string b = right.Name;

            bool aCore = string.Equals(a, "EF.Core", StringComparison.Ordinal);
            bool bCore = string.Equals(b, "EF.Core", StringComparison.Ordinal);
            if (aCore != bCore)
                return aCore ? -1 : 1;

            return string.Compare(a, b, StringComparison.Ordinal);
        }

        /// <summary>
        /// 扫两类程序集：
        /// 1) 真正会炸的 AOT 程序集（运行时程序集，引用 EF 且不在热更列表）；
        /// 2) 配置有误的编辑器程序集（名字/路径是 Editor，但 asmdef 没锁平台，会被编进包体）。
        /// 第 2 类与热更无关，单独提示，不混进「打包会失败」的名单。
        /// </summary>
        private static ConsumerScan ScanConsumers(
            AsmdefDatabase database,
            HashSet<string> efPaths,
            HashSet<string> hotUpdatePaths)
        {
            var scan = new ConsumerScan();

            foreach (var info in database.All)
            {
                if (efPaths.Contains(info.Path) || hotUpdatePaths.Contains(info.Path))
                    continue;

                if (info.IsEditorOnly)
                    continue;

                if (info.Path.StartsWith(LauncherPackageRoot, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!ReferencesAnyEf(info, database, efPaths))
                    continue;

                if (LooksLikeEditorAssembly(info))
                    scan.MisconfiguredEditors.Add(info.Name);
                else
                    scan.AotConsumers.Add(info.Name);
            }

            scan.AotConsumers.Sort(StringComparer.Ordinal);
            scan.MisconfiguredEditors.Sort(StringComparer.Ordinal);
            return scan;
        }

        private static bool ReferencesAnyEf(AsmdefInfo info, AsmdefDatabase database, HashSet<string> efPaths)
        {
            foreach (string reference in info.References)
            {
                var target = database.Resolve(reference);
                if (target != null && efPaths.Contains(target.Path))
                    return true;
            }

            return false;
        }

        /// <summary>名字或路径看起来是编辑器程序集</summary>
        private static bool LooksLikeEditorAssembly(AsmdefInfo info)
        {
            return info.Name.EndsWith(".Editor", StringComparison.Ordinal)
                   || info.Path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static HashSet<string> CollectExistingPaths(SerializedProperty list)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < list.arraySize; i++)
            {
                var asset = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (asset == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(asset);
                if (!string.IsNullOrEmpty(path))
                    set.Add(path);
            }

            return set;
        }

        private static string BuildAddMessage(int addCount, int existingCount, ConsumerScan scan)
        {
            var sb = new StringBuilder();
            sb.AppendLine(LC.Format(Lc.Cs_LauncherAboutToAdd, addCount, existingCount, existingCount + addCount));

            if (scan.AotConsumers.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(LC.Combine(Lc.Cs_LauncherAotConsumerWarning));
                foreach (string name in scan.AotConsumers)
                    sb.AppendLine("   - " + name);
                sb.AppendLine();
                sb.AppendLine(LC.Combine(Lc.Cs_LauncherRemoveReferences));
            }

            if (scan.MisconfiguredEditors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(LC.Combine(Lc.Cs_LauncherEditorAssemblyHint));
                foreach (string name in scan.MisconfiguredEditors)
                    sb.AppendLine("   - " + name);
                sb.AppendLine();
                sb.AppendLine(LC.Combine(Lc.Cs_LauncherEditorAssemblyNote));
            }

            sb.AppendLine();
            sb.AppendLine(LC.Combine(Lc.Cs_LauncherAfterAdd));
            sb.AppendLine();
            sb.Append(LC.Combine(Lc.Proceed));

            return sb.ToString();
        }

        /// <summary>扫描结果：真正会炸的 AOT 程序集 + 配置有误的编辑器程序集</summary>
        private sealed class ConsumerScan
        {
            public readonly List<string> AotConsumers = new List<string>();
            public readonly List<string> MisconfiguredEditors = new List<string>();

            public bool IsEmpty => AotConsumers.Count == 0 && MisconfiguredEditors.Count == 0;
        }

        private static bool TryGetDefinitionsProperty(out SerializedObject serialized, out SerializedProperty list)
        {
            serialized = null;
            list = null;

            var settings = HybridCLRSettings.Instance;
            if (settings == null)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Combine(Lc.Cs_LauncherNoHybridClrSettings), LC.Combine(Lc.Cs_OkConfirm));
                return false;
            }

            serialized = new SerializedObject(settings);
            list = serialized.FindProperty(DefinitionsProperty);
            if (list == null)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Format(Lc.Cs_LauncherMissingField, DefinitionsProperty),
                    LC.Combine(Lc.Cs_OkConfirm));
                return false;
            }

            return true;
        }

        #endregion
    }
}
