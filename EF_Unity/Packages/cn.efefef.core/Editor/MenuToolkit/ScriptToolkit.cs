/*
 * ================================================
 * Describe:        This script is used to change the script encoding format. Adapted from, thanks to the original author.
 * Author:          Xiaohei.Wang(Wenhao)
 * CreationTime:    2024-05-22 14:47:02
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-08 15:51:47
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace EasyFramework.Edit.MenuToolkit
{
    /// <summary>
    /// 脚本工具
    /// <para>Script utilities: batch-convert selected .cs files to UTF-8, and refresh the modify-info header block.</para>
    /// </summary>
    internal static class ScriptToolkit
    {
        // 编码写回用的静态实例，避免每次文件处理都重复分配。
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
        private static readonly Encoding Utf8Bom = new UTF8Encoding(true);
        private static readonly Encoding Utf32Be = new UTF32Encoding(true, true);
        private static readonly Encoding Utf32Le = new UTF32Encoding(false, true);

        // 头部扫描上限与预期字段数，替代原代码中的魔法数。
        private const int HeaderScanLimit = 15;
        private const int ExpectedHeaderFields = 3;

        /// <summary>
        /// Change the script encoding format to UTF-8将选中脚本转换为 UTF-8
        /// </summary>
        [MenuItem("Assets/EF/Script/Format to UTF-8", false, 100)]
        private static void ChangeScriptEncoding()
        {
            string assetPath = EditorUtils.GetSelectFilePath();
            if (!TryResolveScriptPath(assetPath, out string absPath)) return;

            try
            {
                Encoding src = DetectEncoding(absPath, out bool hasBom);

                // 已是 UTF-8（无 BOM）则跳过，避免无谓改写（改动 mtime / 干扰其它逻辑）。
                // Already UTF-8 without BOM: skip to avoid touching mtime for no reason.
                if (src is UTF8Encoding && !hasBom)
                {
                    D.Log($"[ ScriptToolkit ] 已是 UTF-8，无需转换：{assetPath}");
                    return;
                }

                string contents = File.ReadAllText(absPath, src);
                File.WriteAllText(absPath, contents, Utf8NoBom);
                D.Log($"[ ScriptToolkit ] 已转换为 UTF-8：{assetPath}");
            }
            catch (Exception e)
            {
                D.Error($"[ ScriptToolkit ] 转换编码失败：{assetPath}\n{e.Message}");
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Update the script modify information.更新选中脚本的修改信息
        /// </summary>
        [MenuItem("Assets/EF/Script/Update Modify", false, 101)]
        private static void UpdateScriptModify()
        {
            foreach (string assetPath in EditorUtils.GetSelectFilesPath())
            {
                if (!TryResolveScriptPath(assetPath, out string absPath)) continue;

                try
                {
                    Encoding src = DetectEncoding(absPath, out bool hasBom);
                    string[] contents = File.ReadAllLines(absPath, src);

                    if (contents.Length < 10) continue;

                    string configName = ConfigManager.Project?.ScriptAuthor;
                    string authorName =
                        EditorPrefs.GetString($"{ConfigManager.Project?.AppConst?.AppPrefix}EditorUser");
                    authorName = string.IsNullOrEmpty(configName) ||
                                 string.Equals(configName, "Default", StringComparison.Ordinal)
                        ? authorName
                        : configName;

                    string modifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string scriptVersion = ConfigManager.Project?.ScriptVersion ?? "0.1";

                    int replaced = 0;
                    for (int i = 0; i < contents.Length; i++)
                    {
                        string line = contents[i];
                        if (line.Contains("* ModifyAuthor:"))
                        {
                            contents[i] = $" * ModifyAuthor:    {authorName}";
                            replaced++;
                        }
                        else if (line.Contains("* ModifyTime:"))
                        {
                            contents[i] = $" * ModifyTime:      {modifyTime}";
                            replaced++;
                        }
                        else if (line.Contains("* ScriptVersion:"))
                        {
                            contents[i] = $" * ScriptVersion:   {scriptVersion}";
                            replaced++;
                        }

                        if (replaced == ExpectedHeaderFields || i >= HeaderScanLimit)
                            break;
                    }

                    if (replaced == 0) continue;

                    Encoding writeEnc = src is UTF8Encoding ? (hasBom ? Utf8Bom : Utf8NoBom) : src;
                    File.WriteAllLines(absPath, contents, writeEnc);
                }
                catch (Exception e)
                {
                    D.Error($"[ ScriptToolkit ] 更新修改信息失败：{assetPath}\n{e.Message}");
                }
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 将资源相对路径解析为绝对路径并做合法性校验
        /// <para>Resolve an asset-relative path to an absolute path and validate it</para>
        /// </summary>
        /// <returns>校验通过返回 true，并将绝对路径写入 <paramref name="absPath"/><para>True if valid; <paramref name="absPath"/> receives the resolved path.</para></returns>
        private static bool TryResolveScriptPath(string assetPath, out string absPath)
        {
            absPath = null;
            if (string.IsNullOrEmpty(assetPath)) return false;

            if (!string.Equals(Path.GetExtension(assetPath), ".cs", StringComparison.OrdinalIgnoreCase))
            {
                D.Warning("Please select a file of type c#, extension is '.cs'\t请选择一个类型为c#的文件，扩展名为 '.cs'");
                return false;
            }

            string root = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            absPath = Path.Combine(root, assetPath);
            if (!File.Exists(absPath))
            {
                D.Warning($"File not found: {assetPath}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 检测文件编码（优先 BOM，否则默认 UTF-8），并回传是否存在 BOM。
        /// <para>Detect file encoding — BOM first, fallback to UTF-8 — and report whether a BOM is present.</para>
        /// </summary>
        private static Encoding DetectEncoding(string filePath, out bool hasBom)
        {
            hasBom = false;
            byte[] bom = new byte[4];
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            int read = fs.Read(bom, 0, 4);
            switch (read)
            {
                case >= 3 when bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF:
                    hasBom = true;
                    return Utf8NoBom; // UTF-8 BOM
                case >= 2 when bom[0] == 0xFF && bom[1] == 0xFE:
                    return Encoding.Unicode; // UTF-16 LE
                case >= 2 when bom[0] == 0xFE && bom[1] == 0xFF:
                    return Encoding.BigEndianUnicode; // UTF-16 BE
                case >= 4 when bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF:
                    return Utf32Be; // UTF-32 BE
                case >= 4 when bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00:
                    return Utf32Le; // UTF-32 LE
            }

            return Utf8NoBom;
        }
    }
}
