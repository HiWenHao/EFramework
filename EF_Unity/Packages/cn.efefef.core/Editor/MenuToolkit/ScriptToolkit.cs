/*
 * ================================================
 * Describe:      This script is used to change the script encoding format. Adapted from, thanks to the original author.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-05-22 14:47:02
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-04-01 15:42:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace EasyFramework.Edit.MenuToolkit
{
    /// <summary>
    /// 脚本工具
    /// </summary>
    internal static class ScriptToolkit
    {
        /// <summary>
        /// Change the script encoding format
        /// </summary>
        [MenuItem("Assets/EF/Script/Format to UTF-8", false, 100)]
        private static void ChangeScriptEncoding()
        {
            string assetPath = EditorUtils.GetSelectFilePath();

            if (!Path.GetExtension(assetPath).ToLower().Equals(".cs"))
            {
                D.Warning("Please select a file of type c#, extension is '.cs'\t请选择一个类型为c#的文件，扩展名为 '.cs'");
                return;
            }

            string absPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath);
            if (string.IsNullOrEmpty(absPath)) return;

            Encoding detectedEncoding = DetectFileEncoding(absPath);
            string contents = File.ReadAllText(absPath, detectedEncoding);
            File.WriteAllText(absPath, contents, Encoding.UTF8);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Update the script modify information
        /// </summary>
        [MenuItem("Assets/EF/Script/Update Modify", false, 101)]
        private static void UpdateScriptModify()
        {
            foreach (string assetPath in EditorUtils.GetSelectFilesPath())
            {
                if (!Path.GetExtension(assetPath).ToLower().Equals(".cs"))
                {
                    D.Warning("Please select a file of type c#, extension is '.cs'\t请选择一个类型为c#的文件，扩展名为 '.cs'");
                    continue;
                }

                string absPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, assetPath);
                if (string.IsNullOrEmpty(absPath)) return;
                string[] contents = File.ReadAllLines(absPath);

                if (contents.Length < 10)
                    continue;

                string configName = ConfigManager.Project.ScriptAuthor;
                string authorName =
                    EditorPrefs.GetString($"{ConfigManager.Project.AppConst.AppPrefix}EditorUser");
                authorName = string.IsNullOrEmpty(configName) || configName.Equals("Default")
                    ? authorName
                    : configName;
                if (!contents[5].Contains("ModifyAuthor:"))
                    continue;
                contents[5] = $" * ModifyAuthor:    {authorName}";
                contents[6] = $" * ModifyTime:      {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                contents[7] = $" * ScriptVersion:   {ConfigManager.Project.ScriptVersion}";

                File.WriteAllLines(absPath, contents, Encoding.UTF8);
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 检测文件编码（优先 BOM，否则默认 UTF-8）
        /// <para>Detect file encoding — BOM first, fallback to UTF-8</para>
        /// </summary>
        private static Encoding DetectFileEncoding(string filePath)
        {
            byte[] bom = new byte[4];
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int read = fs.Read(bom, 0, 4);
                switch (read)
                {
                    case >= 3 when bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF:
                        return Encoding.UTF8; // UTF-8 BOM
                    case >= 2 when bom[0] == 0xFF && bom[1] == 0xFE:
                        return Encoding.Unicode; // UTF-16 LE
                    case >= 2 when bom[0] == 0xFE && bom[1] == 0xFF:
                        return Encoding.BigEndianUnicode; // UTF-16 BE
                    case >= 4 when bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF:
                        return new UTF32Encoding(true, true); // UTF-32 BE
                    case >= 4 when bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00:
                        return new UTF32Encoding(false, true); // UTF-32 LE
                }
            }
            return Encoding.UTF8;
        }
    }
}