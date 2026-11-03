/*
 * ================================================
 * Describe:        简单加密器：多字节 XOR 混淆 + Base64 编码，防止明文直接可见
 * Author:          Alvin8412
 * CreationTime:    2026-11-03 16:35:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 17:30:00
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using Newtonsoft.Json;

namespace EasyFramework.Preference
{
    /// <summary>
    /// 简单加密器：字符串逐字节 XOR 混淆后经 Base64 编码，仅防明文直接可见，非安全实现；安全需求请自行实现 <see cref="IPreferenceCrypto"/> 接入 AES 等算法
    /// <para>Simple crypto: per-byte XOR obfuscation with Base64 encoding, hides plain text only, NOT secure; implement <see cref="IPreferenceCrypto"/> with AES for real security</para>
    /// </summary>
    public sealed class XorPreferenceCrypto : IPreferenceCrypto
    {
        #region Fields

        // 异或密钥字节序列，循环使用
        private readonly byte[] _keys;

        #endregion

        #region Methods

        /// <summary>
        /// 创建简单加密器，密钥为 UTF8 字节序列循环使用
        /// <para>Creates the crypto with the UTF8 bytes of the key used cyclically</para>
        /// </summary>
        /// <param name="key">任意非空字符串密钥<para>Any non-empty key string</para></param>
        public XorPreferenceCrypto(string key)
        {
            _keys = string.IsNullOrEmpty(key)
                ? new byte[] { 0x5A }
                : System.Text.Encoding.UTF8.GetBytes(key);
        }

        /// <summary>
        /// 加密：UTF8 字节逐位异或后经 Base64 编码，再包装为 JSON 字符串值输出
        /// <para>Encrypts via per-byte XOR on UTF8 bytes then Base64</para>
        /// </summary>
        /// <param name="plain">明文字符串<para>The plain string</para></param>
        /// <returns>密文字符串<para>The cipher string</para></returns>
        public string Encrypt(string plain) =>
            JsonConvert.SerializeObject(ToBase64(Xor(System.Text.Encoding.UTF8.GetBytes(plain))));

        /// <summary>
        /// 解密：先还原 JSON 字符串值，再 Base64 解码后逐位异或
        /// <para>Decrypts via Base64 decode then per-byte XOR back to UTF8</para>
        /// </summary>
        /// <param name="cipher">密文字符串<para>The cipher string</para></param>
        /// <returns>明文字符串<para>The plain string</para></returns>
        public string Decrypt(string cipher) =>
            System.Text.Encoding.UTF8.GetString(Xor(Convert.FromBase64String(JsonConvert.DeserializeObject<string>(cipher))));

        // 异或密钥字节序列
        private byte[] Xor(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] ^= _keys[i % _keys.Length];
            return bytes;
        }

        // 字节转 Base64 字符串
        private static string ToBase64(byte[] bytes) => Convert.ToBase64String(bytes);

        #endregion
    }
}
