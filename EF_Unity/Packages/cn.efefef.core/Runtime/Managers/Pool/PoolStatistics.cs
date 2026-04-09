/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 13:52:29
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-10 13:52:29
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// 对象池统计信息
    /// </summary>
    public struct PoolStatistics
    {
        /// <summary>
        /// 池名称
        /// </summary>
        public string PoolName;

        /// <summary>
        /// 可用数量
        /// </summary>
        public int AvailableCount;

        /// <summary>
        /// 已激活数量
        /// </summary>
        public int ActiveCount;

        /// <summary>
        /// 已创建数量
        /// </summary>
        public int TotalCreated;

        /// <summary>
        /// 已回收数量
        /// </summary>
        public int TotalRecycled;

        /// <summary>
        /// 最大激活数量
        /// </summary>
        public int PeakActiveCount;

        /// <summary>
        /// 池平均持有时间
        /// </summary>
        public TimeSpan AverageHoldTime;

        /// <summary>
        /// 池已被初始化
        /// </summary>
        public bool IsInitialized;

        public override string ToString()
        {
            return $"[{PoolName}] 可用:{AvailableCount}, 使用中:{ActiveCount}, 总计:{TotalCreated}, 峰值:{PeakActiveCount}";
        }
    }
}