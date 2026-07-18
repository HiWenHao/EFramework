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

namespace EasyFramework.Systems.Archive.Editor
{
    /// <summary>
    /// Please modify the description。
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
