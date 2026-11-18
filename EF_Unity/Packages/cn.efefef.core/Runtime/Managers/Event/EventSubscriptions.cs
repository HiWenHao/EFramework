/*
 * ================================================
 * Describe:      泛型订阅容器与处理器快照
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 17:13:40
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-18 14:02:00
 * ScriptVersion: 0.3
 * ===============================================
 */

using System;
using System.Collections.Generic;

namespace EasyFramework.Managers.Event
{
    /// <summary>
    /// 类型无关的订阅容器接口，供字典持有与跨类型批量操作
    /// <para>Type-agnostic subscription container interface for dictionary storage and cross-type bulk operations</para>
    /// </summary>
    internal interface IEventSubscriptions
    {
        /// <summary>是否没有处理器<para>Whether no handlers left</para></summary>
        bool IsEmpty { get; }

        /// <summary>清空所有处理器并使令牌失效<para>Clear all handlers and invalidate tokens</para></summary>
        void Clear();

        /// <summary>使所有处理器令牌失效<para>Invalidate all handler tokens</para></summary>
        void InvalidateAll();

        /// <summary>移除指定分组的处理器，返回移除数量<para>Remove handlers of a group, returns removed count</para></summary>
        int RemoveByGroup(string group);
    }

    /// <summary>
    /// 泛型订阅容器，同时管理同步和异步处理器
    /// <para>Generic subscription container, managing both sync and async handlers</para>
    /// </summary>
    /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
    internal sealed class EventSubscriptions<T> : IEventSubscriptions where T : struct
    {
        public readonly List<EventHandler<T>> SyncHandlers = new();
        public readonly List<EventAsyncHandler<T>> AsyncHandlers = new();

        /// <summary>
        /// 是否没有处理器
        /// <para>Whether no handlers left</para>
        /// </summary>
        public bool IsEmpty => SyncHandlers.Count == 0 && AsyncHandlers.Count == 0;

        /// <summary>
        /// 清空所有处理器并使令牌失效
        /// <para>Clear all handlers and invalidate tokens</para>
        /// </summary>
        public void Clear()
        {
            InvalidateAll();
            SyncHandlers.Clear();
            AsyncHandlers.Clear();
        }

        /// <summary>
        /// 使所有处理器令牌失效
        /// <para>Invalidate all handler tokens</para>
        /// </summary>
        public void InvalidateAll()
        {
            var sync = SyncHandlers;
            for (int i = 0; i < sync.Count; i++)
                sync[i].Token?.Invalidate();
            var asyncList = AsyncHandlers;
            for (int i = 0; i < asyncList.Count; i++)
                asyncList[i].Token?.Invalidate();
        }

        /// <summary>
        /// 移除指定分组的处理器，返回移除数量
        /// <para>Remove handlers of a group, returns removed count</para>
        /// </summary>
        public int RemoveByGroup(string group)
        {
            int removed = 0;
            var sync = SyncHandlers;
            for (int i = sync.Count - 1; i >= 0; i--)
            {
                if (sync[i].Group != group)
                    continue;
                sync[i].Token?.Invalidate();
                sync.RemoveAt(i);
                removed++;
            }

            var asyncList = AsyncHandlers;
            for (int i = asyncList.Count - 1; i >= 0; i--)
            {
                if (asyncList[i].Group != group)
                    continue;
                asyncList[i].Token?.Invalidate();
                asyncList.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        /// <summary>
        /// 用本容器数据填充快照，容量不足时按 2 倍扩容
        /// <para>Fill a snapshot from this container, doubling capacity when insufficient</para>
        /// </summary>
        public void FillSnapshot(EventHandlersSnapshot<T> snapshot)
        {
            snapshot.FillFrom(SyncHandlers, AsyncHandlers);
        }
    }

    /// <summary>
    /// 泛型可复用处理器快照：锁内填充、锁外执行，内部数组只增不减以消除重复分配
    /// <para>Generic reusable handler snapshot: filled under lock, executed outside; internal arrays only grow</para>
    /// </summary>
    /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
    internal sealed class EventHandlersSnapshot<T> where T : struct
    {
        private EventHandler<T>[] _sync = new EventHandler<T>[4];
        private EventAsyncHandler<T>[] _async = new EventAsyncHandler<T>[4];

        // 同步处理器数组
        public EventHandler<T>[] Sync => _sync;
        // 异步处理器数组
        public EventAsyncHandler<T>[] Async => _async;
        // 有效同步处理器数量
        public int SyncCount { get; private set; }
        // 有效异步处理器数量
        public int AsyncCount { get; private set; }

        // 用处理器列表填充快照，容量不足时按 2 倍扩容
        public void FillFrom(List<EventHandler<T>> sync, List<EventAsyncHandler<T>> asyncList)
        {
            SyncCount = sync.Count;
            AsyncCount = asyncList.Count;

            if (_sync.Length < SyncCount)
                _sync = new EventHandler<T>[GetCapacity(SyncCount)];
            if (_async.Length < AsyncCount)
                _async = new EventAsyncHandler<T>[GetCapacity(AsyncCount)];

            for (int i = 0; i < SyncCount; i++)
                _sync[i] = sync[i];
            for (int i = 0; i < AsyncCount; i++)
                _async[i] = asyncList[i];
        }

        // 归还池前清理元素引用，避免快照持有处理器造成泄漏，但保留数组容量
        public void Reset()
        {
            Array.Clear(_sync, 0, _sync.Length);
            Array.Clear(_async, 0, _async.Length);
            SyncCount = 0;
            AsyncCount = 0;
        }

        // 计算不小于 required 的 2 次幂容量
        private static int GetCapacity(int required)
        {
            int capacity = 4;
            while (capacity < required)
                capacity <<= 1;
            return capacity;
        }
    }

    /// <summary>
    /// 池化的 UniTask 列表类型，作为独立池键避免与其它 List 冲突
    /// <para>Pooled UniTask list type, used as a distinct pool key</para>
    /// </summary>
    internal sealed class UniTaskList : List<Cysharp.Threading.Tasks.UniTask>
    {
    }

    /// <summary>
    /// 池化的 object 列表类型，用于延迟事件批量发布
    /// <para>Pooled object list type, used for delayed event batch dispatch</para>
    /// </summary>
    internal sealed class EventObjectList : List<object>
    {
    }

    /// <summary>
    /// 池化的 Type 列表类型，用于分组批量取消
    /// <para>Pooled Type list type, used for group batch unsubscription</para>
    /// </summary>
    internal sealed class EventTypeList : List<System.Type>
    {
    }
}
