/*
 * ================================================
 * Describe:        Auto-bind HotfixDlls entries to their .dll.bytes assets.
 * Author:          Alvin8412
 * CreationTime:    2026-11-15 18:20:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using EasyFramework.Edit;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Launcher.Editor
{
    /// <summary>Location 生成规则，需与 YooAsset 收集器的 AddressRule 一致</summary>
    public enum HotfixLocationRule
    {
        /// <summary>Path.GetFileNameWithoutExtension：EF.UI.Runtime.dll（对应 AddressByFileName）</summary>
        FileName = 0,

        /// <summary>资产路径去掉 .bytes：Assets/AssetsHotfix/FullCode/EF.UI.Runtime.dll</summary>
        AssetPathWithoutBytes = 1,

        /// <summary>自定义前缀 + 程序集名：HotfixDlls/EF.UI.Runtime.dll</summary>
        CustomPrefix = 2,
    }

    /// <summary>自动绑定设置，随编辑器保存在 EditorPrefs（按工程路径隔离）</summary>
    public sealed class HotfixBindOptions
    {
        private const string PrefKey = "EF.Launcher.HotfixBinder.";

        public string SearchRoot = "Assets/AssetsHotfix/FullCode";
        public bool SearchWholeAssets = true;
        public HotfixLocationRule Rule = HotfixLocationRule.FileName;
        public string CustomPrefix = "HotfixDlls";
        public bool OverwriteExisting;
        public string LastConfigGuid = string.Empty;

        public void Save()
        {
            string key = PrefKey + Application.dataPath;
            EditorPrefs.SetString(key + ".Root", SearchRoot);
            EditorPrefs.SetBool(key + ".WholeAssets", SearchWholeAssets);
            EditorPrefs.SetInt(key + ".Rule", (int)Rule);
            EditorPrefs.SetString(key + ".Prefix", CustomPrefix);
            EditorPrefs.SetBool(key + ".Overwrite", OverwriteExisting);
            EditorPrefs.SetString(key + ".Config", LastConfigGuid);
        }

        public static HotfixBindOptions Load()
        {
            string key = PrefKey + Application.dataPath;
            return new HotfixBindOptions
            {
                SearchRoot = EditorPrefs.GetString(key + ".Root", "Assets/AssetsHotfix/FullCode"),
                SearchWholeAssets = EditorPrefs.GetBool(key + ".WholeAssets", true),
                Rule = (HotfixLocationRule)EditorPrefs.GetInt(key + ".Rule", (int)HotfixLocationRule.FileName),
                CustomPrefix = EditorPrefs.GetString(key + ".Prefix", "HotfixDlls"),
                OverwriteExisting = EditorPrefs.GetBool(key + ".Overwrite", false),
                LastConfigGuid = EditorPrefs.GetString(key + ".Config", string.Empty),
            };
        }
    }

    /// <summary>一行的预览结果</summary>
    public sealed class HotfixBindRow
    {
        public string AssemblyName;
        public string AssetPath;
        public string Location;
        public TextAsset Fallback;
        public string Status;
        public bool IsNew;
        public bool WillWrite;
    }

    public sealed class HotfixBindResult
    {
        public readonly List<HotfixBindRow> Rows = new List<HotfixBindRow>();
        public int Added;
        public int LocationBound;
        public int FallbackBound;
        public int Missing;
        public string Error;
    }

    /// <summary>
    /// 把 <see cref="LauncherConfig.HotfixDlls"/> 与磁盘上的 .dll.bytes 资产自动对齐。
    /// <para>条目全集来自 HybridCLRSettings.hotUpdateAssemblyDefinitions（而不是只扫 cn.efefef.*），
    /// 所以 ExampleGameHotfix 这类业务热更程序集不会漏。</para>
    /// </summary>
    public static class LauncherHotfixEntryBinder
    {
        private const string DefinitionsProperty = "hotUpdateAssemblyDefinitions";
        private const string BytesSuffix = ".dll.bytes";

        /// <summary>读热更列表，按 asmdef 依赖图排好拓扑序</summary>
        public static List<string> CollectAssemblyNames(out string error)
        {
            error = null;
            var result = new List<string>();

            var settings = HybridCLRSettings.Instance;
            if (settings == null)
            {
                error = LC.Tr("读不到 HybridCLRSettings，请确认工程已安装 HybridCLR。",
                    "HybridCLRSettings is unavailable; make sure HybridCLR is installed.");
                return result;
            }

            var serialized = new SerializedObject(settings);
            var list = serialized.FindProperty(DefinitionsProperty);
            if (list == null)
            {
                error = LC.Tr($"HybridCLRSettings 里找不到 {DefinitionsProperty}，HybridCLR 版本可能不兼容。",
                    $"Field {DefinitionsProperty} not found; the HybridCLR version may be incompatible.");
                return result;
            }

            var database = AsmdefDatabase.Build();
            var names = new List<string>();

            for (int i = 0; i < list.arraySize; i++)
            {
                var asset = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (asset == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(path))
                    continue;

                if (database.TryGetByPath(path, out var info))
                    names.Add(info.Name);
                else
                    names.Add(Path.GetFileNameWithoutExtension(path));
            }

            var entries = new HotUpdateDllEntry[names.Count];
            for (int i = 0; i < names.Count; i++)
                entries[i] = new HotUpdateDllEntry { Name = names[i], Enabled = true };

            var sorted = LauncherHotfixDllSortTool.Sort(entries, out _, out _);
            foreach (var entry in sorted)
            {
                string name = LauncherHotfixDllSortTool.ResolveAssemblyName(entry);
                if (!string.IsNullOrEmpty(name))
                    result.Add(name);
            }

            return result;
        }

        /// <summary>预览（apply=false）或写回（apply=true）</summary>
        public static HotfixBindResult Build(LauncherConfig config, HotfixBindOptions options, bool apply)
        {
            var result = new HotfixBindResult();
            if (config == null)
            {
                result.Error = LC.Tr("未指定 LauncherConfig。", "No LauncherConfig specified.");
                return result;
            }

            if (options == null)
                options = new HotfixBindOptions();

            var names = CollectAssemblyNames(out string error);
            if (!string.IsNullOrEmpty(error))
            {
                result.Error = error;
                return result;
            }

            var existing = new Dictionary<string, HotUpdateDllEntry>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<HotUpdateDllEntry>();

            if (config.HotfixDlls != null)
            {
                foreach (var entry in config.HotfixDlls)
                {
                    if (entry == null)
                        continue;

                    entries.Add(entry);
                    string name = LauncherHotfixDllSortTool.ResolveAssemblyName(entry);
                    if (!string.IsNullOrEmpty(name) && !existing.ContainsKey(name))
                        existing[name] = entry;
                }
            }

            foreach (string name in names)
            {
                bool isNew = !existing.TryGetValue(name, out var entry);
                if (isNew)
                {
                    entry = new HotUpdateDllEntry { Name = name, Enabled = true };
                    entries.Add(entry);
                    existing[name] = entry;
                    result.Added++;
                }

                var row = new HotfixBindRow { AssemblyName = name, IsNew = isNew };
                result.Rows.Add(row);

                string assetPath = FindAssetPath(name, options);
                row.AssetPath = assetPath;

                if (assetPath == null)
                {
                    row.Status = LC.Tr("缺 .dll.bytes", "missing .dll.bytes");
                    result.Missing++;
                    continue;
                }

                string location = BuildLocation(name, assetPath, options);
                var fallback = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

                row.Location = location;
                row.Fallback = fallback;

                bool writeLocation = options.OverwriteExisting || string.IsNullOrWhiteSpace(entry.Location);
                bool writeFallback = options.OverwriteExisting || entry.FallbackAsset == null;

                if (apply && writeLocation)
                    entry.Location = location;
                if (apply && writeFallback)
                    entry.FallbackAsset = fallback;

                if (writeLocation)
                    result.LocationBound++;
                if (writeFallback)
                    result.FallbackBound++;

                row.WillWrite = writeLocation || writeFallback;
                if (!row.WillWrite)
                {
                    bool mismatch = !string.IsNullOrWhiteSpace(entry.Location) &&
                                    !string.Equals(entry.Location, location, StringComparison.Ordinal);
                    row.Status = mismatch
                        ? LC.Tr("⚠ 保留（与规则不符）", "⚠ kept (rule mismatch)")
                        : LC.Tr("保留原值", "kept");
                }
                else
                {
                    row.Status = isNew
                        ? LC.Tr("新增并绑定", "added + bound")
                        : LC.Tr("已绑定", "bound");
                }
            }

            if (apply)
            {
                config.HotfixDlls = LauncherHotfixDllSortTool.Sort(entries.ToArray(), out _, out _);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            return result;
        }

        private static string FindAssetPath(string assemblyName, HotfixBindOptions options)
        {
            string fileName = assemblyName + BytesSuffix;

            if (!string.IsNullOrWhiteSpace(options.SearchRoot))
            {
                string candidate = options.SearchRoot.Trim().TrimEnd('/', '\\') + "/" + fileName;
                if (File.Exists(Path.GetFullPath(candidate)))
                    return candidate.Replace('\\', '/');
            }

            if (!options.SearchWholeAssets)
                return null;

            foreach (string guid in AssetDatabase.FindAssets(assemblyName))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (path.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return null;
        }

        private static string BuildLocation(string assemblyName, string assetPath, HotfixBindOptions options)
        {
            switch (options.Rule)
            {
                case HotfixLocationRule.AssetPathWithoutBytes:
                    return assetPath.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase)
                        ? assetPath.Substring(0, assetPath.Length - 6)
                        : assetPath;

                case HotfixLocationRule.CustomPrefix:
                    string prefix = string.IsNullOrWhiteSpace(options.CustomPrefix)
                        ? "HotfixDlls"
                        : options.CustomPrefix.Trim().TrimEnd('/', '\\');
                    return prefix + "/" + assemblyName + ".dll";

                default:
                    // YooAsset 的 AddressByFileName = Path.GetFileNameWithoutExtension(assetPath)
                    return Path.GetFileNameWithoutExtension(assetPath);
            }
        }
    }
}
