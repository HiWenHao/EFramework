/*
 * ================================================
 * Describe:      存档系统主管理器。负责槽位路由、序列化/反序列化、
 *                加密协调、自动保存调度。底层存储全委托给 ISaveProvider。
 * Author:        Alvin8412
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin8412
 * ModifyTime:    2026-06-24 22:25:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using Newtonsoft.Json;
using UnityEngine;

namespace EasyFramework.Systems.Save
{
    /// <summary>
    /// 存档系统主管理器（MonoSingleton）。
    /// <para>使用方式：</para>
    /// <code>
    /// // 保存
    /// await SaveManager.Instance.SaveAsync("player_info", myData);
    ///
    /// // 读取
    /// var data = await SaveManager.Instance.LoadAsync&lt;GameSaveData&gt;("player_info");
    ///
    /// // 读取（带默认值）
    /// var settings = await SaveManager.Instance.LoadOrCreateAsync&lt;GameSettings&gt;("settings");
    /// </code>
    /// </summary>
    [Manager(Order = 99100)]
    public class SaveManager : MonoSingleton<SaveManager>, ISingleton
    {
        private ISaveProvider _provider;
        private SaveSettings _settings;
        private readonly Dictionary<int, HashSet<string>> _dirtyKeys = new();
        private CancellationTokenSource _autoSaveCts;
        private JsonSerializerSettings _jsonSettings;

        /// <summary>当前激活的槽位编号</summary>
        public int ActiveSlot { get; set; } = 0;

        /// <summary>存档设置引用</summary>
        public SaveSettings Settings => _settings;

        #region Singleton Lifecycle

        void ISingleton.Init()
        {
            LoadSettings();
            _provider = CreateProvider();
            _jsonSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = _settings.warnOnUnknownFields
                    ? MissingMemberHandling.Error
                    : MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            StartAutoSave();
        }

        void ISingleton.Quit()
        {
            StopAutoSave();
            FlushAsync().Forget();
        }

        #endregion

        #region Provider Management

        /// <summary>
        /// 运行时替换存储后端。调用后立即切换，已有数据不会自动迁移。
        /// </summary>
        public void SetProvider(ISaveProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        private ISaveProvider CreateProvider()
        {
            // 如果配置了自定义 Provider 类型名，通过反射创建
            if (!string.IsNullOrEmpty(_settings.providerTypeName))
            {
                var type = Type.GetType(_settings.providerTypeName, throwOnError: false);
                if (type != null)
                {
                    var instance = Activator.CreateInstance(type, _settings) as ISaveProvider;
                    if (instance != null)
                        return instance;
                }
            }

            // 默认：文件存储
            return new FileSaveProvider(_settings);
        }

        #endregion

        #region Slot Management

        /// <summary>
        /// 创建新存档槽位（自动分配最小可用编号）
        /// </summary>
        public async UniTask<int> CreateSlotAsync(string slotName, CancellationToken ct = default)
        {
            int slotId = FindFreeSlotId();
            var meta = new SaveSlotMeta
            {
                slotId = slotId,
                slotName = slotName,
                progressDescription = "新游戏",
                createdAt = DateTime.Now,
                lastModifiedAt = DateTime.Now,
                dataVersion = _settings.dataVersion,
                isValid = true
            };

            if (_provider is FileSaveProvider fileProvider)
                await fileProvider.SaveMetaAsync(meta, ct);

            ActiveSlot = slotId;
            return slotId;
        }

        /// <summary>
        /// 获取所有存档槽位元数据（用于存档选择界面）
        /// </summary>
        public SaveSlotMeta[] GetAllSlots()
        {
            if (_provider is FileSaveProvider fileProvider)
                return fileProvider.ListSlots();

            return Array.Empty<SaveSlotMeta>();
        }

        /// <summary>
        /// 删除指定槽位
        /// </summary>
        public async UniTask DeleteSlotAsync(int slotId, CancellationToken ct = default)
        {
            await _provider.DeleteSlotAsync(slotId, ct);
            _dirtyKeys.Remove(slotId);
        }

        private int FindFreeSlotId()
        {
            var slots = GetAllSlots();
            for (int i = 0; i < _settings.maxSlots; i++)
            {
                bool taken = false;
                foreach (var s in slots)
                {
                    if (s.slotId == i)
                    {
                        taken = true;
                        break;
                    }
                }
                if (!taken)
                    return i;
            }
            return -1; // 所有槽位已满
        }

        #endregion

        #region Save & Load

        /// <summary>
        /// 保存数据到当前激活槽位
        /// </summary>
        public async UniTask SaveAsync<T>(string key, T data, CancellationToken ct = default)
        {
            await SaveToSlotAsync(ActiveSlot, key, data, ct);
        }

        /// <summary>
        /// 保存数据到指定槽位
        /// </summary>
        public async UniTask SaveToSlotAsync<T>(int slotId, string key, T data, CancellationToken ct = default)
        {
            // 序列化
            string json = JsonConvert.SerializeObject(data, _jsonSettings);
            byte[] raw = Encoding.UTF8.GetBytes(json);

            // 交给 Provider 写入（Provider 自行决定是否需要加密）
            await _provider.SaveRawAsync(slotId, key, raw, ct);

            // 标记为干净
            MarkClean(slotId, key);

            // 更新槽位元数据
            await UpdateSlotMetaAfterSave(slotId);
        }

        /// <summary>
        /// 从当前激活槽位读取数据
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default)
        {
            return await LoadFromSlotAsync<T>(ActiveSlot, key, ct);
        }

        /// <summary>
        /// 从指定槽位读取数据
        /// </summary>
        public async UniTask<T> LoadFromSlotAsync<T>(int slotId, string key, CancellationToken ct = default)
        {
            byte[] raw = await _provider.LoadRawAsync(slotId, key, ct);
            if (raw == null)
                throw new KeyNotFoundException($"Save key '{key}' not found in slot {slotId}.");

            string json = Encoding.UTF8.GetString(raw);
            return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
        }

        /// <summary>
        /// 读取数据，如果不存在则返回 default(T)。
        /// </summary>
        public async UniTask<T> LoadOrDefaultAsync<T>(string key, CancellationToken ct = default)
        {
            return await LoadOrDefaultFromSlotAsync<T>(ActiveSlot, key, ct);
        }

        /// <summary>
        /// 从指定槽位读取数据，如果不存在则返回 default(T)。
        /// </summary>
        public async UniTask<T> LoadOrDefaultFromSlotAsync<T>(int slotId, string key, CancellationToken ct = default)
        {
            try
            {
                return await LoadFromSlotAsync<T>(slotId, key, ct);
            }
            catch (KeyNotFoundException)
            {
                return default;
            }
        }

        /// <summary>
        /// 读取数据，如果不存在则创建并保存默认值，然后返回。
        /// 典型用法：首次游戏时自动初始化设置。
        /// </summary>
        public async UniTask<T> LoadOrCreateAsync<T>(string key, CancellationToken ct = default)
            where T : new()
        {
            return await LoadOrCreateFromSlotAsync<T>(ActiveSlot, key, ct);
        }

        /// <summary>
        /// 从指定槽位读取数据，如果不存在则创建并保存默认值。
        /// </summary>
        public async UniTask<T> LoadOrCreateFromSlotAsync<T>(int slotId, string key, CancellationToken ct = default)
            where T : new()
        {
            try
            {
                return await LoadFromSlotAsync<T>(slotId, key, ct);
            }
            catch (KeyNotFoundException)
            {
                var data = new T();
                await SaveToSlotAsync(slotId, key, data, ct);
                return data;
            }
        }

        #endregion

        #region Other Operations

        /// <summary>
        /// 检查指定 key 是否存在于当前槽位
        /// </summary>
        public UniTask<bool> ExistsAsync(string key, CancellationToken ct = default)
            => _provider.ExistsAsync(ActiveSlot, key, ct);

        /// <summary>
        /// 从当前槽位删除指定 key
        /// </summary>
        public UniTask DeleteKeyAsync(string key, CancellationToken ct = default)
        {
            MarkClean(ActiveSlot, key);
            return _provider.DeleteAsync(ActiveSlot, key, ct);
        }

        /// <summary>
        /// 列出当前槽位的所有 key
        /// </summary>
        public UniTask<string[]> ListKeysAsync(CancellationToken ct = default)
            => _provider.ListKeysAsync(ActiveSlot, ct);

        /// <summary>
        /// 获取当前槽位所有数据总大小
        /// </summary>
        public UniTask<long> GetSlotSizeAsync(CancellationToken ct = default)
            => _provider.GetSlotSizeAsync(ActiveSlot, ct);

        /// <summary>
        /// 强制刷新所有缓存到磁盘
        /// </summary>
        public async UniTask FlushAsync(CancellationToken ct = default)
        {
            await _provider.FlushAsync(ct);
            _dirtyKeys.Clear();
        }

        #endregion

        #region Dirty Marking (for auto-save)

        /// <summary>
        /// 标记某条数据为脏（有未保存的变动）。
        /// 手动调用 SaveAsync 后自动清除脏标记；
        /// 如果通过外部方式修改了数据对象，需手动调用此方法告知自动保存。
        /// </summary>
        public void MarkDirty(string key)
        {
            MarkDirty(ActiveSlot, key);
        }

        /// <summary>
        /// 标记指定槽位的某条数据为脏。
        /// </summary>
        public void MarkDirty(int slotId, string key)
        {
            if (!_dirtyKeys.ContainsKey(slotId))
                _dirtyKeys[slotId] = new HashSet<string>();

            _dirtyKeys[slotId].Add(key);
        }

        private void MarkClean(int slotId, string key)
        {
            if (_dirtyKeys.TryGetValue(slotId, out var set))
                set.Remove(key);
        }

        #endregion

        #region Auto Save

        /// <summary>
        /// 启用自动保存（已在 Init 中自动启动，间隔由 SaveSettings 控制）
        /// </summary>
        public void StartAutoSave()
        {
            StopAutoSave();

            if (_settings.autoSaveIntervalSeconds <= 0)
                return;

            _autoSaveCts = new CancellationTokenSource();
            AutoSaveLoopAsync(_autoSaveCts.Token).Forget();
        }

        /// <summary>
        /// 停止自动保存
        /// </summary>
        public void StopAutoSave()
        {
            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = null;
        }

        private async UniTask AutoSaveLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_settings.autoSaveIntervalSeconds),
                    cancellationToken: ct);

                if (_settings.autoSaveOnlyDirty)
                {
                    // 只保存标记为脏的 key
                    // 注意：当前架构下 SaveManager 不持有数据副本，
                    // 所以脏标记需要外部通过 MarkDirty 显式告知
                    if (_dirtyKeys.Count == 0)
                        continue;
                }

                await FlushAsync(ct);
            }
        }

        #endregion

        #region Internal Helpers

        private void LoadSettings()
        {
            _settings = Resources.Load<SaveSettings>("Configs/SaveSettings");
            if (_settings == null)
            {
                _settings = ScriptableObject.CreateInstance<SaveSettings>();
                D.Warning("[SaveManager] SaveSettings not found at Resources/Configs/SaveSettings. Using defaults.");
            }
        }

        private async UniTask UpdateSlotMetaAfterSave(int slotId)
        {
            if (_provider is FileSaveProvider fileProvider)
            {
                var meta = await fileProvider.LoadMetaAsync(slotId);
                if (meta != null)
                {
                    var updated = meta.Value;
                    updated.lastModifiedAt = DateTime.Now;
                    updated.dataVersion = _settings.dataVersion;
                    updated.totalSizeBytes = await _provider.GetSlotSizeAsync(slotId);
                    await fileProvider.SaveMetaAsync(updated);
                }
            }
        }

        #endregion
    }
}
