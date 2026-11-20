/*
 * ================================================
 * Describe:      Datable-owned folder shortcut in the EFTools Folders menu.
 * Author:        Alvin5100
 * CreationTime:  2026-11-20 12:20:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-20 12:20:00
 * ScriptVersion: 0.1
 * ================================================
 */

using System.IO;
using UnityEditor;

namespace EasyFramework.Edit.Windows.Dataable
{
    /// <summary>
    /// Datable 包在 EFTools Folders 菜单下的文件夹快捷入口
    /// <para>Datable-owned folder shortcuts under the EFTools Folders menu.</para>
    /// </summary>
    public static class DatableFolderMenu
    {
        // core 的 OpenFolderToolkit 占用 FoldersPriority+1..+6；此处留出 >=11 的间隔以生成菜单分隔线
        // Core's OpenFolderToolkit uses FoldersPriority+1..+6; keep a >=11 gap so Unity inserts a separator.
        private const int ExcelFolderPriority = MenuItemToolkit.FoldersPriority + 20;

        /// <summary>
        /// 打开 Luban 表格数据目录（EFTools/Datable/Datas）
        /// <para>Open the Luban table data directory (EFTools/Datable/Datas).</para>
        /// </summary>
        [MenuItem(MenuItemToolkit.Folders + "Excel", false, ExcelFolderPriority)]
        public static void OpenExcelFolderPath()
        {
            EditorCommands.OpenFolder(Path.Combine(DatableConfigPanel.ExcelFolderPath, "Datas"));
        }
    }
}
