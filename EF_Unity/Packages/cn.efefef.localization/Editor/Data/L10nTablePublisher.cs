/*
 * ================================================
 * Describe:        导表产物自动同步器：把 l10n_tbtext_*.json 自动同步进 Resources/L10n
 * Author:          Alvin5100
 * CreationTime:    2026-10-15 18:35:00
 * ModifyAuthor:    Alvin5100
 * ModifyTime:      2026-10-16 18:45:00
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
    /// 导表产物自动同步器：把 l10n_tbtext_*.json 自动同步进 Resources/L10n（导入事件驱动，使用者零手工步骤）
    /// <para>Auto-syncs Luban-exported l10n_tbtext_*.json tables into Resources/L10n
    /// on import; idempotent and loop-safe.</para>
    /// <para>同步目录的原因：Luban 导表输出目录每次导表会被整体清空重建，不能作为常驻资源目录，
    /// 而运行时数据通道只认 Resources/L10n。</para>
    /// <para>幂等与防循环：目标文件与源内容一致时不写盘——否则同步产物的导入事件会再次触发本类，形成无限刷新。</para>
    /// <para>为何不处理删除：Resources 副本属于运行时数据，宁可多留一张过期语言表，也绝不自动删除用户资产。</para>
    /// </summary>
    public sealed class L10nTablePublisher : AssetPostprocessor
    {
        #region Fields

        // 语言表文件名前缀（与 L10nTableStore.TableFilePrefix 保持一致）
        private const string TableFilePrefix = "l10n_tbtext_";

        // 同步目标目录（由 LocalizationData.Folder 拼出，指向 Assets/Resources/L10n；与 Key 清单同目录）
        private static readonly string TargetDir = "Assets/" + LocalizationData.Folder;

        #endregion

        #region Methods

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

        // 判断导入的资产是否为多语言语言表（只看文件名与扩展名，不限定来源目录：导表输出目录调整时本类不用改）
        private static bool IsL10nTable(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (!path.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase)) return false;

            var fileName = Path.GetFileName(path);
            return fileName.StartsWith(TableFilePrefix, StringComparison.Ordinal)
                   && fileName.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase);
        }

        // 把语言表同步到 Resources 目录（逐字节比对：内容一致直接跳过——防导入循环的关键；不一致才覆写并触发一次目标资产导入）
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

        // 逐字节比较两个文件内容（语言表通常只有几 KB，直接读入比较；先比长度可省掉绝大多数全量比对）
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

        #endregion
    }
}