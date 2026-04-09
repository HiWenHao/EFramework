/*
 * ================================================
 * Describe:      This script is used to standardized time events.
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-02 16:15:50
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-02 16:15:50
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Managers.Timer
{
    /// <summary>
    /// 时间事件
    /// </summary>
    internal interface ITimeEvent
    {
        /// <summary>
        /// 时间事件的ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 已经执行完成
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// 已经过去的时间
        /// </summary>
        public float PassedTime { get; set; }

        /// <summary>
        /// 第一次延时时间
        /// </summary>
        public float DelayTime { get; set; }

        /// <summary>
        /// 循环时间
        /// </summary>
        public float CycleTime { get; set; }

        /// <summary>
        /// 循环次数
        /// </summary>
        public int CycleCount { get; set; }

        /// <summary>
        /// 事件结束后的回调
        /// </summary>
        public Action<bool> EndCallback { get; set; }
    }
}