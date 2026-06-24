/*
 * ================================================
 * Describe:      存档存储后端抽象接口。设计目标：用户可以自由替换存储引擎
 *                （文件加密 / SQLite / 云端同步），ArchiveManager 不感知底层实现。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-24 23:19:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Threading;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Systems.Archive
{
    /// <summary>
    /// 存档存储后端抽象接口。
    /// <para>实现此接口即可接入任意存储引擎：
    /// <list type="bullet">
    /// <item><see cref="FileArchiveProvider"/> — 默认：AES加密 + 本地文件</item>
    /// <item><b>SqliteArchiveProvider</b> — 未来：SQLite数据库（支持条件查询、批量写入）</item>
    /// <item><b>CloudArchiveProvider</b> — 未来：云端同步（HTTP上传/下载）</item>
    /// <item><b>EncryptedGzipProvider</b> — 未来：压缩+加密组合</item>
    /// </list>
    /// </para>
    /// <para>切换方式：<c>ArchiveManager.Instance.SetProvider(new YourProvider());</c></para>
    /// </summary>
    public interface IArchiveProvider
    {
        /// <summary>
        /// 持久化一条数据。数据由 ArchiveManager 完成 JSON 序列化后传入原始字节。
        /// Provider 自行决定存储方式（文件 / SQLite / 加密 / 云端）。
        /// </summary>
        /// <param name="slotId">槽位编号</param>
        /// <param name="key">数据键名（如 "player_info"）</param>
        /// <param name="data">已序列化的 JSON 字节数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        UniTask SaveRawAsync(int slotId, string key, byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取一条数据。返回原始字节，若 key 不存在则返回 null。
        /// 由 ArchiveManager 负责 JSON 反序列化。
        /// </summary>
        /// <param name="slotId">槽位编号</param>
        /// <param name="key">数据键名</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>原始字节，若 key 不存在则返回 null</returns>
        UniTask<byte[]> LoadRawAsync(int slotId, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查指定槽位中是否存在某条数据。
        /// </summary>
        UniTask<bool> ExistsAsync(int slotId, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除指定槽位中的某条数据。
        /// </summary>
        UniTask DeleteAsync(int slotId, string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 删除整个槽位的所有数据。
        /// </summary>
        UniTask DeleteSlotAsync(int slotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 列出指定槽位中所有的 key。
        /// </summary>
        UniTask<string[]> ListKeysAsync(int slotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取指定槽位中所有数据的总大小（字节）。
        /// </summary>
        UniTask<long> GetSlotSizeAsync(int slotId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 强制刷新缓冲区（如文件系统 fsync、SQLite WAL checkpoint）。
        /// </summary>
        UniTask FlushAsync(CancellationToken cancellationToken = default);
    }
}
