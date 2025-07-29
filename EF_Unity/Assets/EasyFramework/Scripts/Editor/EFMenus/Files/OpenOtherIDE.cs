/* 
 * ================================================
 * Describe:      This script is used to open file with other ide.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-04-24 20:26:01
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-04-24 20:26:01
 * ScriptVersion: 0.1
 * ===============================================
*/
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit
{
    namespace AssetToolLibrary
    {
        /// <summary>
        /// Open file with other ide.
        /// </summary>
        public class OpenOtherIDE
        {
            [MenuItem("Assets/EF/Open With IDE/Sublime", false, 1)]
            private static void OpenSublime()
            {
                var args = string.Join(" ", GetPathsOfAssets(Selection.objects, false));
                OpenIDEWithPath(ProjectUtility.Path.SublimePath, args, _sublimePaths);

            }
            [MenuItem("Assets/EF/Open With IDE/Sublime *meta", false, 2)]
            private static void OpenSublimeMeta()
            {
                var args = string.Join(" ", GetPathsOfAssets(Selection.objects, true));
                OpenIDEWithPath(ProjectUtility.Path.SublimePath, args, _sublimePaths);

            }

            [MenuItem("Assets/EF/Open With IDE/Notepad++", false, 3)]
            private static void OpenNotepad()
            {
                var args = string.Join(" ", GetPathsOfAssets(Selection.objects, false));
                OpenIDEWithPath(ProjectUtility.Path.NotepadPath, args, _notepadPaths);
            }
            [MenuItem("Assets/EF/Open With IDE/Notepad++ *meta", false, 4)]
            private static void OpenNotepadMeta()
            {
                var args = string.Join(" ", GetPathsOfAssets(Selection.objects, true));
                OpenIDEWithPath(ProjectUtility.Path.NotepadPath, args, _notepadPaths);

            }

            private static string[] _notepadPaths = new string[] {
            @"C:\Program Files\Notepad++\notepad++.exe",
            @"C:\Program Files (x86)\Notepad++\notepad++.exe",
            @"C:\Programs\Notepad++\notepad++.exe",

            @"D:\Program Files\Notepad++\notepad++.exe",
            @"D:\Program Files (x86)\Notepad++\notepad++.exe",
            @"D:\Programs\Notepad++\notepad++.exe",
        };
            private static string[] _sublimePaths = new string[] {
            @"C:\Program Files\Sublime Text 3\subl.exe",
            @"C:\Program Files\Sublime Text 3\sublime_text.exe",
            @"C:\Program Files\Sublime Text 2\sublime_text.exe",
            @"C:\Program Files (x86)\Sublime Text 3\subl.exe",
            @"C:\Program Files (x86)\Sublime Text 3\sublime_text.exe",
            @"C:\Program Files (x86)\Sublime Text 2\sublime_text.exe",
            @"C:\Programs\Sublime Text 3\subl.exe",
            @"C:\Programs\Sublime Text 3\sublime_text.exe",
            @"C:\Programs\Sublime Text 2\sublime_text.exe",

            @"D:\Program Files\Sublime Text 3\subl.exe",
            @"D:\Program Files\Sublime Text 3\sublime_text.exe",
            @"D:\Program Files\Sublime Text 2\sublime_text.exe",
            @"D:\Program Files (x86)\Sublime Text 3\subl.exe",
            @"D:\Program Files (x86)\Sublime Text 3\sublime_text.exe",
            @"D:\Program Files (x86)\Sublime Text 2\sublime_text.exe",
            @"D:\Programs\Sublime Text 3\subl.exe",
            @"D:\Programs\Sublime Text 3\sublime_text.exe",
            @"D:\Programs\Sublime Text 2\sublime_text.exe",
            @"/Applications/Sublime Text.app/Contents/MacOS/sublime_text",
        };

            /// <summary>
            /// Open file with other IDE.
            /// </summary>
            /// <param name="appPath">IDE path</param>
            /// <param name="filePath">Selection objcet path</param>
            private static void OpenIDEWithPath(string appPath, string filePath, string[] defaultPath)
            {
                string path = appPath;
                bool hasPath = File.Exists(path);
                if (!hasPath)
                {
                    if (null == defaultPath)
                    {
                        EditorUtility.DisplayDialog("Error.错误", $"The program could not be found.\n" +
                            $"Please go to Settings to configure the path first.\n" +
                            $"EFTools > Settings > Optimal Setting", "ok");
                        return;
                    }
                    path = defaultPath.FirstOrDefault(File.Exists);
                    if (string.IsNullOrEmpty(path))
                    {
                        EditorUtility.DisplayDialog("Error.错误", $"The program could not be found.\n" +
                            $"Please go to Settings to configure the path first.\n" +
                            $"EFTools > Settings > Optimal Setting", "ok");
                        return;
                    }
                }
                System.Diagnostics.Process.Start(path, filePath);
            }

            /// <summary>
            /// Get path in Assets
            /// </summary>
            /// <returns></returns>
            private static IEnumerable<string> GetPathsOfAssets(Object[] objects, bool metas)
            {
                return objects
                        .Select(AssetDatabase.GetAssetPath)
                        .Where(p => !string.IsNullOrEmpty(p))
                        .Select(p => metas ? AssetDatabase.GetTextMetaFilePathFromAssetPath(p) : p)
                        .Select(p => '"' + p + '"')
                    ;
            }
        }
    }
}