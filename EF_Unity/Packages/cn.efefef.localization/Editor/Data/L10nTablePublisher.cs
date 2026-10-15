/*
 * ================================================
 * Describe:        导表产物自动同步器：把 l10n_tbtext_*.json 自动同步进 Resources/L10n
 * Author:          Alvin5100
 * CreationTime:    2026-10-15 18:35:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-15 18:35:00
 * ScriptVersion:   0.1
 * ================================================
 */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Localization.Edit
{
    /// <summary>
    /// 导表产物自动同步器
    /// <para>Luban 导表输出目录（默认 <c>Assets/luban/Data</c>）每次导表都会被
    /// 整体清空重建，不能作为常驻资源目录；而运行时数据通道
    /// <see cref="L10nTableStore"/> 只认 <c>Resources/L10n</c>。
    /// 本类监听资源导入事件：凡是 <c>l10n_tbtext_*.json</c> 进入 Assets
    /// （不论来自哪个目录），都原样同步一份到 <c>Resources/L10n</c>，
    /// 使用者从填译文到运行时看到效果，全程零手工步骤、零代码。</para>
    ///
    /// <para><b>幂等与防循环：</b>目标文件与源内容一致时不写盘。这一条既是性能优化
    /// 更是正确性要求——否则同步产物的导入事件会再次触发本类，形成无限刷新。</para>
    ///
    /// <para><b>为何不处理删除：</b>Resources 副本属于运行时数据，宁可多留一张过期
    /// 语言表（只是多出一种可查语言，查不到的 key 自有缺词占位兜底），
    /// 也绝不自动删除用户资产——与框架「破坏性操作红线」一致。</para>
    ///
    /// <para>Auto-syncs Luban-exported <c>l10n_tbtext_*.json</c> tables into
    /// <c>Resources/L10n</c> on import; idempotent and loop-safe.</para>
    /// </summary>
    public sealed class L10nTablePublisher : AssetPostprocessor
    {
        /// <summary>
        /// 语言表文件名前缀（与 <see cref="L10nTableStore.TableFilePrefix"/> 保持一致）
        /// </summary>
        private const string TableFilePrefix = "l10n_tbtext_";

        /// <summary>
        /// 同步目标目录（由 LocalizationData.Folder 拼出，指向 Assets/Resources/L10n）
        /// <para>与 Key 清单同目录：运行时取数、编辑器取清单都只认这一处。</para>
        /// </summary>
        private static readonly string TargetDir = "Assets/" + LocalizationData.Folder;

        /// <summary>
        /// 资源导入回调（Unity 自动调用）
        /// </summary>
        /// <param name="importedAssets">本次导入的资产路径</param>
        /// <param name="deletedAssets">本次删除的资产路径（刻意忽略，见类注释）</param>
        /// <param name="movedAssets">本次移动的资产路径（导表流程不产生移动，忽略）</param>
        /// <param name="movedFromAssetPaths">移动源路径（忽略）</param>
        /// <param name="didDomainReload">本次导入是否伴随域重载</param>
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            if (importedAssets == null) return;

            foreach (var path in importedAssets)
            {
                if (IsL10nTable(path)) SyncToResources(path);
            }
        }

        /// <summary>
        /// 判断导入的资产是否为多语言语言表
        /// <para>只看文件名与扩展名，不限定来源目录：导表输出目录将来若调整，
        /// 本类不需要跟着改。</para>
        /// </summary>
        private static bool IsL10nTable(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (!path.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase)) return false;

            var fileName = Path.GetFileName(path);
            return fileName.StartsWith(TableFilePrefix, StringComparison.Ordinal)
                   && fileName.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 把语言表同步到 Resources 目录
        /// <para>逐字节比对：内容一致直接跳过（防导入循环的关键）；
        /// 不一致才覆写并触发一次目标资产导入。</para>
        /// </summary>
        /// <param name="sourcePath">导表产物的 Assets 相对路径</param>
        private static void SyncToResources(string sourcePath)
        {
            var targetPath = Path.Combine(TargetDir, Path.GetFileName(sourcePath)).Replace('\\', '/');

            // 目标与源是同一个文件（手工把表直接放进 Resources）时，比对结果必然相等，自然跳过
            if (File.Exists(targetPath) && FilesEqual(sourcePath, targetPath)) return;

            try
            {
                Directory.CreateDirectory(TargetDir);
                File.Copy(sourcePath, targetPath, true);
                AssetDatabase.ImportAsset(targetPath);

                // 让编辑器预览立即反映新译文（运行时缓存会随域重载自然刷新）
                EditorL10nPreview.Invalidate();

                Debug.Log($"[L10nTablePublisher] 语言表已自动同步：{sourcePath} → {targetPath}");
            }
            catch (IOException e)
            {
                Debug.LogError($"[L10nTablePublisher] 语言表同步失败：{sourcePath} → {targetPath}\n{e.Message}");
            }
        }

        /// <summary>
        /// 逐字节比较两个文件内容
        /// <para>语言表通常只有几 KB，直接读入比较即可；先比长度可省掉绝大多数全量比对。</para>
        /// </summary>
        private static bool FilesEqual(string leftPath, string rightPath)
        {
            var left = File.ReadAllBytes(leftPath);
            var right = File.ReadAllBytes(rightPath);

            if (left.Length != right.Length) return false;

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }

            return true;
        }
    }
}
