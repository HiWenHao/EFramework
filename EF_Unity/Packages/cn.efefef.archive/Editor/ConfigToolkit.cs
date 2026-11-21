/*
 * ================================================
 * Describe:      Archive 配置访问入口（统一走 core 的 ConfigManager）
 * Author:        Alvin8412
 * CreationTime:  2026-06-30 19:42:38
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-11-21
 * ScriptVersion: 1.0
 * ===============================================
 */

using System;
using EasyFramework.Edit;
using UnityEditor;

namespace EasyFramework.Archive
{
    /// <summary>
    /// Archive 配置统一入口：所有编辑器代码都经此获取同一份 ArchiveConfig。
    /// <para>Archive config entry point: all editor code gets the same ArchiveConfig instance through here.</para>
    /// </summary>
    public static class ConfigToolkit
    {
        /// <summary>
        /// 生成唯一加密盐：前缀 + 32 位 hex = 43 字节 ASCII，满足 PBKDF2 对 salt >= 8 字节的要求
        /// <para>Generate a unique salt (43 ASCII bytes, satisfying PBKDF2 salt length >= 8)</para>
        /// </summary>
        public static void InitSalt(ArchiveConfig config) => config.encryptionSalt = $"EF.Archive.{Guid.NewGuid():N}";

        /// <summary>
        /// 获取全局唯一的 ArchiveConfig（不存在则创建并生成唯一盐）
        /// <para>Get the globally unique ArchiveConfig (create with a unique salt if missing)</para>
        /// </summary>
        /// <param name="focusExisting">已存在时选中并 Ping（仅创建菜单传 true）<para>Select and ping if existing</para></param>
        public static ArchiveConfig GetConfig(bool focusExisting = false) =>
            ConfigManager.Get<ArchiveConfig>(ConfigManager.ConfigRuntimePath, InitSalt,
                focusExisting: focusExisting);

        // Assets 下右键创建配置
        [MenuItem("Assets/Create/EF/Archive Config", priority = 311)]
        private static void CreatedArchiveSettings() => GetConfig(focusExisting: true);
    }
}
