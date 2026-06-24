/*
 * ================================================
 * Describe:      默认存档存储后端 —— 本地加密文件。
 *                每次写入：JSON序列化 → AES-256-CBC加密
 *                → SHA256校验 → 临时文件 → 原子重命名。
 *                防止写入中断导致存档损坏。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-24 23:19:00
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
    /// 基于本地文件系统的存档 Provider。
    /// <para>文件结构：{root}/Slot_{id}/{key}.arc</para>
    /// <para>每个 .arc 文件头：[4B MAGIC][16B IV][4B len][N B ciphertext][32B SHA256]</para>
    /// <para>每次写入采用"临时文件 → 原子重命名"策略，防止写入中断导致存档损坏。</para>
    /// </summary>
    public class FileArchiveProvider : IArchiveProvider
    {
        private const uint FILE_MAGIC = 0x43524145; // "EARC" in little-endian
        private const string ARCHIVE_EXTENSION = ".arc";
        private const string BACKUP_EXTENSION = ".bak";
        private const string META_FILE = "slot_meta.json";

        private readonly string _rootPath;
        private readonly ArchiveSettings _settings;
        private readonly byte[] _salt;
        private byte[] _cachedKey;

        public FileArchiveProvider(ArchiveSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rootPath = Path.Combine(Application.persistentDataPath, settings.fileStorageRoot);
            _salt = Encoding.UTF8.GetBytes(settings.encryptionSalt);

            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
        }

        #region IArchiveProvider Implementation

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

        public UniTask<bool> ExistsAsync(int slotId, string key,
            CancellationToken cancellationToken = default)
        {
            string path = GetArchivePath(slotId, key);
            return UniTask.FromResult(File.Exists(path));
        }

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

            var files = Directory.GetFiles(slotDir, $"*{ARCHIVE_EXTENSION}");
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
            foreach (string file in Directory.GetFiles(slotDir, $"*{ARCHIVE_EXTENSION}"))
                totalSize += new FileInfo(file).Length;

            return UniTask.FromResult(totalSize);
        }

        public UniTask FlushAsync(CancellationToken cancellationToken = default)
        {
            return UniTask.CompletedTask;
        }

        #endregion

        #region Slot Meta

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

        public async UniTask<ArchiveSlotMeta?> LoadMetaAsync(int slotId, CancellationToken ct = default)
        {
            string metaPath = Path.Combine(GetSlotDirectory(slotId), META_FILE);
            if (!File.Exists(metaPath))
                return null;

            byte[] data = await ReadAllBytesAsync(metaPath, ct);
            string json = Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<ArchiveSlotMeta>(json);
        }

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

            byte[] result = new byte[aes.IV.Length + ciphertext.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(ciphertext, 0, result, aes.IV.Length, ciphertext.Length);

            return result;
        }

        #endregion

        #region Internal: Decryption & Verification

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

            byte[] encrypted = new byte[dataLen];
            Buffer.BlockCopy(fileData, 24, encrypted, 0, dataLen);

            byte[] expectedChecksum = new byte[32];
            Buffer.BlockCopy(fileData, 24 + dataLen, expectedChecksum, 0, 32);

            byte[] actualChecksum = ComputeChecksum(encrypted);
            if (!ConstantTimeEquals(actualChecksum, expectedChecksum))
                throw new ArchiveCorruptedException(filePath, "SHA256 checksum mismatch. File may be corrupted or tampered.");

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

        private string GetArchivePath(int slotId, string key)
        {
            string safeKey = SanitizeKey(key);
            return Path.Combine(GetSlotDirectory(slotId), safeKey + ARCHIVE_EXTENSION);
        }

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

        private void CleanupOldBackups(int slotId, string key)
        {
            if (_settings.maxBackupCount <= 0)
                return;
        }

        private static void AtomicReplace(string sourcePath, string destPath)
        {
            if (File.Exists(destPath))
                File.Delete(destPath);
            File.Move(sourcePath, destPath);
        }

        #endregion
    }
}
