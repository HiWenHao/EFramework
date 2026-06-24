/*
 * ================================================
 * Describe:      默认存档存储后端 —— 本地加密文件。
 *                每次写入：JSON序列化 → AES-256-CBC加密
 *                → SHA256校验 → 临时文件 → 原子重命名。
 *                防止写入中断导致存档损坏。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 01:34:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
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
        private const uint FILE_MAGIC = 0x43524145; // "EARC" — 文件魔数标识
        private const string ARCHIVE_EXTENSION = ".arc";    // 存档文件扩展名
        private const string BACKUP_EXTENSION = ".bak";     // 备份文件扩展名
        private const string META_FILE = "slot_meta.json";  // 槽位元数据文件名

        private readonly string _rootPath;       // 存档根目录
        private readonly ArchiveSettings _settings; // 全局配置引用
        private readonly byte[] _salt;           // 加密盐值
        private byte[] _cachedKey;               // 缓存的 AES 密钥

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

            if (_settings.enableAutoBackup && File.Exists(finalPath))
            {
                string backupPath = finalPath + BACKUP_EXTENSION;
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Copy(finalPath, backupPath);
            }

            string tempPath = finalPath + ".tmp";
            await WriteAllBytesAsync(tempPath, fileData, cancellationToken);
            AtomicReplace(tempPath, finalPath);

            CleanupOldBackups(slotId, key);
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

        /// <summary>删除存档文件<para>Delete the archive file</para></summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="key">数据键名<para>Data key name</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        public UniTask DeleteAsync(int slotId, string key,
            CancellationToken cancellationToken = default)
        {
            string path = GetArchivePath(slotId, key);
            if (File.Exists(path))
                File.Delete(path);

            string backupPath = path + BACKUP_EXTENSION;
            if (File.Exists(backupPath))
                File.Delete(backupPath);

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

            var files = Directory.GetFiles(slotDir, $"*{ARCHIVE_EXTENSION}");
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
            foreach (string file in Directory.GetFiles(slotDir, $"*{ARCHIVE_EXTENSION}"))
                totalSize += new FileInfo(file).Length;

            return UniTask.FromResult(totalSize);
        }

        /// <summary>文件存储无需额外刷新<para>File storage requires no extra flush</para></summary>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        public UniTask FlushAsync(CancellationToken cancellationToken = default)
        {
            return UniTask.CompletedTask;
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

            string metaPath = Path.Combine(slotDir, META_FILE);
            string json = JsonUtility.ToJson(meta, prettyPrint: true);
            byte[] data = Encoding.UTF8.GetBytes(json);

            string tempPath = metaPath + ".tmp";
            await WriteAllBytesAsync(tempPath, data, ct);
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
            string metaPath = Path.Combine(GetSlotDirectory(slotId), META_FILE);
            if (!File.Exists(metaPath))
                return null;

            byte[] data = await ReadAllBytesAsync(metaPath, ct);
            string json = Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<ArchiveSlotMeta>(json);
        }

        /// <summary>
        /// 列出所有有效的存档槽位<para>List all valid archive slots</para>
        /// </summary>
        public ArchiveSlotMeta[] ListSlots()
        {
            if (!Directory.Exists(_rootPath))
                return Array.Empty<ArchiveSlotMeta>();

            var dirs = Directory.GetDirectories(_rootPath, "Slot_*");
            var result = new System.Collections.Generic.List<ArchiveSlotMeta>();

            foreach (string dir in dirs)
            {
                string metaPath = Path.Combine(dir, META_FILE);
                if (!File.Exists(metaPath))
                    continue;

                try
                {
                    string json = File.ReadAllText(metaPath, Encoding.UTF8);
                    var meta = JsonUtility.FromJson<ArchiveSlotMeta>(json);
                    if (meta.isValid)
                        result.Add(meta);
                }
                catch
                {
                }
            }

            return result.ToArray();
        }

        #endregion

        #region Internal: Encryption

        // 获取或派生出 AES 密钥（使用 PBKDF2 + 设备 ID + 盐值，缓存复用）
        private byte[] GetOrCreateKey()
        {
            if (_cachedKey != null)
                return _cachedKey;

            using var derive = new Rfc2898DeriveBytes(
                Encoding.UTF8.GetBytes(SystemInfo.deviceUniqueIdentifier),
                _salt,
                _settings.pbkdf2Iterations,
                HashAlgorithmName.SHA256);

            _cachedKey = derive.GetBytes(_settings.aesKeySize / 8);
            return _cachedKey;
        }

        // AES-256-CBC 加密，返回 IV + 密文
        private byte[] Encrypt(byte[] plaintext)
        {
            byte[] key = GetOrCreateKey();

            using var aes = Aes.Create();
            aes.KeySize = _settings.aesKeySize;
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

            byte[] result = new byte[aes.IV.Length + ciphertext.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);

            return result;
        }

        #endregion

        #region Internal: Decryption & Verification

        // 解析文件头 → 验证 SHA256 → 解密，失败时抛出 ArchiveCorruptedException
        private byte[] DecryptAndVerify(byte[] fileData, string filePath)
        {
            if (fileData.Length < 56)
                throw new ArchiveCorruptedException(filePath, "File too small to contain valid header.");

            uint magic = BitConverter.ToUInt32(fileData, 0);
            if (magic != FILE_MAGIC)
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

        // AES-256-CBC 解密
        private byte[] Decrypt(byte[] ciphertext, byte[] iv)
        {
            byte[] key = GetOrCreateKey();

            using var aes = Aes.Create();
            aes.KeySize = _settings.aesKeySize;
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
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
            Buffer.BlockCopy(BitConverter.GetBytes(FILE_MAGIC), 0, result, 0, 4);
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
            return Path.Combine(GetSlotDirectory(slotId), safeKey + ARCHIVE_EXTENSION);
        }

        // 过滤 key 中的非法文件名字符
        private static string SanitizeKey(string key)
        {
            var sb = new StringBuilder(key.Length);
            foreach (char c in key)
            {
                sb.Append(c switch
                {
                    '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|' => '_',
                    _ => c
                });
            }
            return sb.ToString();
        }

        // 清理过期的备份文件（轮转删除最旧的 .bak.N）
        private void CleanupOldBackups(int slotId, string key)
        {
            if (_settings.maxBackupCount <= 0) return;

            string basePath = GetArchivePath(slotId, key);

            // 删除超出上限的旧备份：.bak → .bak.1 → .bak.2 → ...
            for (int i = _settings.maxBackupCount - 1; i >= 0; i--)
            {
                string path = i == 0 ? basePath + BACKUP_EXTENSION : $"{basePath}{BACKUP_EXTENSION}.{i}";
                if (File.Exists(path))
                {
                    if (i >= _settings.maxBackupCount - 1)
                        File.Delete(path);
                    else
                        File.Move(path, $"{basePath}{BACKUP_EXTENSION}.{i + 1}");
                }
            }
        }

        // 原子替换文件（先删目标 → 再 Move，同分区内等价于原子 rename）
        private static void AtomicReplace(string sourcePath, string destPath)
        {
            if (File.Exists(destPath))
                File.Delete(destPath);
            File.Move(sourcePath, destPath);
        }

        #endregion
    }
}
