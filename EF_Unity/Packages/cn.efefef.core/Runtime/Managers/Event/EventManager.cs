/*
 * ================================================
 * Describe:      Event service implementation - type-safe, group support, delayed dispatch, async awaiting
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 15:49:19
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-18 13:21:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers.Pool;

namespace EasyFramework.Managers.Event
{
    /// <summary>
    /// 事件服务实现——类型安全、支持分组、延迟发送、异步等待
    /// <para>Event service implementation - type-safe, group support, delayed dispatch, async awaiting</para>
    /// </summary>
    [Manager(Order = 99800)]
    [Dependency(typeof(PoolManager))]
    public sealed class EventManager : MonoSingleton<EventManager>, IEventService, ISingleton
    {
        // 统一锁：保护 _delayedEvents 与 _subscriptions 的所有读写
        private readonly object _gate = new object();
        // 是否输出错误日志
        private bool _openDebug;
        // 延迟事件队列（受 _gate 保护）
        private Queue<object> _delayedEvents;
        // 每种事件类型的订阅列表（受 _gate 保护）
        private Dictionary<Type, EventSubscriptions> _subscriptions;


        #region 生命周期 Lifecycle

        void ISingleton.Init()
        {
            lock (_gate)
            {
                if (_subscriptions != null)
                    return;
                _openDebug = true;
                _delayedEvents = new Queue<object>();
                _subscriptions = new Dictionary<Type, EventSubscriptions>();
            }
        }

        void ISingleton.Quit()
        {
            lock (_gate)
            {
                if (_subscriptions != null)
                {
                    foreach (var kvp in _subscriptions)
                    {
                        InvalidateSubscriptions(kvp.Value);
                    }

                    _subscriptions.Clear();
                }

                _delayedEvents?.Clear();

                _subscriptions = null;
                _delayedEvents = null;
            }
        }

        #endregion

        #region 内部方法 Internal Methods

        // 使容器内所有处理器的令牌失效，避免 Quit/UnsubscribeAll 后再 Dispose 误删其它订阅
        private static void InvalidateSubscriptions(EventSubscriptions subs)
        {
            var syncList = subs.SyncHandlers;
            for (int i = 0; i < syncList.Count; i++)
            {
                syncList[i].Token?.Invalidate();
            }

            var asyncList = subs.AsyncHandlers;
            for (int i = 0; i < asyncList.Count; i++)
            {
                asyncList[i].Token?.Invalidate();
            }
        }

        // 从指定事件类型中移除同步处理器（供令牌从外部调用，内部自行加锁）
        internal void RemoveHandler(Type eventType, EventHandler handler)
        {
            lock (_gate)
            {
                if (_subscriptions == null || !_subscriptions.TryGetValue(eventType, out var subs))
                    return;
                subs.SyncHandlers.Remove(handler);
                if (subs.IsEmpty)
                    _subscriptions.Remove(eventType);
            }
        }

        // 从指定事件类型中移除异步处理器（供令牌从外部调用，内部自行加锁）
        internal void RemoveAsyncHandler(Type eventType, EventAsyncHandler asyncHandler)
        {
            lock (_gate)
            {
                if (_subscriptions == null || !_subscriptions.TryGetValue(eventType, out var subs))
                    return;
                subs.AsyncHandlers.Remove(asyncHandler);
                if (subs.IsEmpty)
                    _subscriptions.Remove(eventType);
            }
        }

        // 获取或创建事件类型对应的订阅容器（调用方需持有 _gate）
        private EventSubscriptions GetOrCreateSubscriptions(Type eventType)
        {
            if (_subscriptions.TryGetValue(eventType, out var subs))
                return subs;

            subs = new EventSubscriptions();
            _subscriptions[eventType] = subs;
            return subs;
        }

        // 在锁内获取指定事件类型的处理器快照，锁外安全执行（调用方需持有 _gate）
        private EventHandlersSnapshot GetSubscriptionsSnapshot(Type eventType)
        {
            if (_subscriptions == null || !_subscriptions.TryGetValue(eventType, out var subs))
                return null;
            return new EventHandlersSnapshot(subs.SyncHandlers.ToArray(), subs.AsyncHandlers.ToArray());
        }

        // 安全执行所有同步处理器（传入快照，锁外执行）
        private void ExecuteSyncHandlers(EventHandler[] handlers, object eventData)
        {
            for (int i = 0; i < handlers.Length; i++)
            {
                try
                {
                    handlers[i].Action?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Error($"[ EventSystem ] Error in sync handler: {e}");
                }
            }
        }

        // 异步处理器：Fire-and-Forget，内部捕捉异常
        private async UniTaskVoid FireAsync(Func<object, UniTask> func, object eventData)
        {
            try
            {
                await func(eventData);
            }
            catch (Exception e)
            {
                Error($"[ EventSystem ] Error in async handler: {e}");
            }
        }

        // 带有 CancellationToken 的异步处理器执行：取消是正常行为，静默完成不外抛
        private async UniTask HandleAsync(Func<object, UniTask> func, object eventData, CancellationToken token)
        {
            // 提前拦截：令牌已取消时不调用处理器，避免其同步前置段产生副作用
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
                Error($"[ EventSystem ] Error in async handler: {e}");
            }
        }

        // 内部弱类型发布（用于 Flush），锁内取快照、锁外分发
        private void PublishInternal(object eventData)
        {
            EventHandlersSnapshot snapshot;
            lock (_gate)
            {
                snapshot = GetSubscriptionsSnapshot(eventData.GetType());
            }

            if (snapshot == null)
                return;

            ExecuteSyncHandlers(snapshot.Sync, eventData);
            var asyncList = snapshot.Async;
            for (int i = 0; i < asyncList.Length; i++)
            {
                FireAsync(asyncList[i].Func, eventData).Forget();
            }
        }

        // 移除同步处理器列表中的指定分组并使令牌失效（调用方需持有 _gate）
        private static void RemoveSyncByGroup(List<EventHandler> handlers, string group)
        {
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                if (handlers[i].Group != group)
                    continue;
                handlers[i].Token?.Invalidate();
                handlers.RemoveAt(i);
            }
        }

        // 移除异步处理器列表中的指定分组并使令牌失效（调用方需持有 _gate）
        private static void RemoveAsyncByGroup(List<EventAsyncHandler> handlers, string group)
        {
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                if (handlers[i].Group != group)
                    continue;
                handlers[i].Token?.Invalidate();
                handlers.RemoveAt(i);
            }
        }

        // 内部日志
        private void Error(string msg)
        {
            if (!_openDebug)
                return;
            D.Error(msg);
        }

        #endregion

        #region 公开方法 Public Methods

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
            var h = new EventHandler
            {
                Action = (obj) => handler((T)obj),
                Group = group
            };
            var token = new EventSubscriptionToken(eventType, h);
            h.Token = token;
            lock (_gate)
            {
                GetOrCreateSubscriptions(eventType).SyncHandlers.Add(h);
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
            var ah = new EventAsyncHandler
            {
                Func = (obj) => asyncHandler((T)obj),
                Group = group
            };
            var token = new EventSubscriptionToken(eventType, ah);
            ah.Token = token;
            lock (_gate)
            {
                GetOrCreateSubscriptions(eventType).AsyncHandlers.Add(ah);
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
            EventHandlersSnapshot snapshot;
            lock (_gate)
            {
                snapshot = GetSubscriptionsSnapshot(typeof(T));
            }

            if (snapshot == null)
                return;

            ExecuteSyncHandlers(snapshot.Sync, eventData);
            var asyncList = snapshot.Async;
            for (int i = 0; i < asyncList.Length; i++)
                FireAsync(asyncList[i].Func, eventData).Forget();
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
            // 提前拦截：已取消则整个发布静默跳过
            if (token.IsCancellationRequested)
                return;

            EventHandlersSnapshot snapshot;
            lock (_gate)
            {
                snapshot = GetSubscriptionsSnapshot(typeof(T));
            }

            if (snapshot == null)
                return;

            ExecuteSyncHandlers(snapshot.Sync, eventData);
            var asyncList = snapshot.Async;
            if (asyncList.Length > 0)
            {
                var tasks = new List<UniTask>(asyncList.Length);
                for (int i = 0; i < asyncList.Length; i++)
                    tasks.Add(HandleAsync(asyncList[i].Func, eventData, token));

                await UniTask.WhenAll(tasks);
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
                _delayedEvents.Enqueue(eventData);
            }
        }

        /// <summary>
        /// 立即发布所有已入队的延迟事件
        /// <para>Flush all enqueued delayed events immediately</para>
        /// </summary>
        public void Flush()
        {
            object[] eventsToPublish;
            lock (_gate)
            {
                if (_delayedEvents == null || _delayedEvents.Count == 0)
                    return;
                eventsToPublish = _delayedEvents.ToArray();
                _delayedEvents.Clear();
            }

            for (int i = 0; i < eventsToPublish.Length; i++)
                PublishInternal(eventsToPublish[i]);
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
                if (_subscriptions == null || !_subscriptions.TryGetValue(eventType, out var subs))
                    return;

                InvalidateSubscriptions(subs);
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

                var typesToRemove = new List<Type>();
                foreach (var kvp in _subscriptions)
                {
                    var subs = kvp.Value;
                    RemoveSyncByGroup(subs.SyncHandlers, group);
                    RemoveAsyncByGroup(subs.AsyncHandlers, group);
                    if (subs.IsEmpty)
                        typesToRemove.Add(kvp.Key);
                }

                for (int i = 0; i < typesToRemove.Count; i++)
                {
                    _subscriptions.Remove(typesToRemove[i]);
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
                if (_subscriptions == null || !_subscriptions.TryGetValue(typeof(T), out var subs))
                    return 0;

                int before = subs.SyncHandlers.Count + subs.AsyncHandlers.Count;
                RemoveSyncByGroup(subs.SyncHandlers, group);
                RemoveAsyncByGroup(subs.AsyncHandlers, group);
                int removed = before - (subs.SyncHandlers.Count + subs.AsyncHandlers.Count);

                if (subs.IsEmpty)
                    _subscriptions.Remove(typeof(T));
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