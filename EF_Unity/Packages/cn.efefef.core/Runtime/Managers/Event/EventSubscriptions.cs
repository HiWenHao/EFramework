/*
 * ================================================
 * Describe:      内部订阅容器，同时管理同步和异步处理器
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 17:13:40
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 17:13:40
 * ScriptVersion: 0.1
 * ===============================================
 */

using System.Collections.Generic;

namespace EasyFramework.Managers.Event
{
    /// <summary>
    /// 内部订阅容器，同时管理同步和异步处理器
    /// <para>Internal subscription container, managing both sync and async handlers</para>
    /// </summary>
    internal class EventSubscriptions
    {
        public readonly List<EventHandler> SyncHandlers = new();
        public readonly List<EventAsyncHandler> AsyncHandlers = new();

        /// <summary>
        /// 是否没有处理器
        /// <para>Whether no handlers left</para>
        /// </summary>
        public bool IsEmpty => SyncHandlers.Count == 0 && AsyncHandlers.Count == 0;

        /// <summary>
        /// 清空所有处理器
        /// <para>Clear all handlers</para>
        /// </summary>
        public void Clear()
        {
            SyncHandlers.Clear();
            AsyncHandlers.Clear();
        }
    }

    /// <summary>
    /// 处理器快照：锁内拷贝、锁外执行，避免迭代中集合被修改
    /// <para>Handler snapshot: copied under lock and executed outside, avoiding in-iteration collection mutation</para>
    /// </summary>
    internal sealed class EventHandlersSnapshot
    {
        public readonly EventHandler[] Sync;
        public readonly EventAsyncHandler[] Async;

        public EventHandlersSnapshot(EventHandler[] sync, EventAsyncHandler[] asyncHandlers)
        {
            Sync = sync;
            Async = asyncHandlers;
        }
    }
}