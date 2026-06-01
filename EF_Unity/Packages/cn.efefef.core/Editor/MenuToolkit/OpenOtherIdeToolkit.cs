/*
 * ================================================
 * Describe:      This script is used to open file with other ide.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-24 20:26:01
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-01 15:54:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.MenuToolkit
{
    /// <summary>
    /// Open file with other ide.
    /// <para>用外部IDE打开选中文件</para>
    /// </summary>
    internal static class OpenOtherIdeToolkit
    {
        [MenuItem("Assets/EF/Open With IDE/Sublime", false, 1)]
        private static void OpenSublime()
        {
            var args = string.Join(" ", GetPathsOfAssets(Selection.objects, false));
            OpenIdeWithPath(ConfigManager.Path?.SublimePath, args, SublimePaths);
        }

        [MenuItem("Assets/EF/Open With IDE/Sublime *meta", false, 2)]
        private static void OpenSublimeMeta()
        {
            var args = string.Join(" ", GetPathsOfAssets(Selection.objects, true));
            OpenIdeWithPath(ConfigManager.Path?.SublimePath, args, SublimePaths);
        }

        [MenuItem("Assets/EF/Open With IDE/Notepad++", false, 3)]
        private static void OpenNotepad()
        {
            var args = string.Join(" ", GetPathsOfAssets(Selection.objects, false));
            OpenIdeWithPath(ConfigManager.Path?.NotepadPath, args, NotepadPaths);
        }

        [MenuItem("Assets/EF/Open With IDE/Notepad++ *meta", false, 4)]
        private static void OpenNotepadMeta()
        {
            var args = string.Join(" ", GetPathsOfAssets(Selection.objects, true));
            OpenIdeWithPath(ConfigManager.Path?.NotepadPath, args, NotepadPaths);
        }

        private static readonly string[] NotepadPaths =
        {
            @"C:\Program Files\Notepad++\notepad++.exe",
            @"C:\Program Files (x86)\Notepad++\notepad++.exe",
            @"C:\Programs\Notepad++\notepad++.exe",
            @"D:\Program Files\Notepad++\notepad++.exe",
            @"D:\Program Files (x86)\Notepad++\notepad++.exe",
            @"D:\Programs\Notepad++\notepad++.exe",
            // 包管理器安装路径
            $@"{System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile)}\scoop\apps\notepadplusplus\current\notepad++.exe",
        };

        private static readonly string[] SublimePaths =
        {
            @"C:\Program Files\Sublime Text\subl.exe",
            @"C:\Program Files\Sublime Text\sublime_text.exe",
            @"C:\Program Files\Sublime Text 3\subl.exe",
            @"C:\Program Files\Sublime Text 3\sublime_text.exe",
            @"C:\Program Files\Sublime Text 2\sublime_text.exe",
            @"C:\Program Files (x86)\Sublime Text\subl.exe",
            @"C:\Program Files (x86)\Sublime Text\sublime_text.exe",
            @"C:\Program Files (x86)\Sublime Text 3\subl.exe",
            @"C:\Program Files (x86)\Sublime Text 3\sublime_text.exe",
            @"C:\Programs\Sublime Text\subl.exe",
            @"C:\Programs\Sublime Text\sublime_text.exe",
            @"C:\Programs\Sublime Text 3\subl.exe",
            @"C:\Programs\Sublime Text 3\sublime_text.exe",
            @"D:\Program Files\Sublime Text\subl.exe",
            @"D:\Program Files\Sublime Text\sublime_text.exe",
            @"D:\Program Files\Sublime Text 3\subl.exe",
            @"D:\Program Files\Sublime Text 3\sublime_text.exe",
            @"D:\Programs\Sublime Text\subl.exe",
            @"D:\Programs\Sublime Text\sublime_text.exe",
            @"D:\Programs\Sublime Text 3\subl.exe",
            @"D:\Programs\Sublime Text 3\sublime_text.exe",
            @"/Applications/Sublime Text.app/Contents/MacOS/sublime_text",
        };

        /// <summary>
        /// 用指定 IDE 打开文件
        /// <para>Open files with the specified IDE</para>
        /// </summary>
        /// <param name="appPath">用户配置的 IDE 路径（优先）<para>User-configured IDE path (preferred)</para></param>
        /// <param name="filePath">要打开的文件路径<para>File paths to open</para></param>
        /// <param name="defaultPaths">默认搜索路径列表<para>Default search path list</para></param>
        private static void OpenIdeWithPath(string appPath, string filePath, string[] defaultPaths)
        {
            string path = appPath;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                System.Diagnostics.Process.Start(path, filePath);
                return;
            }

            if (defaultPaths != null)
            {
                path = defaultPaths.FirstOrDefault(File.Exists);
                if (!string.IsNullOrEmpty(path))
                {
                    System.Diagnostics.Process.Start(path, filePath);
                    return;
                }
            }

            EditorUtility.DisplayDialog("Error",
                "The program could not be found.\n" +
                "Please go to Settings to configure the path first.\n" +
                "EFTools > Settings > Optimal Setting",
                "OK");
        }

        /// <summary>
        /// 获取选中资源的磁盘路径（已加引号）
        /// <para>Get disk paths of selected assets (quoted)</para>
        /// </summary>
        private static IEnumerable<string> GetPathsOfAssets(Object[] objects, bool metas)
        {
            return objects
                    .Select(AssetDatabase.GetAssetPath)
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Select(p => metas ? AssetDatabase.GetTextMetaFilePathFromAssetPath(p) : p)
                    .Select(p => '"' + p.Replace("\"", "\\\"") + '"');
        }
    }
}
