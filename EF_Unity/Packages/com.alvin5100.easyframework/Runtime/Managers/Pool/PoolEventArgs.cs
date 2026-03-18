/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100(Wang)
 * CreationTime:  2025-12-10 13:50:33
 * ModifyAuthor:  Alvin5100(Wang)
 * ModifyTime:    2025-12-10 13:50:33
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Managers.Pool
{
    /// <summary>
    /// 对象池事件参数
    /// </summary>
    public class PoolEventArgs : EventArgs, IDisposable
    {
        private PoolEventArgs()
        {
        }

        /// <summary>
        /// 池名称
        /// </summary>
        public string PoolName { get; set; }

        /// <summary>
        /// 事件类型
        /// </summary>
        public PoolEventType EventType { get; set; }

        /// <summary>
        /// 对象
        /// </summary>
        public object Item { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        public static PoolEventArgs Create(string poolName, PoolEventType eventType, object item = null)
        {
            PoolEventArgs eventArgs = EF.Pool.Get<PoolEventArgs>();
            eventArgs.PoolName = poolName;
            eventArgs.EventType = eventType;
            eventArgs.Item = item;
            eventArgs.Timestamp = DateTime.Now;
            return eventArgs;
        }

        public void Dispose()
        {
            Item = null;
            PoolName = null;
            EventType = default;
            Timestamp = default;
            EF.Pool.Recycle(this);
        }
    }
}
