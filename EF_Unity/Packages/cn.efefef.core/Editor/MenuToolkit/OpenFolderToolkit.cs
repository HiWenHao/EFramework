/*
 * ================================================
 * Describe:      This script is used to quickly open the relevant folder.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-12 17:11:40
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-01 15:10:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System.IO;
using UnityEditor;
using UnityEngine;

namespace EasyFramework.Edit.MenuToolkit
{
    /// <summary>
    /// Quickly open the relevant folder
    /// <para>快速打开相关文件夹</para>
    /// </summary>
    public static class OpenFolderToolkit
    {
        [MenuItem(MenuItemToolkit.Folders + "Assets", false, MenuItemToolkit.FoldersPriority + 1)]
        public static void OpenDataPath()
        {
            OpenFolder(Application.dataPath);
        }

        [MenuItem(MenuItemToolkit.Folders + "Library", false, MenuItemToolkit.FoldersPriority + 2)]
        public static void OpenLibraryPath()
        {
            OpenFolder(Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath, "Library"));
        }

        [MenuItem(MenuItemToolkit.Folders + "StreamingAssets", false, MenuItemToolkit.FoldersPriority + 3)]
        public static void OpenStreamingAssetsPath()
        {
            OpenFolder(Application.streamingAssetsPath);
        }

        [MenuItem(MenuItemToolkit.Folders + "PersistentData", false, MenuItemToolkit.FoldersPriority + 4)]
        public static void OpenPersistent()
        {
            OpenFolder(Application.persistentDataPath);
        }

        [MenuItem(MenuItemToolkit.Folders + "TemporaryCache", false, MenuItemToolkit.FoldersPriority + 5)]
        public static void OpenTemporaryCachePath()
        {
            OpenFolder(Application.temporaryCachePath);
        }

        [MenuItem(MenuItemToolkit.Folders + "Excel", false, MenuItemToolkit.FoldersPriority + 6)]
        public static void OpenExcelFolderPath()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../ExcelFolder"));
            OpenFolder(path);
        }

        [MenuItem(MenuItemToolkit.Folders + "Luban", false, MenuItemToolkit.FoldersPriority + 7)]
        public static void OpenLubanToolsFolderPath()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Packages/cn.efefef.datable/LubanTools~"));
            OpenFolder(path);
        }

        private static void OpenFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
                EditorUtility.RevealInFinder(folderPath + "/");
            else
                D.Warning($"[OpenFolder] Folder not found: {folderPath}");
        }
    }
}
