/*
 * ================================================
 * Describe:      默认存档存储后端 —— 本地加密文件。
 *                每次写入：JSON序列化 → AES-256-CBC加密
 *                → SHA256校验 → 临时文件 → 原子重命名。
 *                防止写入中断导致存档损坏。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 17:30:00
 * ScriptVersion: 0.1.2
 * Changelog:
 *   0.1.2  新增 SaveRawSync / SaveMetaSync / FlushSync 同步写入路径。
 *          退出时走这些同步方法,完全不依赖 PlayerLoop,避免 UniTask 死锁。
 *   0.1.1  增加 ScanSlotDirectoryIds 公共辅助,用于 FindFreeSlotIdAsync 防御残留目录。
 *          GetOrCreateKey 增加空设备 ID 防御,防止 SystemInfo 异常导致空密钥。
 *   0.1.0  首版
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EasyFramework.Systems.Archive
{
    /// <summary>
    /// 基于本地文件系统的存档 Provider。每项数据加密存储为 .arc 文件，采用"临时文件 + 原子重命名"防写入中断。
    /// <para>Local filesystem archive provider. Each entry stored as an encrypted .arc file with temp-file + atomic rename.</para>
    /// <para>文件结构：{root}/Slot_{id}/{key}.arc</para>
    /// <para>File header: [4B MAGIC][16B IV][4B len][N B ciphertext][32B SHA256]</para>
    /// </summary>
    public class FileArchiveProvider : IArchiveProvider
    {
        private const uint FileMagic = 0x43524145; // "EARC" — 文件魔数标识
        private const string ArchiveExtension = ".arc";    // 存档文件扩展名
        private const string BackupExtension = ".bak";     // 备份文件扩展名
        private const string MetaFile = "slot_meta.json";  // 槽位元数据文件名

        private readonly string _rootPath;       // 存档根目录
        private readonly ArchiveSettings _settings; // 全局配置引用
        private readonly byte[] _salt;           // 加密盐值
        private byte[] _cachedKey;               // 缓存的 AES 密钥
        private readonly object _keyLock = new(); // 保护 _cachedKey 的锁
        private readonly Aes _cachedAes = Aes.Create(); // 池化 AES 实例（减少 GC）

        /// <summary>
        /// 创建本地文件存档 Provider<para>Create a local file-based archive provider</para>
        /// </summary>
        /// <param name="settings">存档全局配置<para>Archive settings</para></param>
        public FileArchiveProvider(ArchiveSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rootPath = Path.Combine(Application.persistentDataPath, settings.fileStorageRoot);
            _salt = Encoding.UTF8.GetBytes(settings.encryptionSalt);

            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
        }

        /// <summary>
        /// 扫描物理目录中所有 Slot_N 目录，返回解析出的 N 整数集合。
        /// 用于 ArchiveManager.FindFreeSlotIdAsync 防御"目录存在但无有效 meta"的残留场景。
        /// <para>Scan physical Slot_N directories and return parsed N integers.
        /// Used by ArchiveManager.FindFreeSlotIdAsync to recover from residual slot dirs without valid meta.</para>
        /// </summary>
        public HashSet<int> ScanSlotDirectoryIds()
        {
            var result = new HashSet<int>();
            if (!Directory.Exists(_rootPath)) return result;

            foreach (string dir in Directory.GetDirectories(_rootPath, "Slot_*"))
            {
                string name = Path.GetFileName(dir);
                if (string.IsNullOrEmpty(name)) continue;
                // "Slot_N" 长度至少 6（"Slot_0" = 6）
                if (name.Length < 6 || !name.StartsWith("Slot_", StringComparison.Ordinal)) continue;
                string idPart = name.Substring(5);
                if (int.TryParse(idPart, out int id) && id >= 0)
                    result.Add(id);
            }
            return result;
        }

        #region IArchiveProvider Implementation

        /// <summary>
        /// 加密并写入一条存档数据<para>Encrypt and write an archive entry</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="key">数据键名<para>Data key name</para></param>
        /// <param name="data">已序列化的 JSON 字节<para>Serialized JSON bytes</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        public async UniTask SaveRawAsync(int slotId, string key, byte[] data,
            CancellationToken cancellationToken = default)
        {
            EnsureSlotDirectory(slotId);

            byte[] encrypted = Encrypt(data);
            byte[] checksum = ComputeChecksum(encrypted);
            byte[] fileData = PackFile(encrypted, checksum);

            string finalPath = GetArchivePath(slotId, key);

            // 启用备份时：先轮转旧备份链，再将当前 .arc 复制为最新 .bak
            if (_settings.enableAutoBackup && File.Exists(finalPath))
            {
                CleanupOldBackups(slotId, key);            // 先移位旧备份链，防止旧的 .bak 被覆盖丢失
                File.Copy(finalPath, finalPath + BackupExtension);
            }

            string tempPath = finalPath + ".tmp";
            await WriteAllBytesAsync(tempPath, fileData, cancellationToken);
            AtomicReplace(tempPath, finalPath);
        }

        /// <summary>
        /// 同步写入存档数据。<b>退出流程专用</b>，直接用 FileStream 同步写，不依赖 PlayerLoop。
        /// <para>Synchronous save. <b>For shutdown paths only</b> — uses FileStream synchronously, no PlayerLoop dependency.</para>
        /// </summary>
        public void SaveRawSync(int slotId, string key, byte[] data)
        {
            EnsureSlotDirectory(slotId);

            byte[] encrypted = Encrypt(data);
            byte[] checksum = ComputeChecksum(encrypted);
            byte[] fileData = PackFile(encrypted, checksum);

            string finalPath = GetArchivePath(slotId, key);

            if (_settings.enableAutoBackup && File.Exists(finalPath))
            {
                CleanupOldBackups(slotId, key);
                File.Copy(finalPath, finalPath + BackupExtension);
            }

            string tempPath = finalPath + ".tmp";
            // 同步写,使用 FileOptions.WriteThrough 提示系统绕过缓存直接落盘
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                       FileShare.None, bufferSize: 4096, options: FileOptions.WriteThrough))
            {
                fs.Write(fileData, 0, fileData.Length);
                fs.Flush(true); // flushToDisk = true
            }
            AtomicReplace(tempPath, finalPath);
        }

        /// <summary>
        /// 读取并校验一条存档数据<para>Load and verify an archive entry</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="key">数据键名<para>Data key name</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>解密后的原始字节，不存在则返回 null<para>Decrypted raw bytes, or null if not found</para></returns>
        public async UniTask<byte[]> LoadRawAsync(int slotId, string key,
            CancellationToken cancellationToken = default)
        {
            string path = GetArchivePath(slotId, key);

            if (File.Exists(path))
            {
                byte[] fileData = await ReadAllBytesAsync(path, cancellationToken);
                return DecryptAndVerify(fileData, path);
            }

            return null;
        }

        /// <summary>
        /// 检查存档文件是否存在<para>Check whether the archive file exists</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="key">数据键名<para>Data key name</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>存在返回 true<para>true if found</para></returns>
        public UniTask<bool> ExistsAsync(int slotId, string key,
            CancellationToken cancellationToken = default)
        {
            string path = GetArchivePath(slotId, key);
            return UniTask.FromResult(File.Exists(path));
        }

        /// <summary>删除存档文件及其所有备份<para>Delete the archive file and all its backups</para></summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="key">数据键名<para>Data key name</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        public UniTask DeleteAsync(int slotId, string key,
            CancellationToken cancellationToken = default)
        {
            string path = GetArchivePath(slotId, key);
            if (File.Exists(path))
                File.Delete(path);

            // 删除 .bak（最新备份）
            string backupPath = path + BackupExtension;
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            // 删除编号备份 .bak.1 ~ .bak.(maxBackupCount-1)
            for (int i = 1; i < _settings.maxBackupCount; i++)
            {
                string numberedBak = $"{path}{BackupExtension}.{i}";
                if (File.Exists(numberedBak))
                    File.Delete(numberedBak);
            }

            return UniTask.CompletedTask;
        }

        /// <summary>删除整个槽位目录<para>Delete the entire slot directory</para></summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        public UniTask DeleteSlotAsync(int slotId, CancellationToken cancellationToken = default)
        {
            string slotDir = GetSlotDirectory(slotId);
            if (Directory.Exists(slotDir))
                Directory.Delete(slotDir, recursive: true);

            return UniTask.CompletedTask;
        }

        /// <summary>列出槽位中所有 .arc 文件名<para>List all .arc filenames in the slot</para></summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>键名数组<para>Array of key names</para></returns>
        public UniTask<string[]> ListKeysAsync(int slotId, CancellationToken cancellationToken = default)
        {
            string slotDir = GetSlotDirectory(slotId);
            if (!Directory.Exists(slotDir))
                return UniTask.FromResult(Array.Empty<string>());

            var files = Directory.GetFiles(slotDir, $"*{ArchiveExtension}");
            var keys = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                keys[i] = Path.GetFileNameWithoutExtension(files[i]);

            return UniTask.FromResult(keys);
        }

        /// <summary>计算槽位中所有存档文件的总大小<para>Calculate total size of all archive files in the slot</para></summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>总字节数<para>Total size in bytes</para></returns>
        public UniTask<long> GetSlotSizeAsync(int slotId, CancellationToken cancellationToken = default)
        {
            string slotDir = GetSlotDirectory(slotId);
            if (!Directory.Exists(slotDir))
                return UniTask.FromResult(0L);

            long totalSize = 0;
            foreach (string file in Directory.GetFiles(slotDir, $"*{ArchiveExtension}"))
                totalSize += new FileInfo(file).Length;

            return UniTask.FromResult(totalSize);
        }

        /// <summary>文件存储无需额外刷新<para>File storage requires no extra flush</para></summary>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        public UniTask FlushAsync(CancellationToken cancellationToken = default)
        {
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 同步刷盘占位（FileStream.WriteThrough + Flush(true) 已在 SaveRawSync / SaveMetaSync 中完成），
        /// <b>退出流程专用</b>。
        /// <para>Synchronous flush no-op. WriteThrough + Flush(true) already done in SaveRawSync/SaveMetaSync.
        /// <b>For shutdown paths only</b>.</para>
        /// </summary>
        public void FlushSync()
        {
            // File 写入已经在 SaveRawSync/SaveMetaSync 中通过 FileOptions.WriteThrough + Flush(true) 落盘
        }

        #endregion

        #region Slot Meta

        /// <summary>
        /// 保存槽位元数据（JSON 明文）<para>Save slot metadata (plain JSON)</para>
        /// </summary>
        /// <param name="meta">槽位元数据<para>Slot metadata</para></param>
        /// <param name="ct">取消令牌<para>Cancellation token</para></param>
        public async UniTask SaveMetaAsync(ArchiveSlotMeta meta, CancellationToken ct = default)
        {
            string slotDir = GetSlotDirectory(meta.slotId);
            if (!Directory.Exists(slotDir))
                Directory.CreateDirectory(slotDir);

            string metaPath = Path.Combine(slotDir, MetaFile);
            string json = JsonUtility.ToJson(meta, prettyPrint: true);
            byte[] data = Encoding.UTF8.GetBytes(json);

            string tempPath = metaPath + ".tmp";
            await WriteAllBytesAsync(tempPath, data, ct);
            AtomicReplace(tempPath, metaPath);
        }

        /// <summary>
        /// 同步保存槽位元数据。<b>退出流程专用</b>，不依赖 PlayerLoop。
        /// <para>Synchronous save metadata. <b>For shutdown paths only</b>.</para>
        /// </summary>
        public void SaveMetaSync(ArchiveSlotMeta meta)
        {
            string slotDir = GetSlotDirectory(meta.slotId);
            if (!Directory.Exists(slotDir))
                Directory.CreateDirectory(slotDir);

            string metaPath = Path.Combine(slotDir, MetaFile);
            string json = JsonUtility.ToJson(meta, prettyPrint: true);
            byte[] data = Encoding.UTF8.GetBytes(json);

            string tempPath = metaPath + ".tmp";
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                       FileShare.None, bufferSize: 4096, options: FileOptions.WriteThrough))
            {
                fs.Write(data, 0, data.Length);
                fs.Flush(true);
            }
            AtomicReplace(tempPath, metaPath);
        }

        /// <summary>
        /// 读取槽位元数据<para>Load slot metadata</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="ct">取消令牌<para>Cancellation token</para></param>
        /// <returns>元数据，不存在则返回 null<para>Metadata, or null if not found</para></returns>
        public async UniTask<ArchiveSlotMeta?> LoadMetaAsync(int slotId, CancellationToken ct = default)
        {
            string metaPath = Path.Combine(GetSlotDirectory(slotId), MetaFile);
            if (!File.Exists(metaPath))
                return null;

            byte[] data = await ReadAllBytesAsync(metaPath, ct);
            string json = Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<ArchiveSlotMeta>(json);
        }

        /// <summary>
        /// 列出所有有效的存档槽位（异步，文件 I/O 放在线程池）。
        /// <para>List all valid archive slots asynchronously.</para>
        /// </summary>
        public async UniTask<ArchiveSlotMeta[]> ListSlotsAsync(CancellationToken ct = default)
        {
            return await UniTask.RunOnThreadPool(ListSlotsSync, cancellationToken: ct);
        }

        /// <summary>
        /// 列出所有有效的存档槽位元数据（同步实现，内部使用）。
        /// <para>List all valid archive slot metadata synchronously (internal).</para>
        /// </summary>
        private ArchiveSlotMeta[] ListSlotsSync()
        {
            if (!Directory.Exists(_rootPath))
                return Array.Empty<ArchiveSlotMeta>();

            var dirs = Directory.GetDirectories(_rootPath, "Slot_*");
            var result = new System.Collections.Generic.List<ArchiveSlotMeta>();

            foreach (string dir in dirs)
            {
                string metaPath = Path.Combine(dir, MetaFile);
                if (!File.Exists(metaPath))
                    continue;

                try
                {
                    string json = File.ReadAllText(metaPath, Encoding.UTF8);
                    var meta = JsonUtility.FromJson<ArchiveSlotMeta>(json);
                    if (meta.isValid)
                        result.Add(meta);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FileArchiveProvider] Failed to read slot meta '{metaPath}': {ex.Message}");
                }
            }

            return result.ToArray();
        }

        #endregion

        #region Internal: Encryption

        // 获取或派生出 AES 密钥（使用 PBKDF2 + 设备 ID + 盐值，缓存复用，线程安全）
        private byte[] GetOrCreateKey()
        {
            if (_cachedKey != null)
                return _cachedKey;

            lock (_keyLock)
            {
                if (_cachedKey != null)
                    return _cachedKey;

                // 防御：SystemInfo.deviceUniqueIdentifier 在某些平台（编辑器/容器）可能为空。
                // 退回到稳定标识 + 盐值，保证密钥派生至少有非空输入。
                string deviceId = SystemInfo.deviceUniqueIdentifier;
                if (string.IsNullOrEmpty(deviceId))
                {
                    deviceId = "EF.Archive.EmptyDeviceId";
                    Debug.LogWarning("[FileArchiveProvider] SystemInfo.deviceUniqueIdentifier is empty. " +
                                     "Falling back to a fixed placeholder for key derivation. " +
                                     "Archives written on this device may be readable on other devices with the same salt.");
                }

                using var derive = new Rfc2898DeriveBytes(
                    Encoding.UTF8.GetBytes(deviceId),
                    _salt,
                    _settings.pbkdf2Iterations,
                    HashAlgorithmName.SHA256);

                _cachedKey = derive.GetBytes((int)_settings.aesKeySize / 8);
                return _cachedKey;
            }
        }

        // AES-256-CBC 加密，返回 IV + 密文（复用缓存的 Aes 实例）
        private byte[] Encrypt(byte[] plaintext)
        {
            byte[] key = GetOrCreateKey();

            lock (_cachedAes)
            {
                _cachedAes.KeySize = (int)_settings.aesKeySize;
                _cachedAes.Key = key;
                _cachedAes.Mode = CipherMode.CBC;
                _cachedAes.Padding = PaddingMode.PKCS7;
                _cachedAes.GenerateIV();

                using var encryptor = _cachedAes.CreateEncryptor();
                byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

                byte[] result = new byte[_cachedAes.IV.Length + ciphertext.Length];
                Buffer.BlockCopy(_cachedAes.IV, 0, result, 0, _cachedAes.IV.Length);
                Buffer.BlockCopy(ciphertext, 0, result, _cachedAes.IV.Length, ciphertext.Length);

                return result;
            }
        }

        #endregion

        #region Internal: Decryption & Verification

        // 解析文件头 → 验证 SHA256 → 解密，失败时抛出 ArchiveCorruptedException
        private byte[] DecryptAndVerify(byte[] fileData, string filePath)
        {
            if (fileData.Length < 56)
                throw new ArchiveCorruptedException(filePath, "File too small to contain valid header.");

            uint magic = BitConverter.ToUInt32(fileData, 0);
            if (magic != FileMagic)
                throw new ArchiveCorruptedException(filePath, $"Invalid magic number: 0x{magic:X8}");

            int dataLen = BitConverter.ToInt32(fileData, 20);
            if (dataLen <= 0 || 20 + 4 + dataLen + 32 > fileData.Length)
                throw new ArchiveCorruptedException(filePath, $"Invalid data length: {dataLen}");

            byte[] iv = new byte[16];
            Buffer.BlockCopy(fileData, 4, iv, 0, 16);

            byte[] cipherOnly = new byte[dataLen];
            Buffer.BlockCopy(fileData, 24, cipherOnly, 0, dataLen);

            byte[] expectedChecksum = new byte[32];
            Buffer.BlockCopy(fileData, 24 + dataLen, expectedChecksum, 0, 32);

            // 校验和包含 IV，与写入时一致
            byte[] ivPlusCipher = new byte[16 + dataLen];
            Buffer.BlockCopy(iv, 0, ivPlusCipher, 0, 16);
            Buffer.BlockCopy(cipherOnly, 0, ivPlusCipher, 16, dataLen);

            byte[] actualChecksum = ComputeChecksum(ivPlusCipher);
            if (!ConstantTimeEquals(actualChecksum, expectedChecksum))
                throw new ArchiveCorruptedException(filePath, "SHA256 checksum mismatch. File may be corrupted or tampered.");

            return Decrypt(cipherOnly, iv);
        }

        // AES-256-CBC 解密（复用缓存的 Aes 实例）
        private byte[] Decrypt(byte[] ciphertext, byte[] iv)
        {
            byte[] key = GetOrCreateKey();

            lock (_cachedAes)
            {
                _cachedAes.KeySize = (int)_settings.aesKeySize;
                _cachedAes.Key = key;
                _cachedAes.Mode = CipherMode.CBC;
                _cachedAes.Padding = PaddingMode.PKCS7;
                _cachedAes.IV = iv;

                using var decryptor = _cachedAes.CreateDecryptor();
                return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            }
        }

        #endregion

        #region Internal: File Format

        // 封装文件：[MAGIC 4B][IV 16B][cipherLen 4B][密文][SHA256 32B]
        private byte[] PackFile(byte[] encrypted, byte[] checksum)
        {
            byte[] iv = new byte[16];
            Buffer.BlockCopy(encrypted, 0, iv, 0, 16);

            int cipherLen = encrypted.Length - 16;
            byte[] cipherOnly = new byte[cipherLen];
            Buffer.BlockCopy(encrypted, 16, cipherOnly, 0, cipherLen);

            byte[] result = new byte[4 + 16 + 4 + cipherLen + 32];
            Buffer.BlockCopy(BitConverter.GetBytes(FileMagic), 0, result, 0, 4);
            Buffer.BlockCopy(iv, 0, result, 4, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(cipherLen), 0, result, 20, 4);
            Buffer.BlockCopy(cipherOnly, 0, result, 24, cipherLen);
            Buffer.BlockCopy(checksum, 0, result, 24 + cipherLen, 32);

            return result;
        }

        #endregion

        #region Internal: Checksum

        // SHA256 哈希
        private byte[] ComputeChecksum(byte[] data)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(data);
        }

        // 常量时间比较（防时序攻击）
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];

            return diff == 0;
        }

        #endregion

        #region Internal: File I/O

        // 异步写入全部字节到指定路径
        private async UniTask WriteAllBytesAsync(string path, byte[] data, CancellationToken ct)
        {
            await UniTask.RunOnThreadPool(() =>
            {
                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write,
                    FileShare.None, bufferSize: 4096, useAsync: true);
                fs.Write(data, 0, data.Length);
                fs.Flush();
            }, cancellationToken: ct);
        }

        // 异步读取指定路径的全部字节（循环读取，防止部分读取）
        private async UniTask<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.Read, bufferSize: 4096, useAsync: true);
                byte[] result = new byte[fs.Length];
                int offset = 0;
                int remaining = result.Length;
                while (remaining > 0)
                {
                    int read = fs.Read(result, offset, remaining);
                    if (read == 0) throw new EndOfStreamException($"Unexpected end of file: {path}");
                    offset += read;
                    remaining -= read;
                }
                return result;
            }, cancellationToken: ct);
        }

        #endregion

        #region Internal: Path Helpers

        // 获取槽位目录路径
        private string GetSlotDirectory(int slotId)
            => Path.Combine(_rootPath, $"Slot_{slotId}");

        // 确保指定槽位的存储目录存在
        private void EnsureSlotDirectory(int slotId)
        {
            string dir = GetSlotDirectory(slotId);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        // 获取存档文件的完整路径（自动处理非法字符）
        private string GetArchivePath(int slotId, string key)
        {
            string safeKey = SanitizeKey(key);
            return Path.Combine(GetSlotDirectory(slotId), safeKey + ArchiveExtension);
        }

        // Windows 保留文件名（不区分大小写）
        private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        // 过滤 key 中的非法文件名字符、控制字符，并防御 Windows 保留名
        private static string SanitizeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return "_empty";

            var sb = new StringBuilder(key.Length);
            foreach (char c in key)
            {
                if (c < 0x20 || c == 0x7F)
                    sb.Append('_'); // 控制字符（含 DEL）
                else
                    sb.Append(c switch
                    {
                        '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|' => '_',
                        _ => c
                    });
            }

            string sanitized = sb.ToString().TrimEnd('.', ' ');

            if (sanitized.Length == 0)
                return "_empty";

            // 仅由 . 和 _ 组成、或命中了 Windows 保留名 → 加前缀
            if (IsAllReplacementChars(sanitized) || ReservedNames.Contains(sanitized))
                return "_" + sanitized;

            return sanitized;
        }

        private static bool IsAllReplacementChars(string s)
        {
            foreach (char c in s)
                if (c != '.' && c != '_') return false;
            return true;
        }

        // 轮转备份链：.bak → .bak.1 → .bak.2 → ... → 删除最旧的 .bak.N。
        // 在 SaveRawAsync 创建新的 .bak 之前调用，确保旧 .bak 被正确移位而非丢失。
        // <para>Rotate backup chain so that at most maxBackupCount backups are kept,
        // including the latest .bak.</para>
        private void CleanupOldBackups(int slotId, string key)
        {
            if (_settings.maxBackupCount <= 0) return;

            string basePath = GetArchivePath(slotId, key);

            // maxBackupCount == 1: 只保留 .bak，删除所有编号备份
            if (_settings.maxBackupCount == 1)
            {
                string bakPath1 = $"{basePath}{BackupExtension}.1";
                if (File.Exists(bakPath1))
                    File.Delete(bakPath1);
                return; // .bak 将由 SaveRawAsync 的 File.Copy 直接覆盖
            }

            // 通用情况：保留 maxBackupCount 个备份（.bak + .bak.1 ~ .bak.(maxBackupCount-1)）
            int maxIndex = _settings.maxBackupCount - 1;

            // ① 删除最旧的编号备份（溢出）
            string overflowPath = $"{basePath}{BackupExtension}.{maxIndex}";
            if (File.Exists(overflowPath))
                File.Delete(overflowPath);

            // ② 将每个编号备份向上移位：.bak.(N) → .bak.(N+1)
            for (int i = maxIndex - 1; i >= 1; i--)
            {
                string srcPath = $"{basePath}{BackupExtension}.{i}";
                if (File.Exists(srcPath))
                    File.Move(srcPath, $"{basePath}{BackupExtension}.{i + 1}");
            }

            // ③ .bak → .bak.1（为即将创建的新 .bak 腾出位置）
            string bakPath = basePath + BackupExtension;
            if (File.Exists(bakPath))
                File.Move(bakPath, $"{basePath}{BackupExtension}.1");
        }

        // 原子替换文件（NTFS 级原子：File.Replace 保留文件身份不变，失败时回退 Delete+Move）
        private static void AtomicReplace(string sourcePath, string destPath)
        {
            if (File.Exists(destPath))
                File.Replace(sourcePath, destPath, null);
            else
                File.Move(sourcePath, destPath);
        }

        #endregion

        /// <summary>文件存储与加密资源清理<para>Release file storage and encryption resources</para></summary>
        public void Dispose()
        {
            _cachedAes?.Dispose();
        }
    }
}
