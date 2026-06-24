/*
 * ================================================
 * Describe:      ScriptableObject 配置资产 ——
 *                所有存档系统的可调参数集中在此，
 *                用户通过 Editor 面板修改，无需改代码。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-24 23:19:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using UnityEngine;

namespace EasyFramework.Systems.Archive
{
    [CreateAssetMenu(fileName = "ArchiveSettings", menuName = "EF/Archive Settings", order = 100)]
    public class ArchiveSettings : ScriptableObject
    {
        [Header("槽位")]
        [Tooltip("最大存档槽位数量")]
        [Range(1, 20)]
        public int maxSlots = 5;

        [Tooltip("自动保存间隔（秒），0 表示禁用自动保存")]
        [Range(0, 600)]
        public int autoSaveIntervalSeconds = 120;

        [Tooltip("自动保存时是否只保存变动的数据（脏标记模式）")]
        public bool autoSaveOnlyDirty = true;

        [Header("加密")]
        [Tooltip("AES 密钥派生盐值（建议每款游戏使用不同值）。留空则使用设备唯一标识作为盐。")]
        public string encryptionSalt = "EF.Archive.DefaultSalt";

        [Tooltip("AES 密钥长度（128 / 192 / 256 位）")]
        [Range(128, 256)]
        public int aesKeySize = 256;

        [Tooltip("PBKDF2 迭代次数（越高越安全，越慢）")]
        [Range(1000, 100000)]
        public int pbkdf2Iterations = 10000;

        [Header("兼容性")]
        [Tooltip("当前存档数据格式版本号。版本变更时旧存档自动迁移。")]
        public int dataVersion = 1;

        [Tooltip("是否在读取时对未知 JSON 字段发出警告（调试用）")]
        public bool warnOnUnknownFields = false;

        [Header("备份")]
        [Tooltip("是否在每次写入前自动备份上一个版本")]
        public bool enableAutoBackup = true;

        [Tooltip("每个槽位保留的备份数量上限")]
        [Range(0, 10)]
        public int maxBackupCount = 3;

        [Header("存储后端")]
        [Tooltip("当前使用的存储 Provider 类型名称。空则使用默认 FileArchiveProvider。\n可选值: FileArchiveProvider, SqliteArchiveProvider（未来）, CloudArchiveProvider（未来）")]
        public string providerTypeName = string.Empty;

        [Tooltip("文件存储的根目录（相对于 persistentDataPath）。默认 'Archives'")]
        public string fileStorageRoot = "Archives";
    }
}
