/*
 * ================================================
 * Describe:      存档损坏异常。当 SHA256 校验不匹配或文件格式错误时抛出。
 *                文件系统会自动保留 .bak 备份，可尝试回退恢复。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-24 23:19:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.Archive
{
    public class ArchiveCorruptedException : Exception
    {
        /// <summary>损坏文件的路径</summary>
        public string FilePath { get; }

        public ArchiveCorruptedException(string filePath, string message)
            : base($"[ArchiveCorrupted] {filePath}: {message}")
        {
            FilePath = filePath;
        }

        public ArchiveCorruptedException(string filePath, string message, Exception inner)
            : base($"[ArchiveCorrupted] {filePath}: {message}", inner)
        {
            FilePath = filePath;
        }
    }
}
