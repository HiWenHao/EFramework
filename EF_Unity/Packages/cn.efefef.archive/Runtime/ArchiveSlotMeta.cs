/*
 * ================================================
 * Describe:      存档槽位元数据。每个槽位有一个独立的元数据文件，
 *                用于存档选择界面的列表展示。该结构体不加密存储。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-25 01:34:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.Archive
{
    /// <summary>
    /// 存档槽位元数据，用于存档列表界面展示。不加密，以 JSON 格式存储在每个槽位目录下。
    /// <para>Archive slot metadata for save-selection UI. Stored as plain JSON in each slot directory.</para>
    /// <para>⚠ 注意：本结构体是可变的。通过属性或方法返回值访问时操作的是副本，
    /// 修改前需显式拷贝到局部变量（如 var copy = meta.Value; copy.SetModifiedNow();）。</para>
    /// </summary>
    [Serializable]
    public struct ArchiveSlotMeta
    {
        /// <summary>槽位编号（0-based）<para>Slot index (0-based)</para></summary>
        public int slotId;

        /// <summary>玩家自定义的存档名称<para>Custom save name</para></summary>
        public string slotName;

        /// <summary>游戏内进度描述<para>In-game progress description</para></summary>
        public string progressDescription;

        /// <summary>游戏总时长（秒）<para>Total play time in seconds</para></summary>
        public float playTimeSeconds;

        /// <summary>存档创建时间（DateTime.UtcNow.Ticks）<para>Creation timestamp as DateTime ticks</para></summary>
        public long createdAtTicks;

        /// <summary>存档最后修改时间（DateTime.UtcNow.Ticks）<para>Last modification timestamp as DateTime ticks</para></summary>
        public long lastModifiedAtTicks;

        /// <summary>存档数据格式版本号<para>Archive data format version</para></summary>
        public int dataVersion;

        /// <summary>该槽位所有加密数据的字节总数<para>Total byte size of all encrypted data in this slot</para></summary>
        public long totalSizeBytes;

        /// <summary>该槽位是否有效<para>Whether this slot is valid (not deleted)</para></summary>
        public bool isValid;

        /// <summary>存档创建时间（便利访问器）<para>Creation timestamp (convenience accessor)</para></summary>
        public readonly DateTime CreatedAt => new DateTime(createdAtTicks, DateTimeKind.Utc);

        /// <summary>存档最后修改时间（便利访问器）<para>Last modification timestamp (convenience accessor)</para></summary>
        public readonly DateTime LastModifiedAt => new DateTime(lastModifiedAtTicks, DateTimeKind.Utc);

        /// <summary>
        /// 设置创建时间为当前 UTC 时间
        /// <para>Set creation time to current UTC time</para>
        /// </summary>
        public void SetCreatedNow() => createdAtTicks = DateTime.UtcNow.Ticks;

        /// <summary>
        /// 设置最后修改时间为当前 UTC 时间
        /// <para>Set last-modified time to current UTC time</para>
        /// </summary>
        public void SetModifiedNow() => lastModifiedAtTicks = DateTime.UtcNow.Ticks;

        /// <summary>格式化后的游玩时长（如 "2h 30m"）<para>Formatted play time (e.g. "2h 30m")</para></summary>
        public readonly string PlayTimeFormatted
        {
            get
            {
                var ts = TimeSpan.FromSeconds(playTimeSeconds);
                return ts.TotalHours >= 1
                    ? $"{(int)ts.TotalHours}h {ts.Minutes}m"
                    : $"{ts.Minutes}m {ts.Seconds}s";
            }
        }

        /// <summary>格式化后的最后修改时间<para>Formatted last-modified timestamp</para></summary>
        public readonly string LastModifiedFormatted
            => LastModifiedAt.ToString("yyyy-MM-dd HH:mm");

        /// <summary>调试用的槽位概要字符串<para>Debug summary string</para></summary>
        public override readonly string ToString()
            => $"[Slot {slotId}] {slotName} | {progressDescription} | {PlayTimeFormatted}";
    }
}
