/* 
 * ================================================
 * Describe:      This script is used to quickly open the relevant folder.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-05-12 17:11:40
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-12 17:11:40
 * ScriptVersion: 0.1
 * ===============================================
*/
using UnityEditor;
using UnityEngine;

namespace EFExample
{
    /// <summary>
    /// Quickly open the relevant folder
    /// </summary>
    public class OpenFolder
	{
        [MenuItem("EFTools/Open Folder/Assets", priority = 1000)]
        public static void OpenDataPath()
        {
            Application.OpenURL("file://" + Application.dataPath);
        }

        [MenuItem("EFTools/Open Folder/Library", priority = 1001)]
        public static void OpenLibraryPath()
        {
            Application.OpenURL("file://" + Application.dataPath + "/../Library");
        }

        [MenuItem("EFTools/Open Folder/streamingAssets", priority = 1002)]
        public static void OpenStreamingAssetsPath()
        {
            Application.OpenURL("file://" + Application.streamingAssetsPath);
        }

        [MenuItem("EFTools/Open Folder/persistentData", priority = 1003)]
        public static void OpenPersistent()
        {
            Application.OpenURL("file://" + Application.persistentDataPath);
        }

        [MenuItem("EFTools/Open Folder/temporaryCache", priority = 1004)]
        public static void OpenTemporaryCachePath()
        {
            Application.OpenURL("file://" + Application.temporaryCachePath);
        }

        [MenuItem("EFTools/Open Folder/Excel", priority = 2000)]
        public static void OpenExcelFolderPath()
        {
            Application.OpenURL("file://" + Application.dataPath + "/../../ExcelFolder");
        }

        [MenuItem("EFTools/Open Folder/Luban", priority = 2001)]
        public static void OpenLubanToolsFolderPath()
        {
            Application.OpenURL("file://" + Application.dataPath + "/../../Tools/LubanTools");
        }
    }
}
