/*
 * ================================================
 * Describe:        Auto-bind LauncherConfig AOT metadata dlls from HybridCLR settings.
 * Author:          Alvin8412
 * CreationTime:    2026-11-16 18:45:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-16 18:45:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System.Collections.Generic;
using EasyFramework.Edit;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Launcher.Editor
{
    /// <summary>
    /// 绑定结果：绑定数、缺失数、是否真正写入。
    /// <para>Binding outcome: bound count, missing count, whether the asset was written.</para>
    /// </summary>
    internal sealed class AotBindResult
    {
        /// <summary>成功绑定条数<para>Number of bound entries.</para></summary>
        public int Bound;

        /// <summary>缺失条数<para>Number of missing entries.</para></summary>
        public int Missing;

        /// <summary>是否写入了配置资产<para>Whether the config asset was written.</para></summary>
        public bool Changed;

        /// <summary>缺失的程序集名<para>Names of the missing assemblies.</para></summary>
        public readonly List<string> MissingNames = new List<string>();

        /// <summary>错误信息（如 HybridCLR 设置缺失）<para>Error message, e.g. missing HybridCLR settings.</para></summary>
        public string Error;
    }

    /// <summary>
    /// 把 <see cref="LauncherConfig.AotMetadataDlls"/> 与 <c>HybridCLRSettings.patchAOTAssemblies</c> 对齐。
    /// <para>Aligns the launcher's AOT metadata list with HybridCLRSettings.patchAOTAssemblies.</para>
    /// </summary>
    /// <remarks>
    /// 为什么必须有这一步：<c>.dll.bytes</c> 每次重新导出都会重新生成 <c>.meta</c>，GUID 随之变化，
    /// 旧引用会变成 Missing。运行期 <c>Launcher.TryLoadAotMetadata</c> 对 Missing 是直接失败（不静默跳过），
    /// 所以「重新 Generate 之后忘了重新拖 3 个资产」会表现为启动卡在第 5 步。
    /// <para>Re-exporting the bytes assets regenerates their .meta GUIDs, so the old references go missing.
    /// The runtime fails hard on missing entries, which makes a forgotten re-bind look like a boot hang.</para>
    /// </remarks>
    internal static class LauncherAotMetadataBinder
    {
        /// <summary>
        /// 按 <c>patchAOTAssemblies</c> 顺序重建列表；有缺失时整体不写，避免把配置改成必然启动失败的形态。
        /// <para>Rebuilds the list in patchAOTAssemblies order; writes nothing when any entry is missing.</para>
        /// </summary>
        /// <param name="config">目标配置<para>Target config.</para></param>
        /// <param name="options">绑定规则（复用热更 dll 的搜索目录）<para>Bind options, reusing the hotfix dll search root.</para></param>
        /// <param name="apply">false = 只预览<para>false = preview only.</para></param>
        /// <returns>绑定结果<para>Binding outcome.</para></returns>
        public static AotBindResult Bind(LauncherConfig config, HotfixBindOptions options, bool apply)
        {
            var result = new AotBindResult();
            if (config == null)
            {
                result.Error = LC.Combine("Launcher.ConfigRequired");
                return result;
            }

            var settings = HybridCLRSettings.Instance;
            if (settings == null)
            {
                result.Error = LC.Combine("Launcher.NoHybridClrSettings");
                return result;
            }

            string[] names = settings.patchAOTAssemblies;
            if (names == null || names.Length == 0)
                return result;

            var assets = new TextAsset[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string assetPath = LauncherHotfixEntryBinder.FindAssetPath(names[i], options);
                var asset = assetPath == null ? null : AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

                if (asset == null)
                {
                    result.Missing++;
                    result.MissingNames.Add(names[i]);
                    continue;
                }

                assets[i] = asset;
                result.Bound++;
            }

            if (!apply || result.Missing > 0)
                return result;

            if (!NeedsWrite(config.AotMetadataDlls, assets, options))
                return result;

            config.AotMetadataDlls = assets;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            result.Changed = true;

            return result;
        }

        // 引用未变就不脏化资产，避免每次点同步都产生一次无意义的 SaveAssets
        private static bool NeedsWrite(TextAsset[] current, TextAsset[] next, HotfixBindOptions options)
        {
            if (options.OverwriteExisting || current == null || current.Length != next.Length)
                return true;

            for (int i = 0; i < next.Length; i++)
            {
                if (!ReferenceEquals(current[i], next[i]))
                    return true;
            }

            return false;
        }
    }
}
