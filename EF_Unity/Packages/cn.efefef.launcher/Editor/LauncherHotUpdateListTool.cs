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
    /// 加入前会检查「还有哪些 AOT 程序集在引用 EF」——那些程序集会让打包直接失败，
    /// 所以在对话框里先列出来，避免用户点完按钮就炸。
    /// </remarks>
    public static class LauncherHotUpdateListTool
    {
        private const string LauncherPackageRoot = "Packages/cn.efefef.launcher";
        private const string EfPackageRoot = "Packages/cn.efefef.";
        private const string DefinitionsProperty = "hotUpdateAssemblyDefinitions";
        private const string MenuRoot = MenuItemToolkit.Utility;
        private const string DialogTitle = "EF.Launcher";

        #region 菜单

        [MenuItem(MenuRoot + "➕ Add All EF Assemblies To Hot Update", false, MenuItemToolkit.UtilityPriority + 51)]
        public static void AddEfAssembliesToHotUpdateList()
        {
            var database = AsmdefDatabase.Build();
            var candidates = CollectEfRuntimeAsmdefs(database);

            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                    "没有在工程里找到 cn.efefef.* 的运行时程序集。",
                    "No cn.efefef.* runtime assembly was found in this project."), LC.Tr("确定", "OK"));
                return;
            }

            if (!TryGetDefinitionsProperty(out var serialized, out var list))
                return;

            var existing = CollectExistingPaths(list);
            var toAdd = new List<string>();
            foreach (string path in candidates)
            {
                if (!existing.Contains(path))
                    toAdd.Add(path);
            }

            var candidateSet = new HashSet<string>(candidates, StringComparer.OrdinalIgnoreCase);
            var scan = ScanConsumers(database, candidateSet, existing);

            if (toAdd.Count == 0 && scan.IsEmpty)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                    "EF 的运行时程序集已经全部在热更列表里了。",
                    "All EF runtime assemblies are already in the hot update list."), LC.Tr("确定", "OK"));
                return;
            }

            string message = BuildAddMessage(toAdd.Count, existing.Count, scan);
            if (!EditorUtility.DisplayDialog(DialogTitle, message, LC.Tr("加入", "Add"), LC.Tr("取消", "Cancel")))
                return;

            serialized.Update();
            int added = 0;
            foreach (string path in toAdd)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset == null)
                    continue;

                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).objectReferenceValue = asset;
                added++;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            HybridCLRSettings.Save();

            Debug.Log(LC.Tr(
                $"[EF.Launcher] 已加入 {added} 个 EF 程序集到热更列表：\n  {string.Join("\n  ", toAdd)}",
                $"[EF.Launcher] Added {added} EF assemblies to the hot update list:\n  {string.Join("\n  ", toAdd)}"));

            EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                    $"已加入 {added} 个程序集。\n\n下一步：\n" +
                    "1. 执行 HybridCLR/Generate/All\n" +
                    "2. 把 HybridCLRData/HotUpdateDlls 里的 dll 按拓扑序填进 LauncherConfig.HotfixDlls",
                    $"Added {added} assemblies.\n\nNext:\n" +
                    "1. Run HybridCLR/Generate/All\n" +
                    "2. Fill LauncherConfig.HotfixDlls with the dlls from HybridCLRData/HotUpdateDlls, in topological order"),
                LC.Tr("好", "OK"));
        }

        [MenuItem(MenuRoot + "🗑️ Remove All EF Assemblies From Hot Update", false, MenuItemToolkit.UtilityPriority + 52)]
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
                EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                    "热更列表里没有 EF 相关程序集，当前就是 Classic 形态。",
                    "No EF assembly is in the hot update list; the project is already in Classic mode."), LC.Tr("确定", "OK"));
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine(LC.Tr(
                $"将从热更列表移除 {toRemove.Count} 个程序集，移除后 EF 回到 AOT（Classic 形态）：",
                $"About to remove {toRemove.Count} assemblies; EF returns to AOT (Classic mode):"));
            foreach (string path in toRemove)
                sb.AppendLine("  - " + System.IO.Path.GetFileNameWithoutExtension(path));
            sb.AppendLine();
            sb.AppendLine(LC.Tr(
                "注意：热更程序集被移除后，热更层将不能再覆盖这些类型。确认吗？",
                "Note: once removed, the hot update layer can no longer override these types. Continue?"));

            if (!EditorUtility.DisplayDialog(DialogTitle, sb.ToString(), LC.Tr("移除", "Remove"), LC.Tr("取消", "Cancel")))
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

            Debug.Log(LC.Tr(
                $"[EF.Launcher] 已从热更列表移除 {toRemove.Count} 个程序集。",
                $"[EF.Launcher] Removed {toRemove.Count} assemblies from the hot update list."));

            EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                $"已移除 {toRemove.Count} 个程序集。\n\n下一步：执行 HybridCLR/Generate/All 让产物同步。",
                $"Removed {toRemove.Count} assemblies.\n\nNext: run HybridCLR/Generate/All to refresh the generated output."),
                LC.Tr("好", "OK"));
        }

        #endregion

        #region 收集与判定

        /// <summary>
        /// 收集所有 EF 运行时 asmdef（排除 launcher 自己、编辑器程序集、Plugins 下的第三方）
        /// </summary>
        private static List<string> CollectEfRuntimeAsmdefs(AsmdefDatabase database)
        {
            var result = new List<string>();

            foreach (var info in database.All)
            {
                if (!info.Path.StartsWith(EfPackageRoot, StringComparison.OrdinalIgnoreCase))
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

                result.Add(info.Path);
            }

            result.Sort(CompareForDisplay);
            return result;
        }

        /// <summary>EF.Core 排最前，其余按名字</summary>
        private static int CompareForDisplay(string left, string right)
        {
            string a = System.IO.Path.GetFileNameWithoutExtension(left);
            string b = System.IO.Path.GetFileNameWithoutExtension(right);

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
            sb.AppendLine(LC.Tr(
                $"将新增 {addCount} 个 EF 程序集，列表原有 {existingCount} 个，加入后共 {existingCount + addCount} 个。",
                $"About to add {addCount} EF assemblies. The list already has {existingCount}, total will be {existingCount + addCount}."));

            if (scan.AotConsumers.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(LC.Tr(
                    "⚠ 以下 AOT 程序集仍在引用 EF，加入热更后打包会失败：",
                    "⚠ These AOT assemblies still reference EF; the build will fail after adding them:"));
                foreach (string name in scan.AotConsumers)
                    sb.AppendLine("   - " + name);
                sb.AppendLine();
                sb.AppendLine(LC.Tr(
                    "请先把这些引用移除，或改用 Launcher 启动场景，再执行打包。",
                    "Remove those references (or switch to the Launcher boot scene) before building."));
            }

            if (scan.MisconfiguredEditors.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(LC.Tr(
                    "ℹ 以下编辑器程序集没锁平台（asmdef 缺 includePlatforms: Editor），不会被拦下，但它们是配置问题，建议修掉：",
                    "ℹ These editor assemblies are not restricted to the Editor platform (missing includePlatforms: Editor). They are not blocked here, but the misconfiguration should be fixed:"));
                foreach (string name in scan.MisconfiguredEditors)
                    sb.AppendLine("   - " + name);
                sb.AppendLine();
                sb.AppendLine(LC.Tr(
                    "编辑器程序集不进包体，本来不会影响热更；这里列出来只是提醒它们的 asmdef 少了平台限制。",
                    "Editor assemblies do not ship in the player, so they do not affect hot update; they are listed only because their asmdef lacks the platform restriction."));
            }

            sb.AppendLine();
            sb.AppendLine(LC.Tr(
                "加入后需要执行 HybridCLR/Generate/All，并按拓扑序填写 LauncherConfig.HotfixDlls。",
                "After adding, run HybridCLR/Generate/All and fill LauncherConfig.HotfixDlls in topological order."));
            sb.AppendLine();
            sb.Append(LC.Tr("确认加入？", "Proceed?"));

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
                EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                    "读不到 HybridCLRSettings，请确认工程已安装 HybridCLR。",
                    "HybridCLRSettings is unavailable; make sure HybridCLR is installed."), LC.Tr("确定", "OK"));
                return false;
            }

            serialized = new SerializedObject(settings);
            list = serialized.FindProperty(DefinitionsProperty);
            if (list == null)
            {
                EditorUtility.DisplayDialog(DialogTitle, LC.Tr(
                        $"HybridCLRSettings 里找不到字段 {DefinitionsProperty}，HybridCLR 版本可能不兼容。",
                        $"Field {DefinitionsProperty} not found in HybridCLRSettings; the HybridCLR version may be incompatible."),
                    LC.Tr("确定", "OK"));
                return false;
            }

            return true;
        }

        #endregion
    }
}
