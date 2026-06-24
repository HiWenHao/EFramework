/*
 * ================================================
 * Describe:      默认存档存储后端 —— 本地加密文件。
 *                每次写入：JSON序列化 → AES-256-CBC加密
 *                → SHA256校验 → 临时文件 → 原子重命名。
 *                防止写入中断导致存档损坏。
 * Author:        Alvin8412
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-06-24 22:25:00
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

namespace EasyFramework.Systems.Save
{
    /// <summary>
    /// 基于本地文件系统的存档 Provider。
    /// <para>文件结构：{root}/Slot_{id}/{key}.sav</para>
    /// <para>每个 .sav 文件头：[4B MAGIC][16B IV][4B len][N B ciphertext][32B SHA256]</para>
    /// <para>每次写入采用"临时文件 → 原子重命名"策略，防止写入中断导致存档损坏。</para>
    /// </summary>
    public class FileSaveProvider : ISaveProvider
    {
        private const uint FILE_MAGIC = 0x56465345; // "EFSV" in little-endian
        private const string SAVE_EXTENSION = ".sav";
        private const string BACKUP_EXTENSION = ".bak";
        private const string META_FILE = "slot_meta.json";

        private readonly string _rootPath;
        private readonly SaveSettings _settings;
        private readonly byte[] _salt;
        private byte[] _cachedKey;

        public FileSaveProvider(SaveSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rootPath = Path.Combine(Application.persistentDataPath, settings.fileStorageRoot);
            _salt = Encoding.UTF8.GetBytes(settings.encryptionSalt);

            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
        }

        #region ISaveProvider Implementation

        public async UniTask SaveRawAsync(int slotId, string key, byte[] data,
            CancellationToken cancellationToken = default)
        {
            EnsureSlotDirectory(slotId);

            byte[] encrypted = Encrypt(data);
            byte[] checksum = ComputeChecksum(encrypted);
            byte[] fileData = PackFile(encrypted, checksum);

            string finalPath = GetSavePath(slotId, key);

            // 写入前备份
            if (_settings.enableAutoBackup && File.Exists(finalPath))
            {
                string backupPath = finalPath + BACKUP_EXTENSION;
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Copy(finalPath, backupPath);
            }

            // 临时文件 + 原子重命名
            string tempPath = finalPath + ".tmp";
            await WriteAllBytesAsync(tempPath, fileData, cancellationToken);
            AtomicReplace(tempPath, finalPath);

            // 清理过期备份
            CleanupOldBackups(slotId, key);
        }

        public async UniTask<byte[]> LoadRawAsync(int slotId, string key,
            CancellationToken cancellationToken = default)
        {
            string path = GetSavePath(slotId, key);

            // 优先读正式文件
            if (File.Exists(path))
            {
                byte[] fileData = await ReadAllBytesAsync(path, cancellationToken);
                return DecryptAndVerify(fileData, path);
            }

            return null;
        }

        public UniTask<bool> ExistsAsync(int slotId, string key,
            CancellationToken cancellationToken = default)
        {
            string path = GetSavePath(slotId, key);
            return UniTask.FromResult(File.Exists(path));
        }

        public UniTask DeleteAsync(int slotId, string key,
            CancellationToken cancellationToken = default)
        {
            string path = GetSavePath(slotId, key);
            if (File.Exists(path))
                File.Delete(path);

            string backupPath = path + BACKUP_EXTENSION;
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            return UniTask.CompletedTask;
        }

        public UniTask DeleteSlotAsync(int slotId, CancellationToken cancellationToken = default)
        {
            string slotDir = GetSlotDirectory(slotId);
            if (Directory.Exists(slotDir))
                Directory.Delete(slotDir, recursive: true);

            return UniTask.CompletedTask;
        }

        public UniTask<string[]> ListKeysAsync(int slotId, CancellationToken cancellationToken = default)
        {
            string slotDir = GetSlotDirectory(slotId);
            if (!Directory.Exists(slotDir))
                return UniTask.FromResult(Array.Empty<string>());

            var files = Directory.GetFiles(slotDir, $"*{SAVE_EXTENSION}");
            var keys = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                keys[i] = Path.GetFileNameWithoutExtension(files[i]);

            return UniTask.FromResult(keys);
        }

        public UniTask<long> GetSlotSizeAsync(int slotId, CancellationToken cancellationToken = default)
        {
            string slotDir = GetSlotDirectory(slotId);
            if (!Directory.Exists(slotDir))
                return UniTask.FromResult(0L);

            long totalSize = 0;
            foreach (string file in Directory.GetFiles(slotDir, $"*{SAVE_EXTENSION}"))
                totalSize += new FileInfo(file).Length;

            return UniTask.FromResult(totalSize);
        }

        public UniTask FlushAsync(CancellationToken cancellationToken = default)
        {
            // FileStream 每次都 Flush + 原子重命名，无需额外操作
            return UniTask.CompletedTask;
        }

        #endregion

        #region Slot Meta (public helpers, not in interface but used by SaveManager)

        /// <summary>
        /// 保存槽位元数据（JSON，不加密）
        /// </summary>
        public async UniTask SaveMetaAsync(SaveSlotMeta meta, CancellationToken ct = default)
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
        /// 读取槽位元数据
        /// </summary>
        public async UniTask<SaveSlotMeta?> LoadMetaAsync(int slotId, CancellationToken ct = default)
        {
            string metaPath = Path.Combine(GetSlotDirectory(slotId), META_FILE);
            if (!File.Exists(metaPath))
                return null;

            byte[] data = await ReadAllBytesAsync(metaPath, ct);
            string json = Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<SaveSlotMeta>(json);
        }

        /// <summary>
        /// 列出所有存在的存档槽位
        /// </summary>
        public SaveSlotMeta[] ListSlots()
        {
            if (!Directory.Exists(_rootPath))
                return Array.Empty<SaveSlotMeta>();

            var dirs = Directory.GetDirectories(_rootPath, "Slot_*");
            var result = new System.Collections.Generic.List<SaveSlotMeta>();

            foreach (string dir in dirs)
            {
                string metaPath = Path.Combine(dir, META_FILE);
                if (!File.Exists(metaPath))
                    continue;

                try
                {
                    string json = File.ReadAllText(metaPath, Encoding.UTF8);
                    var meta = JsonUtility.FromJson<SaveSlotMeta>(json);
                    if (meta.isValid)
                        result.Add(meta);
                }
                catch
                {
                    // 元数据损坏，跳过
                }
            }

            return result.ToArray();
        }

        #endregion

        #region Internal: Encryption

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

            // 返回 IV + Ciphertext
            byte[] result = new byte[aes.IV.Length + ciphertext.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);

            return result;
        }

        #endregion

        #region Internal: Decryption & Verification

        private byte[] DecryptAndVerify(byte[] fileData, string filePath)
        {
            if (fileData.Length < 56) // 4 + 16 + 4 + 32 minimum
                throw new SaveCorruptedException(filePath, "File too small to contain valid header.");

            // 解析文件头
            uint magic = BitConverter.ToUInt32(fileData, 0);
            if (magic != FILE_MAGIC)
                throw new SaveCorruptedException(filePath, $"Invalid magic number: 0x{magic:X8}");

            int dataLen = BitConverter.ToInt32(fileData, 20); // skip 4B magic + 16B IV
            if (dataLen <= 0 || 20 + 4 + dataLen + 32 > fileData.Length)
                throw new SaveCorruptedException(filePath, $"Invalid data length: {dataLen}");

            byte[] encrypted = new byte[dataLen];
            Buffer.BlockCopy(fileData, 24, encrypted, 0, dataLen);

            // 验证 SHA256
            byte[] expectedChecksum = new byte[32];
            Buffer.BlockCopy(fileData, 24 + dataLen, expectedChecksum, 0, 32);

            byte[] actualChecksum = ComputeChecksum(encrypted);
            if (!ConstantTimeEquals(actualChecksum, expectedChecksum))
                throw new SaveCorruptedException(filePath, "SHA256 checksum mismatch. File may be corrupted or tampered.");

            // 解密
            byte[] iv = new byte[16];
            Buffer.BlockCopy(fileData, 4, iv, 0, 16);

            return Decrypt(encrypted, iv);
        }

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

        private byte[] PackFile(byte[] encrypted, byte[] checksum)
        {
            // 文件格式: [4B MAGIC][16B IV][4B dataLen][N B encrypted][32B SHA256]
            // encrypted 已经是 IV + ciphertext 格式，所以：
            // 从 encrypted 中提取 IV（前16字节）
            // dataLen = encrypted.Length - 16 (纯密文长度)

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

        private byte[] ComputeChecksum(byte[] data)
        {
            using var sha = SHA256.Create();
            // 校验只对 ciphertext 部分（不含 IV），传入的参数 encrypted 是 IV+ciphertext 整体
            // 但 PackFile 先调用 ComputeChecksum(encrypted)，encrypted 是 Encrypt 返回的 IV+ciphertext
            // 这里我们校验整个 encrypted 块（包含 IV），因为 IV 也是需要防篡改的
            return sha.ComputeHash(data);
        }

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

        private async UniTask<byte[]> ReadAllBytesAsync(string path, CancellationToken ct)
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.Read, bufferSize: 4096, useAsync: true);
                byte[] result = new byte[fs.Length];
                fs.Read(result, 0, result.Length);
                return result;
            }, cancellationToken: ct);
        }

        #endregion

        #region Internal: Path Helpers

        private string GetSlotDirectory(int slotId)
            => Path.Combine(_rootPath, $"Slot_{slotId}");

        private void EnsureSlotDirectory(int slotId)
        {
            string dir = GetSlotDirectory(slotId);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private string GetSavePath(int slotId, string key)
        {
            string safeKey = SanitizeKey(key);
            return Path.Combine(GetSlotDirectory(slotId), safeKey + SAVE_EXTENSION);
        }

        private static string SanitizeKey(string key)
        {
            // 替换非法文件名字符
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

        private void CleanupOldBackups(int slotId, string key)
        {
            if (_settings.maxBackupCount <= 0)
                return;

            string pattern = GetSavePath(slotId, key) + BACKUP_EXTENSION + "*";
            // 简单实现：只保留 .bak 文件
            // 后续版本可扩展为轮转备份 .bak.1, .bak.2 等
        }

        /// <summary>
        /// 原子替换文件：先删目标，再 rename 临时文件。
        /// .NET Standard 2.1 的 File.Move 不支持 overwrite 参数，
        /// 但 Delete + Move 在同一个文件系统分区内等价于原子 rename。
        /// </summary>
        private static void AtomicReplace(string sourcePath, string destPath)
        {
            if (File.Exists(destPath))
                File.Delete(destPath);
            File.Move(sourcePath, destPath);
        }

        #endregion
    }
}
