/*
 * ================================================
 * Describe:      Event service implementation - type-safe, group support, delayed dispatch, async awaiting
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 15:49:19
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-18 14:02:00
 * ScriptVersion: 0.3
 * ===============================================
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Events;
using UnityEngine;

namespace EasyFramework
{
    /// <summary>
    /// 事件服务实现——类型安全、支持分组、延迟发送、异步等待
    /// <para>Event service implementation - type-safe, group support, delayed dispatch, async awaiting</para>
    /// </summary>
    [Manager(Order = 99800)]
    [Dependency(typeof(PoolManager))]
    public sealed class EventManager : MonoSingleton<EventManager>, ISingleton
    {
        #region 常量 Constants

        // UniTask 列表池 idle 上限
        private const int TaskListPoolMax = 32;
        // 延迟事件列表池 idle 上限
        private const int ObjectListPoolMax = 32;
        // 类型列表池 idle 上限
        private const int TypeListPoolMax = 8;
        // 快照静态池 idle 上限（按事件类型各自一份）
        private const int SnapshotPoolMax = 32;
        // 错误日志默认开关
        private const bool DefaultOpenDebug = true;
        // 日志前缀
        private const string LogPrefix = "[ EventSystem ]";
        // 同步处理器异常标签
        private const string SyncHandlerErrorTag = LogPrefix + " Error in sync handler: ";
        // 异步处理器异常标签
        private const string AsyncHandlerErrorTag = LogPrefix + " Error in async handler: ";

        #endregion

        #region 属性 Properties

        /// <summary>
        /// 是否输出处理器异常日志
        /// <para>Whether to output handler exception logs</para>
        /// </summary>
        public bool OpenDebug { get; private set; }

        #endregion

        #region 字段 Fields

        // 统一锁：保护 _subscriptions 与 _delayedEvents 的所有读写
        private readonly object _gate = new object();
        // 每种事件类型的订阅容器（类型擦除为接口，取出时强转回 EventSubscriptions<T>）
        private Dictionary<Type, IEventSubscriptions> _subscriptions;
        // 延迟事件队列（受 _gate 保护）
        private Queue<object> _delayedEvents;
        // 延迟事件的分发器缓存：object → 强类型 Publish<T>，避免 Flush 时反射
        private ConcurrentDictionary<Type, Action<object>> _dispatchers;
        // 依赖的对象池系统引用
        private PoolManager _pools;

        #endregion

        #region 嵌套类型 Nested Types

        // 泛型快照静态池：快照是泛型类型，无法用非泛型 ObjectPool 统一注册，故每种 T 自带一份无锁栈
        private static class SnapshotPool<T> where T : struct
        {
            private static readonly ConcurrentStack<EventHandlersSnapshot<T>> Stack = new();

            public static EventHandlersSnapshot<T> Rent()
            {
                return Stack.TryPop(out var snapshot) ? snapshot : new EventHandlersSnapshot<T>();
            }

            public static void Return(EventHandlersSnapshot<T> snapshot)
            {
                snapshot.Reset();
                if (Stack.Count < SnapshotPoolMax)
                    Stack.Push(snapshot);
            }
        }

        #endregion

        #region 生命周期 Lifecycle

        void ISingleton.Init()
        {
            lock (_gate)
            {
                if (_subscriptions != null)
                    return;
                OpenDebug = DefaultOpenDebug;
                _subscriptions = new Dictionary<Type, IEventSubscriptions>();
                _delayedEvents = new Queue<object>();
                _dispatchers = new ConcurrentDictionary<Type, Action<object>>();

                _pools = PoolManager.Instance;
                CreatePools();
            }
        }

        void ISingleton.Quit()
        {
            lock (_gate)
            {
                if (_subscriptions != null)
                {
                    foreach (var kvp in _subscriptions)
                        kvp.Value.InvalidateAll();
                    _subscriptions.Clear();
                }

                _delayedEvents?.Clear();
                _dispatchers?.Clear();
                DestroyPools();

                _subscriptions = null;
                _delayedEvents = null;
                _dispatchers = null;
                _pools = null;
            }
        }

        // 注册事件系统使用的专用对象池
        private void CreatePools()
        {
            _pools.CreateObjectPool<UniTaskList>(TaskListPoolMax,
                () => new UniTaskList(), list => list.Clear());
            _pools.CreateObjectPool<EventObjectList>(ObjectListPoolMax,
                () => new EventObjectList(), list => list.Clear());
            _pools.CreateObjectPool<EventTypeList>(TypeListPoolMax,
                () => new EventTypeList(), list => list.Clear());
        }

        // 销毁事件系统注册的对象池
        private void DestroyPools()
        {
            _pools.DestroyObjectPool<UniTaskList>();
            _pools.DestroyObjectPool<EventObjectList>();
            _pools.DestroyObjectPool<EventTypeList>();
        }

        #endregion

        #region 内部方法 Internal Methods

        // 获取或创建事件类型对应的泛型订阅容器（调用方需持有 _gate）
        private EventSubscriptions<T> GetOrCreateSubscriptions<T>(Type eventType) where T : struct
        {
            if (_subscriptions.TryGetValue(eventType, out var boxed))
                return (EventSubscriptions<T>)boxed;

            var subs = new EventSubscriptions<T>();
            _subscriptions[eventType] = subs;
            return subs;
        }

        // 获取事件类型对应的泛型订阅容器快照容器（调用方需持有 _gate），无订阅返回 null
        private EventSubscriptions<T> GetSubscriptions<T>(Type eventType) where T : struct
        {
            if (_subscriptions == null || !_subscriptions.TryGetValue(eventType, out var boxed))
                return null;
            return (EventSubscriptions<T>)boxed;
        }

        // 从泛型容器借出并填充快照（调用方需持有 _gate）
        private EventHandlersSnapshot<T> RentSnapshot<T>(EventSubscriptions<T> subs) where T : struct
        {
            var snapshot = SnapshotPool<T>.Rent();
            subs.FillSnapshot(snapshot);
            return snapshot;
        }

        // 安全执行所有同步处理器（传入泛型快照，锁外执行，零装箱）
        private void ExecuteSyncHandlers<T>(EventHandlersSnapshot<T> snapshot, T eventData) where T : struct
        {
            var handlers = snapshot.Sync;
            int count = snapshot.SyncCount;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    handlers[i].Action?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Error($"{SyncHandlerErrorTag}{e}");
                }
            }
        }

        // 异步处理器：Fire-and-Forget，内部捕捉异常（零装箱）
        private async UniTaskVoid FireAsync<T>(Func<T, UniTask> func, T eventData) where T : struct
        {
            try
            {
                await func(eventData);
            }
            catch (Exception e)
            {
                Error($"{AsyncHandlerErrorTag}{e}");
            }
        }

        // 带 CancellationToken 的异步处理器：取消静默完成不外抛（零装箱）
        private async UniTask HandleAsync<T>(Func<T, UniTask> func, T eventData, CancellationToken token)
            where T : struct
        {
            if (token.IsCancellationRequested)
                return;

            try
            {
                await func(eventData).AttachExternalCancellation(token);
            }
            catch (OperationCanceledException)
            {
                // 取消是正常行为，静默完成
            }
            catch (Exception e)
            {
                Error($"{AsyncHandlerErrorTag}{e}");
            }
        }

        // 获取/注册延迟事件的强类型分发器：把 object 拆箱回 T 后调用泛型 Publish<T>
        private Action<object> GetOrCreateDispatcher<T>() where T : struct
        {
            return _dispatchers.GetOrAdd(typeof(T), key => obj => Publish((T)obj));
        }

        // 移除同步处理器（供令牌从外部调用，内部自行加锁）
        internal void RemoveHandler<T>(Type eventType, EfEventHandler<T> handler) where T : struct
        {
            lock (_gate)
            {
                var subs = GetSubscriptions<T>(eventType);
                if (subs == null)
                    return;
                subs.SyncHandlers.Remove(handler);
                if (subs.IsEmpty)
                    _subscriptions.Remove(eventType);
            }
        }

        // 移除异步处理器（供令牌从外部调用，内部自行加锁）
        internal void RemoveAsyncHandler<T>(Type eventType, EfEventAsyncHandler<T> asyncHandler) where T : struct
        {
            lock (_gate)
            {
                var subs = GetSubscriptions<T>(eventType);
                if (subs == null)
                    return;
                subs.AsyncHandlers.Remove(asyncHandler);
                if (subs.IsEmpty)
                    _subscriptions.Remove(eventType);
            }
        }

        // 内部日志
        private void Error(string msg)
        {
            if (!OpenDebug)
                return;
            D.Error(msg);
        }

        #endregion

        #region 公开方法 Public Methods

        /// <summary>
        /// 设置是否输出处理器异常日志
        /// <para>Set whether to output handler exception logs</para>
        /// </summary>
        /// <param name="openDebug">是否开启<para>Whether to enable</para></param>
        public void SetOpenDebug(bool openDebug)
        {
            OpenDebug = openDebug;
        }

        /// <summary>
        /// 订阅同步事件
        /// <para>Subscribe to a synchronous event</para>
        /// </summary>
        /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
        /// <param name="handler">事件处理器<para>Event handler</para></param>
        /// <param name="group">分组名（可选）<para>Group name (optional)</para></param>
        /// <returns>用于取消订阅的令牌<para>Token for unsubscription</para></returns>
        public IDisposable Subscribe<T>(Action<T> handler, string group = null) where T : struct
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            var eventType = typeof(T);
            var h = new EfEventHandler<T>
            {
                Action = handler,
                Group = group
            };
            var token = new EventSubscriptionToken<T>(eventType, h);
            h.Token = token;
            lock (_gate)
            {
                GetOrCreateSubscriptions<T>(eventType).SyncHandlers.Add(h);
            }

            return token;
        }

        /// <summary>
        /// 订阅异步事件
        /// <para>Subscribe to an asynchronous event</para>
        /// </summary>
        /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
        /// <param name="asyncHandler">异步事件处理器<para>Async event handler</para></param>
        /// <param name="group">分组名（可选）<para>Group name (optional)</para></param>
        /// <returns>用于取消订阅的令牌<para>Token for unsubscription</para></returns>
        public IDisposable Subscribe<T>(Func<T, UniTask> asyncHandler, string group = null) where T : struct
        {
            if (asyncHandler == null)
                throw new ArgumentNullException(nameof(asyncHandler));
            var eventType = typeof(T);
            var ah = new EfEventAsyncHandler<T>
            {
                Func = asyncHandler,
                Group = group
            };
            var token = new EventSubscriptionToken<T>(eventType, ah);
            ah.Token = token;
            lock (_gate)
            {
                GetOrCreateSubscriptions<T>(eventType).AsyncHandlers.Add(ah);
            }

            return token;
        }

        /// <summary>
        /// 同步发布事件（不等待异步处理器）
        /// <para>Publish event synchronously (does not wait for async handlers)</para>
        /// </summary>
        /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
        /// <param name="eventData">事件数据<para>Event data</para></param>
        public void Publish<T>(T eventData) where T : struct
        {
            EventHandlersSnapshot<T> snapshot;
            lock (_gate)
            {
                var subs = GetSubscriptions<T>(typeof(T));
                if (subs == null)
                    return;
                snapshot = RentSnapshot(subs);
            }

            try
            {
                ExecuteSyncHandlers(snapshot, eventData);
                var asyncList = snapshot.Async;
                int asyncCount = snapshot.AsyncCount;
                for (int i = 0; i < asyncCount; i++)
                    FireAsync(asyncList[i].Func, eventData).Forget();
            }
            finally
            {
                SnapshotPool<T>.Return(snapshot);
            }
        }

        /// <summary>
        /// 异步发布事件（等待所有处理器完成，支持取消）
        /// <para>Publish event asynchronously (awaits all handlers, supports cancellation)</para>
        /// </summary>
        /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
        /// <param name="eventData">事件数据<para>Event data</para></param>
        /// <param name="token">取消令牌<para>Cancellation token</para></param>
        public async UniTask PublishAsync<T>(T eventData, CancellationToken token = default) where T : struct
        {
            if (token.IsCancellationRequested)
                return;

            PoolManager pools;
            EventHandlersSnapshot<T> snapshot;
            lock (_gate)
            {
                pools = _pools;
                var subs = GetSubscriptions<T>(typeof(T));
                if (subs == null)
                    return;
                snapshot = RentSnapshot(subs);
            }

            UniTaskList tasks = null;
            try
            {
                ExecuteSyncHandlers(snapshot, eventData);
                var asyncList = snapshot.Async;
                int asyncCount = snapshot.AsyncCount;
                if (asyncCount > 0)
                {
                    tasks = pools.GetFromPool<UniTaskList>() ?? new UniTaskList();
                    for (int i = 0; i < asyncCount; i++)
                        tasks.Add(HandleAsync(asyncList[i].Func, eventData, token));

                    await UniTask.WhenAll(tasks);
                }
            }
            finally
            {
                if (tasks != null)
                    pools.ReturnToPool(tasks);
                SnapshotPool<T>.Return(snapshot);
            }
        }

        /// <summary>
        /// 将事件加入延迟队列
        /// <para>Enqueue event for delayed dispatch</para>
        /// </summary>
        /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
        /// <param name="eventData">事件数据<para>Event data</para></param>
        public void Enqueue<T>(T eventData) where T : struct
        {
            lock (_gate)
            {
                // 注册该类型的分发器，Flush 时按运行时类型取用
                GetOrCreateDispatcher<T>();
                _delayedEvents.Enqueue(eventData);
            }
        }

        /// <summary>
        /// 立即发布所有已入队的延迟事件
        /// <para>Flush all enqueued delayed events immediately</para>
        /// </summary>
        public void Flush()
        {
            PoolManager pools;
            EventObjectList eventsToPublish;
            lock (_gate)
            {
                if (_delayedEvents == null || _delayedEvents.Count == 0)
                    return;
                pools = _pools;
                eventsToPublish = pools.GetFromPool<EventObjectList>() ?? new EventObjectList();
                int count = _delayedEvents.Count;
                if (eventsToPublish.Capacity < count)
                    eventsToPublish.Capacity = count;
                while (_delayedEvents.Count > 0)
                    eventsToPublish.Add(_delayedEvents.Dequeue());
            }

            try
            {
                for (int i = 0; i < eventsToPublish.Count; i++)
                {
                    var evt = eventsToPublish[i];
                    if (_dispatchers != null && _dispatchers.TryGetValue(evt.GetType(), out var dispatch))
                        dispatch(evt);
                }
            }
            finally
            {
                if (pools != null)
                    pools.ReturnToPool(eventsToPublish);
            }
        }

        /// <summary>
        /// 取消指定事件类型的所有订阅
        /// <para>Unsubscribe all handlers of a given event type</para>
        /// </summary>
        /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
        public void UnsubscribeAll<T>() where T : struct
        {
            lock (_gate)
            {
                var eventType = typeof(T);
                var subs = GetSubscriptions<T>(eventType);
                if (subs == null)
                    return;
                subs.Clear();
                _subscriptions.Remove(eventType);
            }
        }

        /// <summary>
        /// 取消指定分组的所有订阅（不论事件类型）
        /// <para>Unsubscribe all handlers belonging to a specific group (across all event types)</para>
        /// </summary>
        /// <param name="group">分组名称<para>Group name</para></param>
        public void UnsubscribeGroup(string group)
        {
            if (string.IsNullOrEmpty(group))
                return;

            lock (_gate)
            {
                if (_subscriptions == null)
                    return;

                var pools = _pools;
                var typesToRemove = pools.GetFromPool<EventTypeList>() ?? new EventTypeList();
                try
                {
                    foreach (var kvp in _subscriptions)
                    {
                        kvp.Value.RemoveByGroup(group);
                        if (kvp.Value.IsEmpty)
                            typesToRemove.Add(kvp.Key);
                    }

                    for (int i = 0; i < typesToRemove.Count; i++)
                        _subscriptions.Remove(typesToRemove[i]);
                }
                finally
                {
                    pools.ReturnToPool(typesToRemove);
                }
            }
        }

        /// <summary>
        /// 取消指定事件类型下、指定分组的订阅（同步与异步处理器都会移除）
        /// <para>Unsubscribe handlers of a specific group under a specific event type (both sync and async)</para>
        /// </summary>
        /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
        /// <param name="group">分组名称<para>Group name</para></param>
        /// <returns>实际移除的处理器总数<para>Total number of handlers actually removed</para></returns>
        public int UnsubscribeGroup<T>(string group) where T : struct
        {
            if (string.IsNullOrEmpty(group))
                return 0;

            lock (_gate)
            {
                var eventType = typeof(T);
                var subs = GetSubscriptions<T>(eventType);
                if (subs == null)
                    return 0;

                int removed = subs.RemoveByGroup(group);
                if (subs.IsEmpty)
                    _subscriptions.Remove(eventType);
                return removed;
            }
        }

        /// <summary>
        /// 清空延迟事件队列（不发布）
        /// <para>Clear the delayed event queue without publishing</para>
        /// </summary>
        public void ClearDelayedEvents()
        {
            lock (_gate)
            {
                _delayedEvents?.Clear();
            }
        }

        #endregion
    }
}
