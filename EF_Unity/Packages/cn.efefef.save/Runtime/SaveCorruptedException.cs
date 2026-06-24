/*
 * ================================================
 * Describe:      存档损坏异常。当 SHA256 校验不匹配或文件格式错误时抛出。
 *                文件系统会自动保留 .bak 备份，可尝试回退恢复。
 * Author:        Alvin8412
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-06-24 22:25:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.Save
{
    /// <summary>
    /// 存档文件损坏或校验失败时抛出的异常。
    /// <para>文件系统会自动保留 .bak 备份，捕获此异常后可尝试回退到备份。</para>
    /// </summary>
    public class SaveCorruptedException : Exception
    {
        /// <summary>损坏文件的路径</summary>
        public string FilePath { get; }

        public SaveCorruptedException(string filePath, string message)
            : base($"[SaveCorrupted] {filePath}: {message}")
        {
            FilePath = filePath;
        }

        public SaveCorruptedException(string filePath, string message, Exception inner)
            : base($"[SaveCorrupted] {filePath}: {message}", inner)
        {
            FilePath = filePath;
        }
    }
}
