/*
 * ================================================
 * Describe:      存档系统主管理器。负责槽位路由、序列化/反序列化、
 *                加密协调、自动保存调度。底层存储全委托给 IArchiveProvider。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-24 23:19:00
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
    [Manager(Order = 99100)]
    public class ArchiveManager : MonoSingleton<ArchiveManager>, ISingleton
    {
        private IArchiveProvider _provider;
        private ArchiveSettings _settings;
        private readonly Dictionary<int, HashSet<string>> _dirtyKeys = new();
        private CancellationTokenSource _autoSaveCts;
        private JsonSerializerSettings _jsonSettings;

        /// <summary>当前激活的槽位编号</summary>
        public int ActiveSlot { get; set; } = 0;

        /// <summary>存档设置引用</summary>
        public ArchiveSettings Settings => _settings;

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

        public void SetProvider(IArchiveProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        private IArchiveProvider CreateProvider()
        {
            if (!string.IsNullOrEmpty(_settings.providerTypeName))
            {
                var type = Type.GetType(_settings.providerTypeName, throwOnError: false);
                if (type != null)
                {
                    var instance = Activator.CreateInstance(type, _settings) as IArchiveProvider;
                    if (instance != null)
                        return instance;
                }
            }

            return new FileArchiveProvider(_settings);
        }

        #endregion

        #region Slot Management

        public async UniTask<int> CreateSlotAsync(string slotName, CancellationToken ct = default)
        {
            int slotId = FindFreeSlotId();
            var meta = new ArchiveSlotMeta
            {
                slotId = slotId,
                slotName = slotName,
                progressDescription = "New Game",
                createdAt = DateTime.Now,
                lastModifiedAt = DateTime.Now,
                dataVersion = _settings.dataVersion,
                isValid = true
            };

            if (_provider is FileArchiveProvider fileProvider)
                await fileProvider.SaveMetaAsync(meta, ct);

            ActiveSlot = slotId;
            return slotId;
        }

        public ArchiveSlotMeta[] GetAllSlots()
        {
            if (_provider is FileArchiveProvider fileProvider)
                return fileProvider.ListSlots();

            return Array.Empty<ArchiveSlotMeta>();
        }

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
            return -1;
        }

        #endregion

        #region Save & Load

        public async UniTask SaveAsync<T>(string key, T data, CancellationToken ct = default)
        {
            await SaveToSlotAsync(ActiveSlot, key, data, ct);
        }

        public async UniTask SaveToSlotAsync<T>(int slotId, string key, T data, CancellationToken ct = default)
        {
            string json = JsonConvert.SerializeObject(data, _jsonSettings);
            byte[] raw = Encoding.UTF8.GetBytes(json);

            await _provider.SaveRawAsync(slotId, key, raw, ct);

            MarkClean(slotId, key);
            await UpdateSlotMetaAfterSave(slotId);
        }

        public async UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default)
        {
            return await LoadFromSlotAsync<T>(ActiveSlot, key, ct);
        }

        public async UniTask<T> LoadFromSlotAsync<T>(int slotId, string key, CancellationToken ct = default)
        {
            byte[] raw = await _provider.LoadRawAsync(slotId, key, ct);
            if (raw == null)
                throw new KeyNotFoundException($"Archive key '{key}' not found in slot {slotId}.");

            string json = Encoding.UTF8.GetString(raw);
            return JsonConvert.DeserializeObject<T>(json, _jsonSettings);
        }

        public async UniTask<T> LoadOrDefaultAsync<T>(string key, CancellationToken ct = default)
        {
            return await LoadOrDefaultFromSlotAsync<T>(ActiveSlot, key, ct);
        }

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

        public async UniTask<T> LoadOrCreateAsync<T>(string key, CancellationToken ct = default)
            where T : new()
        {
            return await LoadOrCreateFromSlotAsync<T>(ActiveSlot, key, ct);
        }

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

        public UniTask<bool> ExistsAsync(string key, CancellationToken ct = default)
            => _provider.ExistsAsync(ActiveSlot, key, ct);

        public UniTask DeleteKeyAsync(string key, CancellationToken ct = default)
        {
            MarkClean(ActiveSlot, key);
            return _provider.DeleteAsync(ActiveSlot, key, ct);
        }

        public UniTask<string[]> ListKeysAsync(CancellationToken ct = default)
            => _provider.ListKeysAsync(ActiveSlot, ct);

        public UniTask<long> GetSlotSizeAsync(CancellationToken ct = default)
            => _provider.GetSlotSizeAsync(ActiveSlot, ct);

        public async UniTask FlushAsync(CancellationToken ct = default)
        {
            await _provider.FlushAsync(ct);
            _dirtyKeys.Clear();
        }

        #endregion

        #region Dirty Marking

        public void MarkDirty(string key)
        {
            MarkDirty(ActiveSlot, key);
        }

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

        public void StartAutoSave()
        {
            StopAutoSave();

            if (_settings.autoSaveIntervalSeconds <= 0)
                return;

            _autoSaveCts = new CancellationTokenSource();
            AutoSaveLoopAsync(_autoSaveCts.Token).Forget();
        }

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

                if (_settings.autoSaveOnlyDirty && _dirtyKeys.Count == 0)
                    continue;

                await FlushAsync(ct);
            }
        }

        #endregion

        #region Internal Helpers

        private void LoadSettings()
        {
            _settings = Resources.Load<ArchiveSettings>("Configs/ArchiveSettings");
            if (_settings == null)
            {
                _settings = ScriptableObject.CreateInstance<ArchiveSettings>();
                D.Warning("[ArchiveManager] ArchiveSettings not found at Resources/Configs/ArchiveSettings. Using defaults.");
            }
        }

        private async UniTask UpdateSlotMetaAfterSave(int slotId)
        {
            if (_provider is FileArchiveProvider fileProvider)
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
