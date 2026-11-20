/*
 * ================================================
 * Describe:      ScriptableObject 配置资产 ——
 *                所有存档系统的可调参数集中在此，
 *                用户通过 Editor 面板修改，无需改代码。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 01:00:00
 * ScriptVersion: 0.1
 * ===============================================
 */

// HeaderProAttribute 虽命名空间为 EasyFramework.Edit，但实际定义在
// cn.efefef.core/Runtime 程序集中（PropertyAttribute 子类），Runtime 安全。

using UnityEngine;

namespace EasyFramework.Systems.Archive
{
    /// <summary>
    /// AES 密钥长度枚举，仅允许 AES 规范内的合法值。
    /// <para>Valid AES key sizes as defined by the AES standard.</para>
    /// </summary>
    public enum AesKeySize
    {
        Bits128 = 128,
        Bits192 = 192,
        Bits256 = 256
    }

    /// <summary>
    /// 存档系统全局可调参数，通过 Editor 面板或 Project Settings 修改。
    /// <para>Archive system global settings, editable via Editor panel or Project Settings.</para>
    /// </summary>
    public class ArchiveSettings : ScriptableObject
    {
#if UNITY_EDITOR
        [HeaderPro("槽位设置", "Slot Settings")]
#endif
        /// <summary>
        /// 最大存档槽位数量（SQLite 等后端可无视此限制）
        /// <para>Maximum number of save slots (backends such as SQLite may ignore this limit)</para>
        /// </summary>
        [Tooltip("最大存档槽位数量（SQLite 等后端可无视此限制）")]
        [Range(1, 99)]
        public int maxSlots = 10;

        /// <summary>
        /// 自动保存间隔（秒），0 表示禁用自动保存
        /// <para>Auto-save interval in seconds; 0 disables auto-saving</para>
        /// </summary>
        [Tooltip("自动保存间隔（秒），0 表示禁用自动保存")]
        [Range(0, 600)]
        public int autoSaveIntervalSeconds = 120;

        /// <summary>
        /// 自动保存时是否只保存变动的数据（脏标记模式）
        /// <para>Whether to save only changed data during auto-save (dirty-flag mode)</para>
        /// </summary>
        [Tooltip("自动保存时是否只保存变动的数据（脏标记模式）")]
        public bool autoSaveOnlyDirty = true;

#if UNITY_EDITOR
        [HeaderPro("加密设置", "Encryption Settings")]
#endif
        /// <summary>
        /// AES 密钥派生盐值（建议每款游戏使用不同值）。留空则使用设备唯一标识作为盐。
        /// <para>AES key derivation salt (a different value is recommended for each game). If left empty, the device's unique identifier is used as the salt.</para>
        /// </summary>
        [Tooltip("AES 密钥派生盐值（建议每款游戏使用不同值）。留空则使用设备唯一标识作为盐。")]
        public string encryptionSalt = "EF.Archive.DefaultSalt";

        /// <summary>
        /// AES 密钥长度（仅允许 128 / 192 / 256 位，其他值运行时会自动修正为最近合法值）
        /// <para>AES key size (only 128 / 192 / 256 bits are allowed; other values are automatically corrected to the nearest valid value at runtime)</para>
        /// </summary>
        [Tooltip("AES 密钥长度（仅允许 128 / 192 / 256 位，其他值运行时会自动修正为最近合法值）")]
        public AesKeySize aesKeySize = AesKeySize.Bits256;

        /// <summary>
        /// PBKDF2 迭代次数（越高越安全，越慢）
        /// <para>Number of PBKDF2 iterations (higher is more secure but slower)</para>
        /// </summary>
        [Tooltip("PBKDF2 迭代次数（越高越安全，越慢）")]
        [Range(1000, 100000)]
        public int pbkdf2Iterations = 100000;

#if UNITY_EDITOR
        [HeaderPro("兼容性设置", "Compatibility Settings")]
#endif
        /// <summary>
        /// 当前存档数据格式版本号。版本变更时旧存档自动迁移。
        /// <para>Current save data format version. Old saves are automatically migrated when the version changes.</para>
        /// </summary>
        [Tooltip("当前存档数据格式版本号。版本变更时旧存档自动迁移。")]
        public int dataVersion = 1;

        /// <summary>
        /// 是否在反序列化时对未知 JSON 字段抛出异常（errorOnUnknownFields）。注意：开启后任何新增字段都会导致 Load 失败。
        /// <para>Whether to throw an exception for unknown JSON fields during deserialization (errorOnUnknownFields). Note: when enabled, any newly added field will cause Load to fail.</para>
        /// </summary>
        [Tooltip("是否在反序列化时对未知 JSON 字段抛出异常（errorOnUnknownFields）。注意：开启后任何新增字段都会导致 Load 失败。")]
        public bool warnOnUnknownFields = false;

#if UNITY_EDITOR
        [HeaderPro("备份设置", "Backup Settings")]
#endif
        /// <summary>
        /// 是否在每次写入前自动备份上一个版本
        /// <para>Whether to automatically back up the previous version before each write</para>
        /// </summary>
        [Tooltip("是否在每次写入前自动备份上一个版本")]
        public bool enableAutoBackup = true;

        /// <summary>
        /// 每个槽位保留的备份数量上限
        /// <para>Maximum number of backups retained per slot</para>
        /// </summary>
        [Tooltip("每个槽位保留的备份数量上限")]
        [Range(0, 10)]
        public int maxBackupCount = 3;

#if UNITY_EDITOR
        [HeaderPro("存储后端", "Storage Backend")]
#endif
        /// <summary>
        /// 当前使用的存储 Provider 类型名称。空则使用默认 FileArchiveProvider。
        /// <para>Type name of the storage provider currently in use. If empty, the default FileArchiveProvider is used.</para>
        /// </summary>
        [Tooltip("当前使用的存储 Provider 类型名称。空则使用默认 FileArchiveProvider。")]
        public string providerTypeName = string.Empty;

        /// <summary>
        /// 文件存储的根目录（相对于 persistentDataPath）
        /// <para>Root directory for file storage (relative to persistentDataPath)</para>
        /// </summary>
        [Tooltip("文件存储的根目录（相对于 persistentDataPath）")]
        public string fileStorageRoot = "Archives";
    }
}
