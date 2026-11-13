/*
 * ================================================
 * Describe:        偏好加密器契约：变换写入存储的字符串，默认框架不加密
 * Author:          Alvin8412
 * CreationTime:    2026-11-03 15:05:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 15:05:00
 * ScriptVersion:   0.1
 * ===============================================
 */

namespace EasyFramework.Preference
{
    /// <summary>
    /// 偏好加密器契约：对写入存储的字符串做双向变换，默认框架不加密；客户端加密仅提高篡改门槛，不构成安全边界
    /// <para>Preference crypto contract: bidirectional transform for strings written to storage; encryption is off by default. Client-side crypto only raises the tamper bar, it is not a security boundary</para>
    /// </summary>
    public interface IPreferenceCrypto
    {
        #region 方法 / Methods

        /// <summary>
        /// 加密：写入存储前调用
        /// <para>Encrypts the plain string before it is written to storage</para>
        /// </summary>
        /// <param name="plain">明文字符串<para>The plain string</para></param>
        /// <returns>密文字符串<para>The cipher string</para></returns>
        string Encrypt(string plain);

        /// <summary>
        /// 解密：从存储读出后调用，失败时返回 null 由管理器回退默认值
        /// <para>Decrypts the cipher string after it is read from storage; return null on failure so the manager falls back</para>
        /// </summary>
        /// <param name="cipher">密文字符串<para>The cipher string</para></param>
        /// <returns>明文字符串<para>The plain string</para></returns>
        string Decrypt(string cipher);

        #endregion
    }
}