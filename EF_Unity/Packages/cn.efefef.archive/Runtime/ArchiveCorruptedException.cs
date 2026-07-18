/*
 * ================================================
 * Describe:      存档损坏异常。当 SHA256 校验不匹配或文件格式错误时抛出。
 *                文件系统会自动保留 .bak 备份，可尝试回退恢复。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 01:00:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.Archive
{
    /// <summary>
    /// 存档损坏异常，当 SHA256 校验失败或文件格式错误时抛出。系统自动保留 .bak 备份供回退。
    /// <para>Thrown when SHA256 verification fails or file format is invalid. .bak backups are kept for recovery.</para>
    /// </summary>
    public class ArchiveCorruptedException : Exception
    {
        /// <summary>损坏文件的路径<para>Path of the corrupted file</para></summary>
        public string FilePath { get; }

        /// <summary>
        /// 创建存档损坏异常<para>Create an archive corruption exception</para>
        /// </summary>
        /// <param name="filePath">损坏文件路径<para>Corrupted file path</para></param>
        /// <param name="message">错误描述<para>Error description</para></param>
        public ArchiveCorruptedException(string filePath, string message)
            : base($"[ArchiveCorrupted] {filePath}: {message}")
        {
            FilePath = filePath;
        }

        /// <summary>
        /// 创建存档损坏异常（带内部异常）<para>Create an archive corruption exception (with inner exception)</para>
        /// </summary>
        /// <param name="filePath">损坏文件路径<para>Corrupted file path</para></param>
        /// <param name="message">错误描述<para>Error description</para></param>
        /// <param name="inner">内部异常<para>Inner exception</para></param>
        public ArchiveCorruptedException(string filePath, string message, Exception inner)
            : base($"[ArchiveCorrupted] {filePath}: {message}", inner)
        {
            FilePath = filePath;
        }
    }
}
