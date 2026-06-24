/*
 * ================================================
 * Describe:      存档存储后端抽象接口。设计目标：用户可以自由替换存储引擎
 *                （文件加密 / SQLite / 云端同步），ArchiveManager 不感知底层实现。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 01:29:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Systems.Archive
{
    /// <summary>
    /// 存档存储后端抽象接口，实现此接口即可接入任意存储引擎。
    /// <para>Abstract storage backend interface. Implement to plug in any storage engine.
    /// Switch via <c>ArchiveManager.Instance.SetProvider(new YourProvider());</c></para>
    /// </summary>
    public interface IArchiveProvider
    {
        /// <summary>
        /// 持久化一条原始字节数据。由 ArchiveManager 完成 JSON 序列化后传入，Provider 自行决定存储方式。
        /// <para>Persist raw byte data. ArchiveManager handles JSON serialization; Provider decides how to store it.</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="key">数据键名<para>Data key name</para></param>
        /// <param name="data">已序列化的 JSON 字节数据<para>Serialized JSON bytes</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        UniTask SaveRawAsync(int slotId, string key, byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取一条原始字节数据，若 key 不存在则返回 null。由 ArchiveManager 负责 JSON 反序列化。
        /// <para>Load raw byte data, returns null if key not found. ArchiveManager handles deserialization.</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="key">数据键名<para>Data key name</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>原始字节，若 key 不存在则返回 null<para>Raw bytes, or null if key not found</para></returns>
        UniTask<byte[]> LoadRawAsync(int slotId, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查指定槽位中是否存在某条数据。
        /// <para>Check whether a key exists in the specified slot.</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="key">数据键名<para>Data key name</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>存在返回 true<para>true if the key exists</para></returns>
        UniTask<bool> ExistsAsync(int slotId, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除指定槽位中的某条数据。
        /// <para>Delete a key from the specified slot.</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="key">数据键名<para>Data key name</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        UniTask DeleteAsync(int slotId, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除整个槽位的所有数据。
        /// <para>Delete an entire slot and all its data.</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        UniTask DeleteSlotAsync(int slotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 列出指定槽位中所有的 key。
        /// <para>List all keys in the specified slot.</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>键名数组<para>Array of key names</para></returns>
        UniTask<string[]> ListKeysAsync(int slotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定槽位中所有数据的总大小（字节）。
        /// <para>Get the total byte size of all data in the specified slot.</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        /// <returns>总字节数<para>Total size in bytes</para></returns>
        UniTask<long> GetSlotSizeAsync(int slotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 强制刷新缓冲区（如文件系统 fsync、SQLite WAL checkpoint）。
        /// <para>Force-flush buffers (e.g. filesystem fsync, SQLite WAL checkpoint).</para>
        /// </summary>
        /// <param name="cancellationToken">取消令牌<para>Cancellation token</para></param>
        UniTask FlushAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 列出所有有效的存档槽位元数据。
        /// <para>List all valid archive slot metadata.</para>
        /// </summary>
        /// <returns>槽位元数据数组<para>Array of slot metadata</para></returns>
        ArchiveSlotMeta[] ListSlots();

        /// <summary>
        /// 保存槽位元数据（JSON 明文，不加密）。
        /// <para>Save slot metadata (plain JSON, not encrypted).</para>
        /// </summary>
        /// <param name="meta">槽位元数据<para>Slot metadata</para></param>
        /// <param name="ct">取消令牌<para>Cancellation token</para></param>
        UniTask SaveMetaAsync(ArchiveSlotMeta meta, CancellationToken ct = default);

        /// <summary>
        /// 读取槽位元数据，不存在则返回 null。
        /// <para>Load slot metadata, returns null if not found.</para>
        /// </summary>
        /// <param name="slotId">槽位编号<para>Slot ID</para></param>
        /// <param name="ct">取消令牌<para>Cancellation token</para></param>
        /// <returns>元数据，不存在则返回 null<para>Metadata, or null if not found</para></returns>
        UniTask<ArchiveSlotMeta?> LoadMetaAsync(int slotId, CancellationToken ct = default);
    }
}
