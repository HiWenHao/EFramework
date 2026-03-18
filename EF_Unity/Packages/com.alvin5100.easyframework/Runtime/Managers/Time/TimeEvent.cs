/*
 * ================================================
 * Describe:      This script is used to record the time event.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2024-02-04 11:25:47
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-02-04 11:25:47
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Managers.Timer
{
    /// <summary>
    /// 时间管理器管理的时间事件
    /// </summary>
    public class TimeEvent : ITimeEvent
    {
        public int Id { get; set; }
        public bool IsCompleted { get; set; }
        public float PassedTime { get; set; }
        public float DelayTime { get; set; }
        public float CycleTime { get; set; }
        public int CycleCount { get; set; }
        public Action<bool> EndCallback { get; set; }
    }
}
