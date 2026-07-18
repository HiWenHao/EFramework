/*
 * ================================================
 * Describe:      存档系统主管理器。负责槽位路由、序列化/反序列化、
 *                加密协调、自动保存调度。底层存储全委托给 IArchiveProvider。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 17:30:00
 * ScriptVersion: 0.1.2
 * Changelog:
 *   0.1.2  修复退出时 UniTask "Not yet completed" 错误:
 *          - 删除 OnApplicationQuit 里的 UniTask 投递(EFC.OnApplicationQuit 也会触发
 *            ISingleton.Quit,两者重叠会在 PlayerLoop 关闭后产生死锁)。
 *          - ISingleton.Quit 走真同步路径,直接调 provider.SaveRawSync(),
 *            不依赖 PlayerLoop,不会触发 UniTask 死锁。
 *   0.1.1  修复缓存淘汰静默丢数据 / Quit 同步等待死锁 /
 *          FindFreeSlotId 不防御残留 / struct 缓存过期值 /
 *          MarkClean 语义混乱 / Settings 校验 / 退出时优先走异步协程。
 *   0.1.0  首版
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
        // 注意：缓存中只保存引用类型 / 可变对象。值类型(struct)在 MarkDirty 时
        // 必须配合 MarkDirtyWithData 重新装箱，否则会读到旧快照。
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
            ValidateSettingsOrThrow();

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
            // ⚠ 关键设计:退出流程必须走"真同步"路径,绝不能用 UniTask。
            //
            // 原因:UniTask 完全在 PlayerLoop 上调度(Cysharp 官方说明),
            // 退出时 PlayerLoop 已停,任何 GetAwaiter().GetResult() 都会死锁主线程,
            // 而已投递的 UniTask 永远跑不完,后续 await 会抛 "Not yet completed"。
            //
            // EFC.OnApplicationQuit 也会触发 ISingleton.Quit —— 如果再叠一个
            // MonoBehaviour.OnApplicationQuit 投递 UniTask,会双重死锁。
            //
            // 解决方案:遍历 _dirtyKeys,直接调 provider.SaveRawSync() 同步写。
            // 同步路径用 FileOptions.WriteThrough + Flush(true),绕过系统缓存。
            StopAutoSave();
            FlushDirtyKeysSync();
        }

        // ⚠ 不再在 OnApplicationQuit 里投递 UniTask 刷盘任务。
        // 原因:EFC.OnApplicationQuit → ISingleton.Quit 才是真正的退出入口,
        // MonoBehaviour.OnApplicationQuit 投递的 UniTask 会与 ISingleton.Quit 重叠
        // 死锁。让 ISingleton.Quit 一个入口处理所有退出刷盘逻辑。
        //
        // 此处保留为空以避免基类在某些 Unity 版本(2022/2023)的 OnApplicationQuit
        // 反射调用造成未定义行为(基类 MonoSingleton 没有 OnApplicationQuit,
        // 但部分 Unity 版本会在 Editor 停止播放时显式调用所有 MonoBehaviour 的
        // OnApplicationQuit 一次,空方法不抛异常即可)。
        private void OnApplicationQuit()
        {
            // 留空:所有刷盘逻辑由 ISingleton.Quit 处理。
        }

        #endregion

        #region Private

        // 日志安全包装（防御 D 日志系统未初始化的 NRE，回退到 Unity Debug）
        private static void LogWarning(string msg)
        {
            try { D.Warning(msg); } catch { Debug.LogWarning(msg); }
        }

        private static void LogError(string msg)
        {
            try { D.Error(msg); } catch { Debug.LogError(msg); }
        }

        // 加载 ArchiveSettings 配置资产（从 Resources/Configs 读取，不存在则使用默认值）
        private void LoadSettings()
        {
            Settings = Resources.Load<ArchiveSettings>("Configs/ArchiveSettings");
            if (Settings != null) return;
            Settings = ScriptableObject.CreateInstance<ArchiveSettings>();
            LogWarning("[ArchiveManager] ArchiveSettings not found at Resources/Configs/ArchiveSettings. Using defaults.");
        }

        // 校验 Settings 的关键字段。无效值抛异常，防止后续静默错误。
        private void ValidateSettingsOrThrow()
        {
            if (Settings == null)
                throw new InvalidOperationException("[ArchiveManager] Settings is null after LoadSettings.");

            if (Settings.maxSlots < 1)
                throw new InvalidOperationException(
                    $"[ArchiveManager] maxSlots must be >= 1, got {Settings.maxSlots}.");

            if (Settings.autoSaveIntervalSeconds < 0)
                throw new InvalidOperationException(
                    $"[ArchiveManager] autoSaveIntervalSeconds must be >= 0, got {Settings.autoSaveIntervalSeconds}.");

            if (Settings.pbkdf2Iterations < 1000)
                throw new InvalidOperationException(
                    $"[ArchiveManager] pbkdf2Iterations must be >= 1000 (security floor), got {Settings.pbkdf2Iterations}.");

            if (Settings.maxBackupCount < 0)
                throw new InvalidOperationException(
                    $"[ArchiveManager] maxBackupCount must be >= 0, got {Settings.maxBackupCount}.");

            if (string.IsNullOrEmpty(Settings.fileStorageRoot))
                throw new InvalidOperationException("[ArchiveManager] fileStorageRoot cannot be empty.");

            if (string.IsNullOrEmpty(Settings.encryptionSalt))
                throw new InvalidOperationException("[ArchiveManager] encryptionSalt cannot be empty.");
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

        // 查找最小的空闲槽位编号。
        // 防御性策略：除了元数据中"isValid=true"占用的 ID 之外，
        // 还扫描物理目录 Slot_*，把"残留目录但无有效 meta"的 ID 也视为可回收槽位。
        // 残留目录会在 CreateSlotAsync 之后被覆盖写新 meta，不会造成文件污染。
        private async UniTask<int> FindFreeSlotIdAsync()
        {
            var takenIds = new HashSet<int>();
            var slots = await GetAllSlotsAsync();
            if (slots != null)
            {
                foreach (var s in slots)
                    takenIds.Add(s.slotId);
            }

            // 防御：扫描物理目录，回收"目录存在但元数据缺失或无效"的 ID
            if (_provider is FileArchiveProvider)
            {
                try
                {
                    var physicalIds = ((FileArchiveProvider)_provider).ScanSlotDirectoryIds();
                    foreach (var id in physicalIds)
                    {
                        if (!takenIds.Contains(id))
                        {
                            // 该槽位有残留目录但无有效 meta —— 作为可回收 ID 返回
                            return id;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWarning($"[ArchiveManager] Failed to scan physical slot directories: {ex.Message}. " +
                               "Falling back to metadata-only allocation.");
                }
            }

            for (int i = 0; i < Settings.maxSlots; i++)
                if (!takenIds.Contains(i)) return i;

            return -1;
        }

        // 清除指定 key 的脏标记（仅自动保存线程调用，公开给重载使用）
        private void MarkClean(int slotId, string key)
        {
            if (!_dirtyKeys.TryGetValue(slotId, out var set))
            {
                // 不警告：SaveToSlotAsync 会无副作用地调用 MarkClean（用户未标脏）
                return;
            }
            set.Remove(key);
            if (set.Count == 0) _dirtyKeys.Remove(slotId);
        }

        // 保存后更新槽位元数据（修改时间、数据版本、总大小）并缓存数据供自动保存使用
        private async UniTask UpdateSlotMetaAfterSave(int slotId)
        {
            var metaNullable = await _provider.LoadMetaAsync(slotId);

            ArchiveSlotMeta meta;
            if (metaNullable.HasValue)
            {
                meta = metaNullable.Value;
            }
            else
            {
                // meta 文件不存在（跳过 CreateSlotAsync 直接保存的场景），补建一条最小 meta
                meta = new ArchiveSlotMeta
                {
                    slotId = slotId,
                    slotName = $"Slot {slotId}",
                    progressDescription = string.Empty,
                    isValid = true
                };
                meta.SetCreatedNow();
            }

            meta.SetModifiedNow();
            meta.dataVersion = Settings.dataVersion;
            meta.totalSizeBytes = await _provider.GetSlotSizeAsync(slotId);
            await _provider.SaveMetaAsync(meta);
        }

        // 缓存最近一次保存的数据（供自动保存重放），超出上限时淘汰最早条目。
        // 关键修复：淘汰时如果该条目处于 dirty 状态，先同步刷盘，绝不静默丢弃。
        private void CacheData<T>(int slotId, string key, T data)
        {
            if (!_dataCache.TryGetValue(slotId, out var slotCache))
                _dataCache[slotId] = slotCache = new Dictionary<string, object>();
            slotCache[key] = data;

            EvictIfNeeded();
        }

        // 缓存条目数超过上限时，从最先找到的非空槽位中淘汰一条（近似 FIFO）。
        // 关键修复：淘汰前如果该 key 处于 dirty 状态，必须先把它刷到磁盘再移除缓存，
        // 否则自动保存线程 2 分钟后跑 FlushDirtyKeysAsync 会发现缓存中无数据，
        // 静默调用 MarkClean，造成玩家数据丢失。
        private void EvictIfNeeded()
        {
            int totalCount = 0;
            foreach (var sc in _dataCache.Values)
                totalCount += sc.Count;

            while (totalCount > MaxCachedEntries)
            {
                bool removed = false;
                foreach (var (slotId, slotCache) in _dataCache)
                {
                    if (slotCache.Count == 0) continue;

                    // 找到最旧的一条（FIFO 近似：字典遍历顺序）
                    string oldestKey = null;
                    foreach (var k in slotCache.Keys)
                    {
                        oldestKey = k;
                        break;
                    }
                    if (oldestKey == null) continue;

                    // 关键修复点：淘汰前检查 dirty
                    if (IsDirty(slotId, oldestKey))
                    {
                        // 使用 SaveRawSync 同步写,不依赖 PlayerLoop,不会触发 UniTask 死锁。
                        // 此路径在 PlayerLoop 正常运行期间被调用,GetAwaiter().GetResult() 也能用,
                        // 但统一用 SaveRawSync 更安全、行为更可预测。
                        try
                        {
                            if (slotCache.TryGetValue(oldestKey, out var pending))
                            {
                                string json = JsonConvert.SerializeObject(pending, _jsonSettings);
                                byte[] raw = Encoding.UTF8.GetBytes(json);
                                _provider.SaveRawSync(slotId, oldestKey, raw);
                                MarkClean(slotId, oldestKey);
                                LogWarning($"[ArchiveManager] Cache eviction for dirty key '{oldestKey}' in slot {slotId} " +
                                           "triggered a synchronous flush. Consider raising MaxCachedEntries to avoid this.");
                            }
                        }
                        catch (Exception ex)
                        {
                            // 刷盘失败：绝不能移除缓存中的 dirty 数据，宁可保留并警告
                            LogError($"[ArchiveManager] Failed to flush dirty key '{oldestKey}' in slot {slotId} " +
                                     $"during cache eviction: {ex.Message}. Keeping in cache to avoid data loss.");
                            // 跳出淘汰循环，让缓存略微超限
                            return;
                        }
                    }

                    slotCache.Remove(oldestKey);
                    totalCount--;
                    removed = true;
                    break;
                }
                // 防御：若所有槽位均为空但 totalCount 仍 > 0（极端情况），跳出避免死循环
                if (!removed) break;
            }
        }

        // 检查指定 key 是否处于 dirty 状态
        private bool IsDirty(int slotId, string key)
        {
            return _dirtyKeys.TryGetValue(slotId, out var set) && set.Contains(key);
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
                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(Settings.autoSaveIntervalSeconds),
                        cancellationToken: ct);
                }
                catch (OperationCanceledException)
                {
                    return; // 正常停止
                }

                if (Settings.autoSaveOnlyDirty && _dirtyKeys.Count == 0)
                    continue;

                // 先快照脏 key 列表，防止枚举过程中外部 MarkDirty 修改字典
                try
                {
                    await FlushDirtyKeysAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    LogError($"[ArchiveManager] Auto-save loop iteration failed: {ex.Message}");
                }
            }
        }

        // 遍历所有槽位的脏 key，从缓存取出数据重放保存。
        // 若缓存中没有数据（如仅调用 MarkDirty 而未先 Save/Load），跳过并保留 dirty 标记。
        // 关键修复：之前会在缓存缺失时静默 MarkClean 丢数据，现改为保留并要求用户补救。
        private async UniTask FlushDirtyKeysAsync(CancellationToken ct)
        {
            if (_dirtyKeys.Count == 0) return;

            // 快照：防止枚举过程中被 MarkDirty / MarkClean 修改
            var snapshot = new Dictionary<int, string[]>();
            foreach (var (slotId, keySet) in _dirtyKeys)
                snapshot[slotId] = new List<string>(keySet).ToArray();

            foreach (var (slotId, keys) in snapshot)
            {
                if (ct.IsCancellationRequested) return;

                if (!_dataCache.TryGetValue(slotId, out var slotCache))
                {
                    // 槽位缓存为空：所有属于该槽位的脏 key 都无法保存。
                    // 关键修复：保留 dirty 标记，绝不静默 MarkClean。
                    foreach (var key in keys)
                    {
                        LogError($"[ArchiveManager] Slot {slotId} key '{key}' marked dirty but slot cache is empty. " +
                                 "Auto-save SKIPPED and dirty flag PRESERVED to prevent data loss. " +
                                 "Use SaveToSlotAsync to provide data, or MarkDirtyWithData to provide current data.");
                    }
                    continue;
                }

                foreach (var key in keys)
                {
                    if (ct.IsCancellationRequested) return;

                    if (!slotCache.TryGetValue(key, out var cached))
                    {
                        // 关键修复：保留 dirty 标记，绝不静默 MarkClean。
                        LogError($"[ArchiveManager] Key '{key}' in slot {slotId} marked dirty but not in cache. " +
                                 "Auto-save SKIPPED and dirty flag PRESERVED to prevent data loss. " +
                                 "Use MarkDirtyWithData to provide current data.");
                        continue;
                    }

                    try
                    {
                        string json = JsonConvert.SerializeObject(cached, _jsonSettings);
                        byte[] raw = Encoding.UTF8.GetBytes(json);
                        await _provider.SaveRawAsync(slotId, key, raw, ct);
                        // 仅在自动保存路径调用 MarkClean（成功落盘后才清脏标记）
                        MarkClean(slotId, key);
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消时不要 MarkClean，让下次循环重试
                        return;
                    }
                    catch (Exception ex)
                    {
                        // 写入失败：保留 dirty 标记，下次循环重试
                        LogError($"[ArchiveManager] Auto-save of key '{key}' in slot {slotId} failed: {ex.Message}. " +
                                 "Dirty flag preserved for retry.");
                    }
                }
            }
        }

        // 同步刷盘:仅在 ISingleton.Quit 退出流程调用。
        // 关键设计:不走 UniTask,不依赖 PlayerLoop,直接调 provider.SaveRawSync()
        // 内部 FileStream + FileOptions.WriteThrough + Flush(true) 同步落盘。
        // 这是唯一不会触发 "Not yet completed" UniTask 死锁的退出路径。
        //
        // ⚠ 性能警告:PBKDF2 派生 10 万次迭代 + AES-256 加密 + 同步文件 I/O
        // 单个 key 大约 100-300ms;10 个 key 可能阻塞主线程 1-3 秒。
        // 但这是退出流程,可接受短时阻塞以保证数据安全。
        private void FlushDirtyKeysSync()
        {
            if (_provider == null) return;
            if (_dirtyKeys.Count == 0) return;

            int flushedCount = 0;
            int failedCount = 0;

            // 快照:防止枚举过程中被 MarkDirty / MarkClean 修改
            var snapshot = new List<KeyValuePair<int, string>>();
            foreach (var (slotId, keySet) in _dirtyKeys)
                foreach (var key in keySet)
                    snapshot.Add(new KeyValuePair<int, string>(slotId, key));

            foreach (var pair in snapshot)
            {
                int slotId = pair.Key;
                string key = pair.Value;

                if (!_dataCache.TryGetValue(slotId, out var slotCache) ||
                    !slotCache.TryGetValue(key, out var cached))
                {
                    // 缓存缺失:保留 dirty 标记,等下次进程启动时用户补救
                    LogError($"[ArchiveManager] Sync flush: key '{key}' in slot {slotId} not in cache. " +
                             "Dirty flag preserved.");
                    continue;
                }

                try
                {
                    string json = JsonConvert.SerializeObject(cached, _jsonSettings);
                    byte[] raw = Encoding.UTF8.GetBytes(json);
                    _provider.SaveRawSync(slotId, key, raw);
                    MarkClean(slotId, key);
                    flushedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    LogError($"[ArchiveManager] Sync flush of key '{key}' in slot {slotId} failed: {ex.Message}. " +
                             "Dirty flag preserved for next session.");
                }
            }

            if (flushedCount > 0 || failedCount > 0)
            {
                Debug.Log($"[ArchiveManager] Shutdown sync flush: {flushedCount} keys saved, {failedCount} failed.");
            }

            // 注意:不在 finally 里清 _dataCache —— FlushAsync 公开 API 也用它,
            // 仅在 quit 流程中清掉 dirty 标记即可。
            // 实际在 ISingleton.Quit 之后 GameObject 会被 EFC 销毁,字段随对象释放。
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
            if (string.IsNullOrEmpty(slotName))
                throw new ArgumentException("Slot name cannot be empty.", nameof(slotName));

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
            if (slotId < 0 || slotId >= Settings.maxSlots)
                throw new ArgumentOutOfRangeException(nameof(slotId),
                    $"slotId must be in [0, {Settings.maxSlots - 1}], got {slotId}.");

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
            if (slotId < 0 || slotId >= Settings.maxSlots)
                throw new ArgumentOutOfRangeException(nameof(slotId),
                    $"slotId must be in [0, {Settings.maxSlots - 1}], got {slotId}.");
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be empty.", nameof(key));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

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
                LogWarning($"[ArchiveManager] Meta update for '{key}' in slot {slotId} failed: {ex.Message}. Data is safe, meta will be corrected on next save.");
            }

            // 关键修复：不再在 SaveToSlotAsync 末尾调用 MarkClean。
            // 语义修正：MarkClean 只在自动保存线程把脏数据成功落盘后才调用。
            // 显式 SaveToSlotAsync 是用户主动行为，与脏标记无关。
            // 仍然清除可能存在的脏标记：用户既 Save 又 MarkDirty 时，避免自动保存重复写一次
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
            if (slotId < 0 || slotId >= Settings.maxSlots)
                throw new ArgumentOutOfRangeException(nameof(slotId),
                    $"slotId must be in [0, {Settings.maxSlots - 1}], got {slotId}.");
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be empty.", nameof(key));

            byte[] raw = await _provider.LoadRawAsync(slotId, key, ct);
            if (raw == null)
                throw new KeyNotFoundException($"Archive key '{key}' not found in slot {slotId}.");

            string json = Encoding.UTF8.GetString(raw);
            T data = JsonConvert.DeserializeObject<T>(json, _jsonSettings);

            // 关键修复：对值类型(struct)，不缓存。避免 MarkDirty 后读到的还是旧快照。
            // 引用类型(class)才缓存到 _dataCache，以便 MarkDirty + 自动保存重放。
            if (data != null && !typeof(T).IsValueType)
                CacheData(slotId, key, data);
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
            => _provider?.ListKeysAsync(ActiveSlot, ct) ?? UniTask.FromResult(Array.Empty<string>());

        /// <summary>
        /// 获取当前激活槽位中所有存档数据的总字节数
        /// <para>Get the total byte size of all archive data in the active slot</para>
        /// </summary>
        public UniTask<long> GetSlotSizeAsync(CancellationToken ct = default)
            => _provider?.GetSlotSizeAsync(ActiveSlot, ct) ?? UniTask.FromResult(0L);

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
        /// <para>⚠ 对值类型(struct)，必须配合 MarkDirtyWithData 使用 —— 否则自动保存会读到旧快照。</para>
        /// </summary>
        public void MarkDirty(string key) => MarkDirty(ActiveSlot, key);

        /// <summary>
        /// 标记指定槽位的某条数据为脏（有未保存的变动）。
        /// 自动保存需要缓存中有对应数据引用 —— 请确保在 MarkDirty 之前已调用过 SaveAsync 或 LoadAsync，
        /// 或者使用 MarkDirtyWithData 同时传入当前数据。
        /// <para>Mark a key as dirty. Auto-save needs cached data — call SaveAsync/LoadAsync first,
        /// or use MarkDirtyWithData to provide current data directly.</para>
        /// <para>⚠ 对值类型(struct)，必须使用 MarkDirtyWithData —— struct 在 CacheData 中会装箱为副本，
        /// 后续修改原变量不会反映到缓存中。</para>
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
            // 即便是 struct，也保存用户传入的最新值（这样 struct 也能正确走自动保存）
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
