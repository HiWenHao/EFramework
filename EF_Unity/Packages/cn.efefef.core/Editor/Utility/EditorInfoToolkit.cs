/*
 * ================================================
 * Describe:      统一编辑器工具集下获取对应内容
 * Author:        Alvin8412
 * CreationTime:  2026-05-28 16:22:11
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-06-01 14:50:00
 * ScriptVersion: 0.2
 * ================================================
 */

using System;
using UnityEditor;

namespace EasyFramework.Edit
{
    /// <summary>
    /// 统一编辑器工具集下获取对应内容
    /// <para>Unified editor toolkit for retrieving various editor content</para>
    /// </summary>
    public static class EditorInfoToolkit
    {
        /// <summary>
        /// 获取脚本注释头
        /// <para>Obtain the script comment header</para>
        /// </summary>
        /// <param name="describe">当前脚本描述<para>Current script description</para></param>
        public static string GetFileHead(string describe)
        {
            string authorName = GetAuthorName();
            string createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string version = ConfigManager.Project?.ScriptVersion ?? "0.1";

            return "/*\n"
                   + " * ================================================\r\n"
                   + $" * Describe:        {describe}\r\n"
                   + $" * Author:          {authorName}\r\n"
                   + $" * CreationTime:    {createTime}\r\n"
                   + $" * ModifyAuthor:    {authorName}\r\n"
                   + $" * ModifyTime:      {createTime}\r\n"
                   + $" * ScriptVersion:   {version}\r\n"
                   + " * ================================================\r\n"
                   + " */";
        }

        /// <summary>
        /// 获取当前编辑器的用户名，或是已经修改过的字段名
        /// <para>Get the username of the current editor, or the name of the field that has already been modified</para>
        /// </summary>
        public static string GetAuthorName()
        {
            string configName = ConfigManager.Project?.ScriptAuthor;
            string authorName = EditorPrefs.GetString($"{ConfigManager.Project?.AppConst?.AppPrefix}EditorUser");
            return string.IsNullOrEmpty(configName) || configName.Equals("Default") ? authorName : configName;
        }
    }
}
