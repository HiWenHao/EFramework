/*
 * ================================================
 * Describe:        基于 PlayerPrefs 的键值存储实现
 * Author:          Alvin8412
 * CreationTime:    2026-11-03 13:20:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 13:20:00
 * ScriptVersion:   0.1
 * ===============================================
 */

using UnityEngine;

namespace EasyFramework.Preference
{
    /// <summary>
    /// PlayerPrefs 实现：平台托管存储，默认零配置
    /// <para>PlayerPrefs-backed storage: platform-managed, works out of the box</para>
    /// </summary>
    public sealed class PlayerPrefStorage : IKeyValueStorage
    {
        #region Methods

        /// <summary>
        /// 判断指定键是否存在
        /// <para>Returns whether the given key exists</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <returns>键是否存在<para>Whether the key exists</para></returns>
        public bool ExistKey(string key) => PlayerPrefs.HasKey(key);

        /// <summary>
        /// 读取指定键的字符串值
        /// <para>Reads the string value of the given key</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <param name="fallback">键不存在时的回退值<para>The fallback when the key is missing</para></param>
        /// <returns>存储的字符串值<para>The stored string value</para></returns>
        public string GetString(string key, string fallback = null) => PlayerPrefs.GetString(key, fallback);

        /// <summary>
        /// 写入指定键的字符串值
        /// <para>Writes the string value of the given key</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <param name="value">要写入的字符串值<para>The string value to write</para></param>
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);

        /// <summary>
        /// 删除指定键
        /// <para>Deletes the given key</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);

        /// <summary>
        /// 调用 PlayerPrefs.Save 立即落盘
        /// <para>Calls PlayerPrefs.Save to flush immediately</para>
        /// </summary>
        public void Flush() => PlayerPrefs.Save();

        #endregion
    }
}