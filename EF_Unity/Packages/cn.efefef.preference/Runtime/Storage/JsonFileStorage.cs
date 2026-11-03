/*
 * ================================================
 * Describe:        基于 JSON 文件的键值存储实现（脏标记落盘）
 * Author:          Alvin8412
 * CreationTime:    2026-11-03 13:20:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 13:20:00
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EasyFramework.Preference
{
    /// <summary>
    /// JSON 文件实现：内存缓存 + 脏标记，Flush 时整体写盘，文件可读可手动改
    /// <para>JSON file storage: in-memory cache with dirty flag, writes the whole file on Flush, human-readable</para>
    /// </summary>
    public sealed class JsonFileStorage : IKeyValueStorage
    {
        // 单条键值记录
        [Serializable]
        private class StorageData
        {
            public string Key;
            public string Value;
        }

        // 序列化根对象：JsonUtility 不支持根级 List，须包一层普通类
        [Serializable]
        private class StorageFile
        {
            public List<StorageData> PreferenceItems = new List<StorageData>();
        }

        #region Fields

        // 偏好文件根目录，与 archive 包的 Archives 根约定一致
        private const string RootFolder = "Preference";

        // 存储文件完整路径
        private readonly string _filePath;

        // 键值内存缓存，懒加载
        private Dictionary<string, string> _cache;

        // 是否有未落盘的变更
        private bool _dirty;

        #endregion

        #region Methods

        /// <summary>
        /// 创建文件存储，文件位于 persistentDataPath/Preference 下
        /// <para>Creates a file storage under persistentDataPath/Preference</para>
        /// </summary>
        /// <param name="fileName">存储文件名<para>The storage file name</para></param>
        public JsonFileStorage(string fileName = "ef.preferences.json")
        {
            _filePath = Path.Combine(Application.persistentDataPath, RootFolder, fileName);
        }

        /// <summary>
        /// 判断指定键是否存在
        /// <para>Returns whether the given key exists</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <returns>键是否存在<para>Whether the key exists</para></returns>
        public bool ExistKey(string key) => EnsureCache().ContainsKey(key);

        /// <summary>
        /// 读取指定键的字符串值
        /// <para>Reads the string value of the given key</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <param name="fallback">键不存在时的回退值<para>The fallback when the key is missing</para></param>
        /// <returns>存储的字符串值<para>The stored string value</para></returns>
        public string GetString(string key, string fallback = null) =>
            EnsureCache().GetValueOrDefault(key, fallback);

        /// <summary>
        /// 写入指定键的字符串值并标记脏
        /// <para>Writes the string value of the given key and marks dirty</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <param name="value">要写入的字符串值<para>The string value to write</para></param>
        public void SetString(string key, string value)
        {
            EnsureCache()[key] = value;
            _dirty = true;
        }

        /// <summary>
        /// 删除指定键并标记脏
        /// <para>Deletes the given key and marks dirty</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        public void DeleteKey(string key)
        {
            if (!EnsureCache().Remove(key))
                return;
            _dirty = true;
        }

        /// <summary>
        /// 有脏变更时将缓存整体序列化写盘
        /// <para>Serializes and writes the whole cache when dirty</para>
        /// </summary>
        public void Flush()
        {
            if (!_dirty)
                return;

            var file = new StorageFile();
            foreach (var pair in EnsureCache())
            {
                file.PreferenceItems.Add(new StorageData
                {
                    Key = pair.Key,
                    Value = pair.Value
                });
            }

            // 幂等创建根目录，避免首次落盘因目录不存在而失败
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            File.WriteAllText(_filePath, JsonUtility.ToJson(file, true));
            _dirty = false;
        }

        // 懒加载磁盘文件到内存缓存
        private Dictionary<string, string> EnsureCache()
        {
            if (_cache != null)
                return _cache;

            _cache = new Dictionary<string, string>();
            if (!File.Exists(_filePath))
                return _cache;

            var file = JsonUtility.FromJson<StorageFile>(File.ReadAllText(_filePath));
            if (file?.PreferenceItems == null)
                return _cache;

            foreach (var storageData in file.PreferenceItems)
            {
                _cache[storageData.Key] = storageData.Value;
            }

            return _cache;
        }

        #endregion
    }
}