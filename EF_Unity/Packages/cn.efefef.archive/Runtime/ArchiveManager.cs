/*
 * ================================================
 * Describe:      存档系统主管理器。负责槽位路由、序列化/反序列化、
 *                加密协调、自动保存调度。底层存储全委托给 IArchiveProvider。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 01:34:00
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

namespace EasyFramework.Systems.Archive
{
    /// <summary>
    /// 存档系统主管理器，负责槽位路由、JSON 序列化、加密协调与自动保存调度。
    /// <para>Archive system main manager. Handles slot routing, JSON serialization,
    /// encryption coordination, and auto-save scheduling.</para>
    /// </summary>
    [Manager(Order = 99100)]
    public class ArchiveManager : MonoSingleton<ArchiveManager>, ISingleton
    {
        private IArchiveProvider _provider;           // 存档存储后端
        private readonly Dictionary<int, HashSet<string>> _dirtyKeys = new(); // 脏标记（槽位 → 已变动的 key 集合）
        private readonly Dictionary<int, Dictionary<string, object>> _dataCache = new(); // 数据缓存（槽位 → key → 对象引用），用于自动保存重放
        private const int MaxCachedEntries = 50; // 缓存上限（防止内存无限增长）
        private CancellationTokenSource _autoSaveCts;   // 自动保存取消令牌
        private JsonSerializerSettings _jsonSettings;   // JSON 序列化设置（循环引用处理、字段忽略策略）

        /// <summary>
        /// 当前激活的槽位编号（默认 0）
        /// <para>Currently active slot index (default 0)</para>
        /// </summary>
        public int ActiveSlot { get; set; } = 0;

        /// <summary>
        /// 存档系统全局配置引用
        /// <para>Reference to the archive system configuration</para>
        /// </summary>
        public ArchiveSettings Settings { get; private set; }

        #region Singleton Lifecycle

        void ISingleton.Init()
        {
            LoadSettings();
            _provider = CreateProvider();
            _jsonSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MissingMemberHandling = Settings.warnOnUnknownFields
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
            FlushDirtySync();
        }

        // 同步写入所有脏数据（Quit 时调用），单条失败不中断整体流程
        private void FlushDirtySync()
        {
            foreach (var (slotId, keySet) in _dirtyKeys)
            {
                if (!_dataCache.TryGetValue(slotId, out var slotCache)) continue;
                foreach (var key in keySet)
                {
                    if (!slotCache.TryGetValue(key, out var cached)) continue;
                    try
                    {
                        string json = JsonConvert.SerializeObject(cached, _jsonSettings);
                        byte[] raw = Encoding.UTF8.GetBytes(json);
                        _provider.SaveRawAsync(slotId, key, raw);
                    }
                    catch (Exception ex)
                    {
                        D.Error($"[ArchiveManager] Failed to flush dirty key '{key}' in slot {slotId} on quit: {ex.Message}");
                    }
                }
            }
            _dirtyKeys.Clear();
            _dataCache.Clear();
        }

        #endregion

        #region Private

        // 加载 ArchiveSettings 配置资产（从 Resources/Configs 读取，不存在则使用默认值）
        private void LoadSettings()
        {
            Settings = Resources.Load<ArchiveSettings>("Configs/ArchiveSettings");
            if (Settings != null) return;
            Settings = ScriptableObject.CreateInstance<ArchiveSettings>();
            D.Warning("[ArchiveManager] ArchiveSettings not found at Resources/Configs/ArchiveSettings. Using defaults.");
        }

        // 根据配置创建存储后端（默认 FileArchiveProvider，可通过 providerTypeName 反射创建自定义后端）
        private IArchiveProvider CreateProvider()
        {
            if (string.IsNullOrEmpty(Settings.providerTypeName)) return new FileArchiveProvider(Settings);
            var type = Type.GetType(Settings.providerTypeName, throwOnError: false);
            if (type == null) return new FileArchiveProvider(Settings);
            if (Activator.CreateInstance(type, Settings) is IArchiveProvider instance)
                return instance;

            return new FileArchiveProvider(Settings);
        }

        // 查找最小的空闲槽位编号（O(n) via HashSet）
        // 查找最小的空闲槽位编号
        private async UniTask<int> FindFreeSlotIdAsync()
        {
            var takenIds = new HashSet<int>();
            var slots = await GetAllSlotsAsync();
            if (slots != null)
            {
                foreach (var s in slots)
                    takenIds.Add(s.slotId);
            }

            for (int i = 0; i < Settings.maxSlots; i++)
                if (!takenIds.Contains(i)) return i;

            return -1;
        }

        // 清除指定 key 的脏标记（保存成功后调用）
        private void MarkClean(int slotId, string key)
        {
            if (_dirtyKeys.TryGetValue(slotId, out var set))
                set.Remove(key);
        }

        // 保存后更新槽位元数据（修改时间、数据版本、总大小）并缓存数据供自动保存使用
        private async UniTask UpdateSlotMetaAfterSave(int slotId)
        {
            var meta = await _provider.LoadMetaAsync(slotId);
            if (meta != null)
            {
                var updated = meta.Value;
                updated.SetModifiedNow();
                updated.dataVersion = Settings.dataVersion;
                updated.totalSizeBytes = await _provider.GetSlotSizeAsync(slotId);
                await _provider.SaveMetaAsync(updated);
            }
        }

        // 缓存最近一次保存的数据（供自动保存重放），超出上限时淘汰最早条目
        private void CacheData<T>(int slotId, string key, T data)
        {
            if (!_dataCache.TryGetValue(slotId, out var slotCache))
                _dataCache[slotId] = slotCache = new Dictionary<string, object>();
            slotCache[key] = data;

            EvictIfNeeded();
        }

        // 缓存条目数超过上限时，从最先找到的非空槽位中淘汰一条（近似 FIFO）
        private void EvictIfNeeded()
        {
            int totalCount = 0;
            foreach (var sc in _dataCache.Values)
                totalCount += sc.Count;

            while (totalCount > MaxCachedEntries)
            {
                bool removed = false;
                foreach (var slotCache in _dataCache.Values)
                {
                    if (slotCache.Count == 0) continue;
                    var e = slotCache.GetEnumerator();
                    e.MoveNext();
                    slotCache.Remove(e.Current.Key);
                    totalCount--;
                    removed = true;
                    break;
                }
                // 防御：若所有槽位均为空但 totalCount 仍 > 0（极端情况），跳出避免死循环
                if (!removed) break;
            }
        }

        // 移除指定 key 的缓存数据（删除 key 或槽位时调用，释放内存）
        private void RemoveCachedData(int slotId, string key)
        {
            if (_dataCache.TryGetValue(slotId, out var slotCache))
                slotCache.Remove(key);
        }

        // 自动保存循环：遍历脏 key，用缓存的数据重放保存
        private async UniTask AutoSaveLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(Settings.autoSaveIntervalSeconds), cancellationToken: ct);

                if (Settings.autoSaveOnlyDirty && _dirtyKeys.Count == 0)
                    continue;

                // 先快照脏 key 列表，防止枚举过程中外部 MarkDirty 修改字典
                await FlushDirtyKeysAsync(ct);
            }
        }

        // 遍历所有槽位的脏 key，从缓存取出数据重放保存。
        // 若缓存中没有数据（如仅调用 MarkDirty 而未先 Save/Load），跳过并警告。
        private async UniTask FlushDirtyKeysAsync(CancellationToken ct)
        {
            if (_dirtyKeys.Count == 0) return;

            // 快照：防止枚举过程中被 MarkDirty / MarkClean 修改
            var snapshot = new Dictionary<int, string[]>();
            foreach (var (slotId, keySet) in _dirtyKeys)
                snapshot[slotId] = new List<string>(keySet).ToArray();

            foreach (var (slotId, keys) in snapshot)
            {
                if (!_dataCache.TryGetValue(slotId, out var slotCache))
                {
                    // 槽位缓存为空：所有属于该槽位的脏 key 都无法保存，清除标记
                    foreach (var key in keys)
                    {
                        D.Warning($"[ArchiveManager] Slot {slotId} key '{key}' marked dirty but slot cache is empty. Skipping auto-save. Use SaveToSlotAsync or MarkDirtyWithData to provide data.");
                        MarkClean(slotId, key);
                    }
                    continue;
                }

                foreach (var key in keys)
                {
                    if (!slotCache.TryGetValue(key, out var cached))
                    {
                        D.Warning($"[ArchiveManager] Key '{key}' in slot {slotId} marked dirty but not in cache. Skipping auto-save. Use MarkDirtyWithData to provide current data.");
                        MarkClean(slotId, key);
                        continue;
                    }

                    string json = JsonConvert.SerializeObject(cached, _jsonSettings);
                    byte[] raw = Encoding.UTF8.GetBytes(json);
                    await _provider.SaveRawAsync(slotId, key, raw, ct);
                    MarkClean(slotId, key);
                }
            }
        }

        #endregion

        #region Public — Provider

        /// <summary>
        /// 运行时替换存储后端（文件 / SQLite / 云端）。调用后立即生效，已有数据不会自动迁移。
        /// <para>Replace the storage backend at runtime (File / SQLite / Cloud).
        /// Takes effect immediately; existing data is not migrated automatically.</para>
        /// </summary>
        /// <param name="provider">新的存储后端实例
        /// <para>The new storage provider instance</para></param>
        public void SetProvider(IArchiveProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        #endregion

        #region Public — Slot

        /// <summary>
        /// 创建新存档槽位（自动分配最小可用编号），并切换为当前激活槽位。
        /// <para>Create a new archive slot (auto-assigns the lowest free ID) and switch to it.</para>
        /// </summary>
        /// <param name="slotName">槽位名称（如 "主存档"）
        /// <para>Slot display name (e.g. "Main Save")</para></param>
        /// <param name="ct">取消令牌
        /// <para>Cancellation token</para></param>
        /// <returns>新创建的槽位编号
        /// <para>The newly created slot ID</para></returns>
        /// <exception cref="InvalidOperationException">所有槽位已满时抛出
        /// <para>Thrown when all slots are occupied</para></exception>
        public async UniTask<int> CreateSlotAsync(string slotName, CancellationToken ct = default)
        {
            int slotId = await FindFreeSlotIdAsync();
            if (slotId < 0)
                throw new InvalidOperationException($"All {Settings.maxSlots} slots are full. Delete a slot first.");

            var meta = new ArchiveSlotMeta
            {
                slotId = slotId,
                slotName = slotName,
                progressDescription = "New Game",
                dataVersion = Settings.dataVersion,
                isValid = true
            };
            meta.SetCreatedNow();
            meta.SetModifiedNow();

            await _provider.SaveMetaAsync(meta, ct);

            ActiveSlot = slotId;
            return slotId;
        }

        /// <summary>
        /// 获取所有已存在的存档槽位元数据（用于存档选择界面）
        /// <para>Get metadata for all existing archive slots (used for save-selection UI)</para>
        /// </summary>
        /// <returns>槽位元数据数组
        /// <para>Array of slot metadata</para></returns>
        public UniTask<ArchiveSlotMeta[]> GetAllSlotsAsync(CancellationToken ct = default)
        {
            return _provider.ListSlotsAsync(ct);
        }

        /// <summary>
        /// 删除指定槽位及其所有存档数据
        /// <para>Delete a slot and all its archived data</para>
        /// </summary>
        /// <param name="slotId">要删除的槽位编号
        /// <para>ID of the slot to delete</para></param>
        /// <param name="ct">取消令牌
        /// <para>Cancellation token</para></param>
        public async UniTask DeleteSlotAsync(int slotId, CancellationToken ct = default)
        {
            await _provider.DeleteSlotAsync(slotId, ct);
            _dirtyKeys.Remove(slotId);
            _dataCache.Remove(slotId);
        }

        #endregion

        #region Public — Save & Load

        /// <summary>
        /// 将数据保存到当前激活槽位
        /// <para>Save data to the currently active slot</para>
        /// </summary>
        /// <typeparam name="T">可序列化的数据类型
        /// <para>Serializable data type</para></typeparam>
        /// <param name="key">数据键名（如 "player_data"）
        /// <para>Data key name (e.g. "player_data")</para></param>
        /// <param name="data">要保存的数据对象
        /// <para>The data object to save</para></param>
        /// <param name="ct">取消令牌
        /// <para>Cancellation token</para></param>
        public async UniTask SaveAsync<T>(string key, T data, CancellationToken ct = default)
        {
            await SaveToSlotAsync(ActiveSlot, key, data, ct);
        }

        /// <summary>
        /// 将数据保存到指定槽位
        /// <para>Save data to a specific slot</para>
        /// </summary>
        /// <typeparam name="T">可序列化的数据类型
        /// <para>Serializable data type</para></typeparam>
        /// <param name="slotId">目标槽位编号
        /// <para>Target slot ID</para></param>
        /// <param name="key">数据键名
        /// <para>Data key name</para></param>
        /// <param name="data">要保存的数据对象
        /// <para>The data object to save</para></param>
        /// <param name="ct">取消令牌
        /// <para>Cancellation token</para></param>
        public async UniTask SaveToSlotAsync<T>(int slotId, string key, T data, CancellationToken ct = default)
        {
            string json = JsonConvert.SerializeObject(data, _jsonSettings);
            byte[] raw = Encoding.UTF8.GetBytes(json);
            await _provider.SaveRawAsync(slotId, key, raw, ct);

            // Meta 更新为 best-effort：数据已安全落盘，meta 失败不影响数据正确性
            try
            {
                await UpdateSlotMetaAfterSave(slotId);
            }
            catch (Exception ex)
            {
                D.Warning($"[ArchiveManager] Meta update for '{key}' in slot {slotId} failed: {ex.Message}. Data is safe, meta will be corrected on next save.");
            }

            MarkClean(slotId, key);
            CacheData(slotId, key, data);
        }

        /// <summary>
        /// 从当前激活槽位读取数据
        /// <para>Load data from the currently active slot</para>
        /// </summary>
        /// <typeparam name="T">反序列化的目标类型
        /// <para>Target type for deserialization</para></typeparam>
        /// <param name="key">数据键名
        /// <para>Data key name</para></param>
        /// <param name="ct">取消令牌
        /// <para>Cancellation token</para></param>
        /// <returns>反序列化后的数据对象
        /// <para>Deserialized data object</para></returns>
        /// <exception cref="KeyNotFoundException">键不存在时抛出
        /// <para>Thrown when the key does not exist</para></exception>
        public async UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default)
        {
            return await LoadFromSlotAsync<T>(ActiveSlot, key, ct);
        }

        /// <summary>
        /// 从指定槽位读取数据
        /// <para>Load data from a specific slot</para>
        /// </summary>
        /// <typeparam name="T">反序列化的目标类型
        /// <para>Target type for deserialization</para></typeparam>
        /// <param name="slotId">源槽位编号
        /// <para>Source slot ID</para></param>
        /// <param name="key">数据键名
        /// <para>Data key name</para></param>
        /// <param name="ct">取消令牌
        /// <para>Cancellation token</para></param>
        /// <returns>反序列化后的数据对象
        /// <para>Deserialized data object</para></returns>
        /// <exception cref="KeyNotFoundException">键不存在时抛出
        /// <para>Thrown when the key does not exist</para></exception>
        public async UniTask<T> LoadFromSlotAsync<T>(int slotId, string key, CancellationToken ct = default)
        {
            byte[] raw = await _provider.LoadRawAsync(slotId, key, ct);
            if (raw == null)
                throw new KeyNotFoundException($"Archive key '{key}' not found in slot {slotId}.");

            string json = Encoding.UTF8.GetString(raw);
            T data = JsonConvert.DeserializeObject<T>(json, _jsonSettings);
            CacheData(slotId, key, data); // 缓存数据引用，供 MarkDirty + 自动保存回放
            return data;
        }

        /// <summary>
        /// 从当前激活槽位读取数据，若键不存在则返回 default(T)
        /// <para>Load data from the active slot, returning default(T) if the key does not exist</para>
        /// </summary>
        public async UniTask<T> LoadOrDefaultAsync<T>(string key, CancellationToken ct = default)
        {
            return await LoadOrDefaultFromSlotAsync<T>(ActiveSlot, key, ct);
        }

        /// <summary>
        /// 从指定槽位读取数据，若键不存在则返回 default(T)
        /// <para>Load data from a specific slot, returning default(T) if the key does not exist</para>
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
        /// 从当前激活槽位读取数据，若不存在则创建默认值并自动保存
        /// <para>Load data from the active slot; if not found, create a default instance, save and return it</para>
        /// </summary>
        public async UniTask<T> LoadOrCreateAsync<T>(string key, CancellationToken ct = default) where T : new()
        {
            return await LoadOrCreateFromSlotAsync<T>(ActiveSlot, key, ct);
        }

        /// <summary>
        /// 从指定槽位读取数据，若不存在则创建默认值并自动保存
        /// <para>Load data from a specific slot; if not found, create a default instance, save and return it</para>
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

        #region Public — Operations

        /// <summary>
        /// 检查当前激活槽位中是否存在指定键的数据
        /// <para>Check whether a key exists in the active slot</para>
        /// </summary>
        public UniTask<bool> ExistsAsync(string key, CancellationToken ct = default)
            => _provider?.ExistsAsync(ActiveSlot, key, ct) ?? UniTask.FromResult(false);

        /// <summary>
        /// 从当前激活槽位删除指定键的数据
        /// <para>Delete a key from the active slot</para>
        /// </summary>
        public UniTask DeleteKeyAsync(string key, CancellationToken ct = default)
        {
            MarkClean(ActiveSlot, key);
            RemoveCachedData(ActiveSlot, key);
            return _provider?.DeleteAsync(ActiveSlot, key, ct) ?? UniTask.CompletedTask;
        }

        /// <summary>
        /// 列出当前激活槽位中所有的键名
        /// <para>List all key names in the active slot</para>
        /// </summary>
        public UniTask<string[]> ListKeysAsync(CancellationToken ct = default)
            => _provider.ListKeysAsync(ActiveSlot, ct);

        /// <summary>
        /// 获取当前激活槽位中所有存档数据的总字节数
        /// <para>Get the total byte size of all archive data in the active slot</para>
        /// </summary>
        public UniTask<long> GetSlotSizeAsync(CancellationToken ct = default)
            => _provider.GetSlotSizeAsync(ActiveSlot, ct);

        /// <summary>
        /// 强制刷新所有缓存到磁盘，并清空脏标记
        /// <para>Force-flush all caches to disk and clear dirty flags</para>
        /// </summary>
        public async UniTask FlushAsync(CancellationToken ct = default)
        {
            await FlushDirtyKeysAsync(ct);
            await _provider.FlushAsync(ct);
        }

        #endregion

        #region Public — Dirty

        /// <summary>
        /// 标记当前激活槽位的某条数据为脏（有未保存的变动）。自动保存会根据此标记决定是否写入。
        /// <para>Mark a key in the active slot as dirty. Auto-save uses this flag to decide whether to write.</para>
        /// </summary>
        public void MarkDirty(string key) => MarkDirty(ActiveSlot, key);

        /// <summary>
        /// 标记指定槽位的某条数据为脏（有未保存的变动）。
        /// 自动保存需要缓存中有对应数据引用 —— 请确保在 MarkDirty 之前已调用过 SaveAsync 或 LoadAsync，
        /// 或者使用 MarkDirtyWithData 同时传入当前数据。
        /// <para>Mark a key as dirty. Auto-save needs cached data — call SaveAsync/LoadAsync first,
        /// or use MarkDirtyWithData to provide current data directly.</para>
        /// </summary>
        public void MarkDirty(int slotId, string key)
        {
            if (!_dirtyKeys.ContainsKey(slotId))
                _dirtyKeys[slotId] = new HashSet<string>();
            _dirtyKeys[slotId].Add(key);
        }

        /// <summary>
        /// 标记当前激活槽位的某条数据为脏，同时更新缓存中的引用。
        /// 适用于"创建新数据对象后直接标记脏"的场景，无需先调用 SaveAsync。
        /// <para>Mark a key as dirty AND update the cached reference.
        /// Use this when you have a fresh data object that hasn't been saved yet.</para>
        /// </summary>
        public void MarkDirtyWithData<T>(string key, T data) => MarkDirtyWithData(ActiveSlot, key, data);

        /// <summary>
        /// 标记指定槽位的某条数据为脏，同时更新缓存中的引用。
        /// <para>Mark a key in a specific slot as dirty AND update the cached reference.</para>
        /// </summary>
        public void MarkDirtyWithData<T>(int slotId, string key, T data)
        {
            CacheData(slotId, key, data);
            MarkDirty(slotId, key);
        }

        #endregion

        #region Public — Auto Save

        /// <summary>
        /// 启动自动保存（已内置在 Init 中自动调用）。间隔由 ArchiveSettings.autoSaveIntervalSeconds 控制。
        /// <para>Start the auto-save loop. Interval controlled by ArchiveSettings.autoSaveIntervalSeconds.</para>
        /// </summary>
        public void StartAutoSave()
        {
            StopAutoSave();
            if (Settings.autoSaveIntervalSeconds <= 0) return;
            _autoSaveCts = new CancellationTokenSource();
            AutoSaveLoopAsync(_autoSaveCts.Token).Forget();
        }

        /// <summary>
        /// 停止自动保存循环
        /// <para>Stop the auto-save loop</para>
        /// </summary>
        public void StopAutoSave()
        {
            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = null;
        }

        #endregion
    }
}