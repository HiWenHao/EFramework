/* 
 * ================================================
 * Describe:      This script is used to show the menus in projects title. 
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2023-02-15 17:01:26
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-02-15 17:01:26
 * ScriptVersion: 0.1
 * ===============================================
*/
using UnityEditor;

namespace EasyFramework.Windows
{
    /// <summary>
    /// Show the menus in projects title。
    /// </summary>
    public sealed class EFMenus
	{
        #region Settings
        [MenuItem("EFTools/Settings/Project Settings", priority = 100)]
        public static void OpenDeerSettings() => SettingsService.OpenProjectSettings("EF/Project Setting");
        [MenuItem("EFTools/Settings/Auto Bind Setting", priority = 200)]
        public static void OpenAutoBindGlobalSettings() => SettingsService.OpenProjectSettings("EF/Auto Bind Setting");
        [MenuItem("EFTools/Settings/Optimal Setting", priority = 300)]
        public static void OpenOptimalSettings() => SettingsService.OpenProjectSettings("EF/Optimal Setting");
        #endregion
    }
}
