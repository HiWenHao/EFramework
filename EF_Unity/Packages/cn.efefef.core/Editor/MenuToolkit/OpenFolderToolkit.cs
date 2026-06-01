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
        [MenuItem("EFTools/Folders/Assets", priority = 90000)]
        public static void OpenDataPath()
        {
            OpenFolder(Application.dataPath);
        }

        [MenuItem("EFTools/Folders/Library", priority = 90001)]
        public static void OpenLibraryPath()
        {
            OpenFolder(Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath, "Library"));
        }

        [MenuItem("EFTools/Folders/StreamingAssets", priority = 90002)]
        public static void OpenStreamingAssetsPath()
        {
            OpenFolder(Application.streamingAssetsPath);
        }

        [MenuItem("EFTools/Folders/PersistentData", priority = 90003)]
        public static void OpenPersistent()
        {
            OpenFolder(Application.persistentDataPath);
        }

        [MenuItem("EFTools/Folders/TemporaryCache", priority = 90004)]
        public static void OpenTemporaryCachePath()
        {
            OpenFolder(Application.temporaryCachePath);
        }

        [MenuItem("EFTools/Folders/Excel", priority = 91001)]
        public static void OpenExcelFolderPath()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../ExcelFolder"));
            OpenFolder(path);
        }

        [MenuItem("EFTools/Folders/Luban", priority = 91002)]
        public static void OpenLubanToolsFolderPath()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Tools/LubanTools"));
            OpenFolder(path);
        }

        private static void OpenFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
                Application.OpenURL("file://" + folderPath);
            else
                Debug.LogWarning($"[OpenFolder] Folder not found: {folderPath}");
        }
    }
}
