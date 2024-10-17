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
        [MenuItem("EFTools/Folders/Assets", priority = 90000)]
        public static void OpenDataPath()
        {
            Application.OpenURL("file://" + Application.dataPath);
        }

        [MenuItem("EFTools/Folders/Library", priority = 90001)]
        public static void OpenLibraryPath()
        {
            Application.OpenURL("file://" + Application.dataPath + "/../Library");
        }

        [MenuItem("EFTools/Folders/StreamingAssets", priority = 90002)]
        public static void OpenStreamingAssetsPath()
        {
            Application.OpenURL("file://" + Application.streamingAssetsPath);
        }

        [MenuItem("EFTools/Folders/PersistentData", priority = 90003)]
        public static void OpenPersistent()
        {
            Application.OpenURL("file://" + Application.persistentDataPath);
        }

        [MenuItem("EFTools/Folders/TemporaryCache", priority = 90004)]
        public static void OpenTemporaryCachePath()
        {
            Application.OpenURL("file://" + Application.temporaryCachePath);
        }

        [MenuItem("EFTools/Folders/Excel", priority = 91001)]
        public static void OpenExcelFolderPath()
        {
            Application.OpenURL("file://" + Application.dataPath + "/../../ExcelFolder");
        }

        [MenuItem("EFTools/Folders/Luban", priority = 91002)]
        public static void OpenLubanToolsFolderPath()
        {
            Application.OpenURL("file://" + Application.dataPath + "/../../Tools/LubanTools");
        }
    }
}
