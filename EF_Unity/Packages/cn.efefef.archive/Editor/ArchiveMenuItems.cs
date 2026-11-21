/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-07-14 16:51:23
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-07-14 16:51:23
 * ScriptVersion:   0.1
 * ================================================
 */

using System.IO;
using EasyFramework.Edit;
using UnityEngine;
using EasyFramework.Edit.Windows.ConfigPanel;
using UnityEditor;

namespace EasyFramework.Archive
{
    internal static class ArchiveMenuItems
    {
        private const int RootPriority = 80000;
        private const string Root = MenuItemToolkit.EfRoot + "🗃️ Archive/";

        // 打开 EF Configs 面板中的 Archive Settings 页面
        [MenuItem(Root + "⚙️ Open Config Panel", false, RootPriority + 1)]
        private static void OpenConfigPanel()
        {
            EFConfigsPanel.Open<ArchiveSettingsConfigPanel>();
        }

        // 在文件资源管理器中打开持久化存档目录
        [MenuItem(Root + "📂 Open Archives Folder", false, RootPriority + 2)]
        private static void OpenArchivesFolder()
        {
            var settings = Resources.Load<ArchiveConfig>("Configs/" + nameof(ArchiveConfig));
            string root = settings != null && !string.IsNullOrEmpty(settings.fileStorageRoot)
                ? settings.fileStorageRoot
                : "Archives";
            string path = Path.Combine(Application.persistentDataPath, root);
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (System.Exception ex)
            {
                D.Warning(LC.Tr(
                    "[Archive] 创建存档目录 '{0}' 失败：{1}",
                    "[Archive] Failed to create archives directory '{0}': {1}",
                    path, ex.Message));
                return;
            }

            EditorCommands.OpenFolder(path);
        }

        // 危险操作：清空磁盘上所有存档数据
        [MenuItem(Root + "🗑️ Clear All Archives", false, RootPriority + 3)]
        private static void ClearAllArchives()
        {
            if (!EditorUtility.DisplayDialog(LC.Combine("Archive.ClearAllArchives"),
                    LC.Combine("Archive.ClearAllHint"),
                    LC.Combine("Archive.YesDeleteEverything"), LC.Combine("Cancel")))
                return;

            var settings = Resources.Load<ArchiveConfig>("Configs/" + nameof(ArchiveConfig));
            string root = settings != null && !string.IsNullOrEmpty(settings.fileStorageRoot)
                ? settings.fileStorageRoot
                : "Archives";
            string path = Path.Combine(Application.persistentDataPath, root);
            if (Directory.Exists(path))
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                    D.Log(LC.Tr("[Archive] 已清空所有存档数据。", "[Archive] All archive data cleared."));
                }
                catch (System.Exception ex)
                {
                    D.Error(LC.Tr(
                        "[Archive] 清空存档目录 '{0}' 失败：{1}",
                        "[Archive] Failed to clear archives directory '{0}': {1}",
                        path, ex.Message));
                }
            }
            else
            {
                D.Log(LC.Tr("[Archive] 没有需要清空的存档目录。", "[Archive] No archives directory to clear."));
            }
        }
    }
}