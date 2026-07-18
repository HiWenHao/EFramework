/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin8412
 * CreationTime:  2026-05-20 11:07:40
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-05-20 11:07:40
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEditor;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 编辑器下通用命令统一
    /// </summary>
    public static class EditorCommands
    {
        /// <summary>
        /// 刷新编辑器
        /// <para>Editor refresh</para>
        /// </summary>
        public static void Refresh()
        {
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 将未保存的内容写入磁盘
        /// </summary>
        public static void SaveAssets()
        {
            AssetDatabase.SaveAssets();
        }

        public static void CreateAssetsObject(string assetsName)
        {
            ProjectWindowUtil.CreateAssetWithContent(assetsName, "Assets/");
        }
    }
}
