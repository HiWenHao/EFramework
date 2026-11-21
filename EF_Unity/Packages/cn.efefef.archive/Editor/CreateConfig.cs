/*
 * ================================================
 * Describe:        This script is used to .
 * Author:          Alvin8412
 * CreationTime:    2026-06-30 19:42:38
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-06-30 19:42:38
 * ScriptVersion:   0.1
 * ================================================
 */

using EasyFramework.Edit.Create;
using UnityEditor;

namespace EasyFramework.Archive
{
    /// <summary>
    /// 注册 Assets/Create 菜单项，用于创建 ArchiveSettings 配置资产。
    /// <para>Registers the Assets/Create menu item that creates the ArchiveSettings asset.</para>
    /// </summary>
    public class CreateConfig
    {
        [MenuItem("Assets/Create/EF/Archive Settings", priority = 311)]
        private static void CreatedArchiveSettings()
        {
            CreateSettings.Instance<ArchiveSettings>();
        }
    }
}
