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
#if UNITY_EDITOR
using EasyFramework.Edit;
#endif
using UnityEngine;

namespace EasyFramework.Systems.Archive
{
    /// <summary>
    /// 存档系统全局可调参数，通过 Editor 面板或 Project Settings 修改。
    /// <para>Archive system global settings, editable via Editor panel or Project Settings.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "ArchiveSettings", menuName = "EF/Archive Settings", order = 100)]
    public class ArchiveSettings : ScriptableObject
    {
#if UNITY_EDITOR
        [HeaderPro("槽位设置", "Slot Settings")]
#endif
        [Tooltip("最大存档槽位数量（SQLite 等后端可无视此限制）")]
        [Range(1, 99)]
        public int maxSlots = 10;

        [Tooltip("自动保存间隔（秒），0 表示禁用自动保存")]
        [Range(0, 600)]
        public int autoSaveIntervalSeconds = 120;

        [Tooltip("自动保存时是否只保存变动的数据（脏标记模式）")]
        public bool autoSaveOnlyDirty = true;

#if UNITY_EDITOR
        [HeaderPro("加密设置", "Encryption Settings")]
#endif
        [Tooltip("AES 密钥派生盐值（建议每款游戏使用不同值）。留空则使用设备唯一标识作为盐。")]
        public string encryptionSalt = "EF.Archive.DefaultSalt";

        [Tooltip("AES 密钥长度（128 / 192 / 256 位）")]
        [Range(128, 256)]
        public int aesKeySize = 256;

        [Tooltip("PBKDF2 迭代次数（越高越安全，越慢）")]
        [Range(1000, 100000)]
        public int pbkdf2Iterations = 10000;

#if UNITY_EDITOR
        [HeaderPro("兼容性设置", "Compatibility Settings")]
#endif
        [Tooltip("当前存档数据格式版本号。版本变更时旧存档自动迁移。")]
        public int dataVersion = 1;

        [Tooltip("是否在读取时对未知 JSON 字段发出警告（调试用）")]
        public bool warnOnUnknownFields = false;

#if UNITY_EDITOR
        [HeaderPro("备份设置", "Backup Settings")]
#endif
        [Tooltip("是否在每次写入前自动备份上一个版本")]
        public bool enableAutoBackup = true;

        [Tooltip("每个槽位保留的备份数量上限")]
        [Range(0, 10)]
        public int maxBackupCount = 3;

#if UNITY_EDITOR
        [HeaderPro("存储后端", "Storage Backend")]
#endif
        [Tooltip("当前使用的存储 Provider 类型名称。空则使用默认 FileArchiveProvider。")]
        public string providerTypeName = string.Empty;

        [Tooltip("文件存储的根目录（相对于 persistentDataPath）")]
        public string fileStorageRoot = "Archives";
    }
}
