/*
 * ================================================
 * Describe:      存档槽位元数据。每个槽位有一个独立的元数据文件，
 *                用于存档选择界面的列表展示。该结构体不加密存储。
 * Author:        Alvin5100
 * CreationTime:  2026-06-24 22:25:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-06-24 23:19:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.Archive
{
    /// <summary>
    /// 存档槽位元数据 — 用于存档列表界面展示。
    /// <para>该结构体不加密，直接以 JSON 格式存储在每个槽位目录下。</para>
    /// </summary>
    [Serializable]
    public struct ArchiveSlotMeta
    {
        /// <summary>槽位编号（0-based）</summary>
        public int slotId;

        /// <summary>玩家自定义的存档名称</summary>
        public string slotName;

        /// <summary>游戏内进度描述（如 "第3章 · 火焰神殿"）</summary>
        public string progressDescription;

        /// <summary>游戏总时长（秒）</summary>
        public float playTimeSeconds;

        /// <summary>存档创建时间</summary>
        public DateTime createdAt;

        /// <summary>存档最后修改时间</summary>
        public DateTime lastModifiedAt;

        /// <summary>存档数据版本号（用于热更新兼容）</summary>
        public int dataVersion;

        /// <summary>该槽位所有加密数据的字节总数</summary>
        public long totalSizeBytes;

        /// <summary>这个槽位是否有效（未被删除）</summary>
        public bool isValid;

        /// <summary>格式化后的游戏时长文本</summary>
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

        /// <summary>格式化后的最后修改时间</summary>
        public readonly string LastModifiedFormatted
            => lastModifiedAt.ToString("yyyy-MM-dd HH:mm");

        public override readonly string ToString()
            => $"[Slot {slotId}] {slotName} | {progressDescription} | {PlayTimeFormatted}";
    }
}
