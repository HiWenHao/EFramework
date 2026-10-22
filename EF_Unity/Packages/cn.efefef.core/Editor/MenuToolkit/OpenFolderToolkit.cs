/*
 * ================================================
 * Describe:      This script is used to quickly open the relevant folder.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-12 17:11:40
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-22 16:30:00
 * ScriptVersion: 0.3
 * ===============================================
 */

using System.IO;
using EasyFramework.Edit.Windows.Dataable;
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
            // 打开配表数据目录（__tables__.xlsx 与各 value CSV 所在处）；路径与数据表配置面板共用同一来源
            OpenFolder(Path.Combine(DatableConfigPanel.ExcelFolderPath, "Datas"));
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
