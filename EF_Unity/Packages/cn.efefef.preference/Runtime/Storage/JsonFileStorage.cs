/*
 * ================================================
 * Describe:        基于 JSON 文件的键值存储实现（脏标记落盘，值以原生 JSON 节点内嵌）
 * Author:          Alvin8412
 * CreationTime:    2026-11-03 13:20:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 17:32:00
 * ScriptVersion:   0.3
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace EasyFramework.Preference
{
    /// <summary>
    /// JSON 文件实现：内存缓存 + 脏标记，Flush 时整体写盘；存入的字符串须为合法 JSON 值文本（由 PreferenceManager 保证），文件中以原生 JSON 节点呈现而非转义字符串
    /// <para>JSON file storage: in-memory cache with dirty flag, writes the whole file on Flush; stored strings must be valid JSON value texts (guaranteed by PreferenceManager) and appear as native JSON nodes instead of escaped strings</para>
    /// </summary>
    public sealed class JsonFileStorage : IKeyValueStorage
    {
        // 单条键值记录，Value 以 object 承载原生 JSON 节点，序列化时按运行时类型原样嵌入
        private class ItemData
        {
            public string Key;
            public object Value;
        }

        // 序列化根对象
        private class FileData
        {
            public List<ItemData> PreferenceItems = new();
        }

        #region 字段 / Fields

        // 偏好文件根目录，与 archive 包的 Archives 根约定一致
        private const string RootFolder = "Preference";

        // 存储文件完整路径
        private readonly string _filePath;

        // 键值内存缓存，懒加载
        private Dictionary<string, string> _cache;

        // 是否有未落盘的变更
        private bool _dirty;

        // 读取失败/回退空缓存标记：Flush 覆盖前先备份损坏文件，避免静默抹掉尚可恢复的数据
        private bool _loadFailed;

        #endregion

        #region 方法 / Methods

        // 将 JSON 值文本转为原生节点；非 JSON 文本（如绕过管理器写入的密文）降级为字符串节点
        private static object ToNode(string text)
        {
            try
            {
                return JsonConvert.DeserializeObject(text);
            }
            catch
            {
                return text;
            }
        }

        // 懒加载磁盘文件到内存缓存，值节点还原为 JSON 值文本
        private Dictionary<string, string> EnsureCache()
        {
            if (_cache != null)
                return _cache;

            _cache = new Dictionary<string, string>();
            if (!File.Exists(_filePath))
                return _cache;

            // 文件损坏（截断/外部误编辑）、被占用或无权限时，回退空缓存而不是崩溃——
            // EnsureCache 是 Get/Set/Flush 的公共入口，异常上抛会让偏好系统整体不可用，
            // 且 Quit 中的 Flush 抛异常会中断退出流程
            try
            {
                var file = JsonConvert.DeserializeObject<FileData>(File.ReadAllText(_filePath));
                if (file?.PreferenceItems == null)
                {
                    _loadFailed = true; // 结构无效，Flush 前需备份原文件
                    return _cache;
                }

                foreach (var item in file.PreferenceItems)
                {
                    if (string.IsNullOrEmpty(item.Key)) continue; // 损坏条目：跳过而非抛 ArgumentNullException
                    _cache[item.Key] = JsonConvert.SerializeObject(item.Value);
                }
            }
            catch (Exception e)
            {
                _loadFailed = true;
                Debug.LogError($"[JsonFileStorage] 读取 {_filePath} 失败，已回退空缓存：{e.Message}");
            }
            return _cache;
        }

        /// <summary>
        /// 创建文件存储，文件位于 persistentDataPath/Preference 下
        /// <para>Creates a file storage under persistentDataPath/Preference</para>
        /// </summary>
        /// <param name="fileName">存储文件名<para>The storage file name</para></param>
        public JsonFileStorage(string fileName = "preferences.json")
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
        /// 写入指定键的字符串值并标记脏，值须为合法 JSON 值文本
        /// <para>Writes the string value of the given key and marks dirty; the value must be a valid JSON value text</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <param name="value">要写入的 JSON 值文本<para>The JSON value text to write</para></param>
        public void SetString(string key, string value)
        {
            JsonConvert.DeserializeObject(value);
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
        /// 有脏变更时将缓存整体序列化写盘，值以原生 JSON 节点内嵌
        /// <para>Serializes and writes the whole cache when dirty, values embedded as native JSON nodes</para>
        /// </summary>
        public void Flush()
        {
            if (!_dirty)
                return;

            var file = new FileData();
            foreach (var pair in EnsureCache())
            {
                file.PreferenceItems.Add(new ItemData
                {
                    Key = pair.Key,
                    Value = ToNode(pair.Value)
                });
            }

            // 幂等创建根目录，避免首次落盘因目录不存在而失败
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_filePath));

                // 之前读取失败回退了空缓存，这次 Flush 会用空数据整体覆盖原文件；
                // 覆盖前备份损坏文件，保留手动恢复的可能（只备份一次）
                if (_loadFailed && File.Exists(_filePath))
                {
                    string backup = $"{_filePath}.corrupt.{DateTime.Now:yyyyMMddHHmmss}";
                    File.Copy(_filePath, backup, true);
                    Debug.LogWarning($"[JsonFileStorage] 已备份读取失败的文件到 {backup}");
                    _loadFailed = false;
                }

                File.WriteAllText(_filePath, JsonConvert.SerializeObject(file, Formatting.Indented),
                    new System.Text.UTF8Encoding(false));
                _dirty = false;
            }
            catch (Exception e)
            {
                // 磁盘满/只读/权限不足时保留 _dirty=true 供后续重试，不让异常中断 Quit 流程
                Debug.LogError($"[JsonFileStorage] 写入 {_filePath} 失败：{e.Message}");
            }
        }

        #endregion
    }
}