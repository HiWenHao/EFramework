/*
 * ================================================
 * Describe:        玩家偏好管理器：存储可替换、变更通知、生命周期自动落盘
 * Author:          Alvin8412
 * CreationTime:    2026-11-03 13:25:00
 * ModifyAuthor:    Alvin8412
 * ModifyTime:      2026-11-03 13:25:00
 * ScriptVersion:   0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using EasyFramework.Managers;
using Newtonsoft.Json;

namespace EasyFramework.Preference
{
    /// <summary>
    /// 玩家偏好管理器：在存储契约之上提供类型化读写、键级变更通知与自动落盘
    /// <para>Preference manager: typed read/write, per-key change notification and lifecycle auto flush on top of the storage contract</para>
    /// </summary>
    [Manager(Order = 99850)]
    public sealed class PreferenceManager : Singleton<PreferenceManager>, ISingleton
    {
        // 退订句柄
        private sealed class Subscription : IDisposable
        {
            // 退订动作
            private readonly Action _dispose;

            public Subscription(Action dispose) => _dispose = dispose;

            public void Dispose() => _dispose?.Invoke();
        }

        #region Fields

        // 存储实现，默认 PlayerPrefs，可在启动早期替换
        private IKeyValueStorage _storage;

        // 偏好加密器，null 表示不加密
        private IPreferenceCrypto _crypto;

        // 键级变更通知缓存，懒创建
        private Dictionary<string, BindableProperty<string>> _properties;

        #endregion

        #region Methods

        void ISingleton.Init()
        {
            _properties = new Dictionary<string, BindableProperty<string>>();
            _storage ??= new PlayerPrefStorage();
        }

        void ISingleton.Quit()
        {
            Flush();
            _properties.Clear();
            _properties = null;
            _storage = null;
        }

        /// <summary>
        /// 替换存储实现，须在任何读写前调用；框架内置两种实现：<see cref="PlayerPrefStorage"/>（默认，平台托管）与 <see cref="JsonFileStorage"/>（本地 JSON 文件，位于 persistentDataPath/Preference 下）
        /// <para>Replaces the storage; call before any read/write. Two built-in implementations: <see cref="PlayerPrefStorage"/> (default, platform-managed) and <see cref="JsonFileStorage"/> (local JSON file under persistentDataPath/Preference)</para>
        /// </summary>
        /// <param name="storage">新的存储实现<para>The new storage implementation</para></param>
        public void SetStorage(IKeyValueStorage storage)
        {
            _storage = storage;
            _properties.Clear();
        }

        /// <summary>
        /// 设置偏好加密器，传 null 关闭加密（默认不加密）；框架内置一种实现：<see cref="XorPreferenceCrypto"/>（XOR+Base64 简单混淆，仅防明文直接可见，非安全实现）；切换会清空已缓存的通知值，建议仅启动早期调用
        /// <para>Sets the preference crypto, pass null to disable (off by default). One built-in implementation: <see cref="XorPreferenceCrypto"/> (simple XOR+Base64 obfuscation, hides plain text only, NOT secure); switching clears cached values, call early at startup only</para>
        /// </summary>
        /// <param name="crypto">加密器实例<para>The crypto instance</para></param>
        public void SetCrypto(IPreferenceCrypto crypto)
        {
            _crypto = crypto;
            _properties.Clear();
        }

        /// <summary>
        /// 判断指定偏好键是否存在
        /// <para>Returns whether the given preference key exists</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <returns>键是否存在<para>Whether the key exists</para></returns>
        public bool Exist(string key) => _storage != null && _storage.ExistKey(key);

        /// <summary>
        /// 读取偏好值，键不存在或反序列化失败时返回回退值
        /// <para>Reads the preference value; returns the fallback when the key is missing or deserialization fails</para>
        /// </summary>
        /// <typeparam name="T">偏好值类型，须可被 Newtonsoft 序列化<para>The value type, must be Newtonsoft-serializable</para></typeparam>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <param name="fallback">回退值<para>The fallback value</para></param>
        /// <returns>偏好值<para>The preference value</para></returns>
        public T Get<T>(string key, T fallback = default)
        {
            string raw = _storage != null && _storage.ExistKey(key) ? Decrypt(_storage.GetString(key)) : null;
            return Deserialize(raw, fallback);
        }

        /// <summary>
        /// 写入偏好值并广播变更，值未变化时不触发通知
        /// <para>Writes the preference value and raises change notification; no-op notification when the value is unchanged</para>
        /// </summary>
        /// <typeparam name="T">偏好值类型，须可被 Newtonsoft 序列化<para>The value type, must be Newtonsoft-serializable</para></typeparam>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <param name="value">要写入的偏好值<para>The preference value to write</para></param>
        public void Set<T>(string key, T value)
        {
            if (_storage == null)
                return;

            string raw = JsonConvert.SerializeObject(value);
            GetProperty(key, raw).Value = raw;
            _storage.SetString(key, Encrypt(raw));
        }

        /// <summary>
        /// 删除偏好键并清除其变更通知缓存
        /// <para>Deletes the preference key and clears its change cache</para>
        /// </summary>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        public void Delete(string key)
        {
            _storage?.DeleteKey(key);
            _properties.Remove(key);
        }

        /// <summary>
        /// 订阅偏好变更：立即回调一次当前值，之后每次变化回调，返回退订句柄
        /// <para>Subscribes to changes: invokes once with the current value, then on every change; returns an unsubscribe handle</para>
        /// </summary>
        /// <typeparam name="T">偏好值类型，须可被 Newtonsoft 序列化<para>The value type, must be Newtonsoft-serializable</para></typeparam>
        /// <param name="key">偏好键名<para>The preference key</para></param>
        /// <param name="onChanged">变更回调<para>The change callback</para></param>
        /// <returns>退订句柄<para>The unsubscribe handle</para></returns>
        public IDisposable Bind<T>(string key, Action<T> onChanged)
        {
            if (onChanged == null)
                throw new ArgumentNullException(nameof(onChanged));

            BindableProperty<string> property = GetProperty(key, null);
            property.OnValueChanged += Handler;
            onChanged(Deserialize<T>(property.Value, default));

            return new Subscription(() => property.OnValueChanged -= Handler);
            void Handler(string raw) => onChanged(Deserialize<T>(raw, default));
        }

        /// <summary>
        /// 立即持久化未落盘的变更
        /// <para>Persists pending changes immediately</para>
        /// </summary>
        public void Flush() => _storage?.Flush();

        // 获取键级通知属性，首次访问时从存储回填当前值
        private BindableProperty<string> GetProperty(string key, string initial)
        {
            if (_properties.TryGetValue(key, out var property))
                return property;

            if (initial == null && _storage != null && _storage.ExistKey(key))
                initial = Decrypt(_storage.GetString(key));

            property = new BindableProperty<string> { Value = initial };
            _properties[key] = property;
            return property;
        }

        // 反序列化，空串或失败时返回回退值
        private static T Deserialize<T>(string raw, T fallback)
        {
            if (string.IsNullOrEmpty(raw))
                return fallback;
            try
            {
                return JsonConvert.DeserializeObject<T>(raw);
            }
            catch (Exception e)
            {
                D.Error($"[ PreferenceManager ] Deserialize {typeof(T).Name} failed:\t{e.Message}");
                return fallback;
            }
        }

        // 加密，未设置加密器时原样返回
        private string Encrypt(string plain) => _crypto != null ? _crypto.Encrypt(plain) : plain;

        // 解密，未设置加密器时原样返回，解密失败时返回 null 走回退值
        private string Decrypt(string cipher)
        {
            if (_crypto == null || string.IsNullOrEmpty(cipher))
                return cipher;
            try
            {
                return _crypto.Decrypt(cipher);
            }
            catch (Exception e)
            {
                D.Warning($"[ PreferenceManager ] Decrypt failed, fallback to default:\t{e.Message}");
                return null;
            }
        }

        #endregion
    }
}