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

namespace EasyFramework.Managers.Utility
{
    /// <summary>
    /// 时间管理器管理的时间事件
    /// </summary>
    public class TimeEvent
	{
		/// <summary> 时间事件的ID </summary>
		public int Id;
        /// <summary> 已经执行完成 </summary>
        public bool IsCompleted;
        /// <summary> 已经过去的时间 </summary>
        public float PassedTime;
        /// <summary> 第一次延时时间 </summary>
        public float DelayTime;
		/// <summary> 循环时间 </summary>
		public float CycleTime;
        /// <summary> 循环次数 </summary>
        public int CycleCount;
		/// <summary> 事件结束后的回调 </summary>
		public Action EndCallback;
    }
}
