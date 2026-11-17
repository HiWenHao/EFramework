/*
 * ================================================
 * Describe:      统一编辑器工具集下获取对应内容
 * Author:        Alvin8412
 * CreationTime:  2026-05-28 16:22:11
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-17 18:50:00
 * ScriptVersion: 0.3
 * ================================================
 */

using System;

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
        /// 获取当前用户的署名。来源是逐用户本地文件（<c>EFTools/Local/EditorUser.txt</c>），
        /// 不读 <c>ProjectConfig</c>——那会让一个人的署名覆盖全组，且每次提交都冲突。
        /// <para>Gets the current user's display name from the per-user local file, not from ProjectConfig.</para>
        /// </summary>
        public static string GetAuthorName()
        {
            return LocalUserSettings.Author;
        }
    }
}
