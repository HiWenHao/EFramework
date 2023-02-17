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
        [MenuItem("EFTools/Settings/EF - ProjectSettings", priority = 100)]
        public static void OpenDeerSettings() => SettingsService.OpenProjectSettings("EF/ProjectSetting");
        [MenuItem("EFTools/Settings/EF - AutoBindSetting", priority = 200)]
        public static void OpenAutoBindGlobalSettings() => SettingsService.OpenProjectSettings("EF/AutoBindSetting");
    }
}
