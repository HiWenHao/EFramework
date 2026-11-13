/*
 * ================================================
 * Describe:        键值存储契约：以字符串为通道的同步读写
 * Author:          Alvin8412
 * CreationTime:    2026-11-03 13:20:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 13:20:00
 * ScriptVersion:   0.1
 * ===============================================
 */

namespace EasyFramework.Preference
{
    /// <summary>
    /// 键值存储接口：全部方法同步、以字符串为值通道、序列化由上层负责
    /// <para>Key-value storage interface: fully synchronous, string-typed values, serialization is the caller's duty</para>
    /// </summary>
    public interface IKeyValueStorage
    {
        #region 方法 / Methods

        /// <summary>
        /// 判断指定键是否存在
        /// <para>Returns whether the given key exists</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <returns>键是否存在<para>Whether the key exists</para></returns>
        bool ExistKey(string key);

        /// <summary>
        /// 读取指定键的字符串值
        /// <para>Reads the string value of the given key</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <param name="fallback">键不存在时的回退值<para>The fallback when the key is missing</para></param>
        /// <returns>存储的字符串值<para>The stored string value</para></returns>
        string GetString(string key, string fallback = null);

        /// <summary>
        /// 写入指定键的字符串值
        /// <para>Writes the string value of the given key</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <param name="value">要写入的字符串值<para>The string value to write</para></param>
        void SetString(string key, string value);

        /// <summary>
        /// 删除指定键
        /// <para>Deletes the given key</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        void DeleteKey(string key);

        /// <summary>
        /// 将未落盘的变更立即持久化，退出前必须调用
        /// <para>Persists pending changes immediately; must be called before quit</para>
        /// </summary>
        void Flush();

        #endregion
    }
}