/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-20 11:07:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-20 11:07:40
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 编辑器下通用命令统一
    /// </summary>
    public static class EditorCommands
    {
        /// <summary>
        /// 刷新编辑器
        /// <para>Editor refresh</para>
        /// </summary>
        public static void Refresh()
        {
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 将未保存的内容写入磁盘
        /// </summary>
        public static void SaveAssets()
        {
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 在系统文件管理器中打开文件夹本身（显示目录内容，而不是在父级选中该文件夹）
        /// <para>Open the folder itself in the OS file manager, showing its contents.</para>
        /// </summary>
        /// <param name="folderPath">文件夹路径，绝对路径或工程根相对路径（如 Assets/...） / Folder path, absolute or project-root relative</param>
        public static void OpenFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                D.Warning(LC.Tr(
                    "[ EditorCommands ] 路径为空，无法打开文件夹。",
                    "[ EditorCommands ] Folder path is empty; cannot open."));
                return;
            }

            string full = Path.IsPathRooted(folderPath)
                ? folderPath
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", folderPath));
            if (!Directory.Exists(full))
            {
                D.Warning(LC.Tr(
                    "[ EditorCommands ] 文件夹不存在，无法打开：{0}",
                    "[ EditorCommands ] Folder not found, cannot open: {0}",
                    full));
                return;
            }

            // 先去掉已有分隔符，避免重复斜杠
            full = Path.GetFullPath(full).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
#if UNITY_EDITOR_WIN
            // Windows：尾部分隔符让 Shell 把路径识别为「要打开的文件夹」而非「要选中的对象」，
            // 从而直接显示目录内容；复用已运行的 explorer，不新建进程，没有启动应用的忙光标。此斜杠不可删。
            EditorUtility.RevealInFinder(full + "/");
#else
            // macOS / Linux：尾部分隔符可能被 NSWorkspace 等标准化掉，仍会退回「在父级选中该目录」；
            // 改为定位目录内任一子项 —— 传文件/子目录时 RevealInFinder 会打开其所在目录，同样不新建进程。
            EditorUtility.RevealInFinder(ResolveRevealTarget(full));
#endif
        }

#if !UNITY_EDITOR_WIN
        // 非 Windows 分支专用：优先取目录内第一个文件，其次第一个子目录，按名称排序保证每次高亮一致；
        // 空目录没有子项时退化为定位目录本身（会打开父级，但空目录本就无内容可显示）。
        private static string ResolveRevealTarget(string full)
        {
            try
            {
                string[] files = Directory.GetFiles(full);
                System.Array.Sort(files, System.StringComparer.OrdinalIgnoreCase);
                if (files.Length > 0) return files[0];

                string[] folders = Directory.GetDirectories(full);
                System.Array.Sort(folders, System.StringComparer.OrdinalIgnoreCase);
                if (folders.Length > 0) return folders[0];
            }
            catch (System.Exception e)
            {
                D.Warning(LC.Tr(
                    "[ EditorCommands ] 读取目录内容失败，已回退为定位目录：{0}\n{1}",
                    "[ EditorCommands ] Failed to enumerate folder contents; falling back to revealing the folder: {0}\n{1}",
                    full, e.Message));
            }

            return full;
        }
#endif

        public static void CreateAssetsObject(string assetsName)
        {
            ProjectWindowUtil.CreateAssetWithContent(assetsName, "Assets/");
        }
    }
}