/*
 * ================================================
 * Describe:      Event service implementation - type-safe, group support, delayed dispatch, async awaiting
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 15:49:19
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 15:49:19
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EasyFramework.Managers;
using EasyFramework.Systems.Pool;

namespace EasyFramework.Systems.Event
{
    /// <summary>
    /// 事件服务实现——类型安全、支持分组、延迟发送、异步等待
    /// <para>Event service implementation - type-safe, group support, delayed dispatch, async awaiting</para>
    /// </summary>
    [Manager(Order = -999)]
    [Dependency(typeof(PoolSystem))]
    public sealed class EventSystem : MonoSingleton<EventSystem>, IEventService, ISingleton
    {
        private bool _openDebug;
        private object _flushLock; // 用于延迟发布的锁
        private Queue<object> _delayedEvents; // 延迟事件队列（弱类型存储，发布时再确定类型）
        private Dictionary<Type, EventSubscriptions> _subscriptions; // 存储每种事件类型的订阅列表

        public void Init()
        {
            _openDebug = true;
            _flushLock = new object();
            _delayedEvents = new Queue<object>();
            _subscriptions = new Dictionary<Type, EventSubscriptions>();
        }

        public void Quit()
        {
            lock (_flushLock)
            {
                _delayedEvents.Clear();
            }

            _subscriptions.Clear();
        }

        #region 内部方法 Internal Methods

        //  从指定事件类型中移除同步处理器
        internal void RemoveHandler(Type eventType, EventHandler handler)
        {
            if (!_subscriptions.TryGetValue(eventType, out var subs))
                return;
            subs.SyncHandlers.Remove(handler);
            if (subs.IsEmpty)
                _subscriptions.Remove(eventType);
        }

        //  从指定事件类型中移除异步处理器
        internal void RemoveAsyncHandler(Type eventType, EventAsyncHandler asyncHandler)
        {
            if (!_subscriptions.TryGetValue(eventType, out var subs))
                return;
            subs.AsyncHandlers.Remove(asyncHandler);
            if (subs.IsEmpty)
                _subscriptions.Remove(eventType);
        }

        //  获取或创建事件类型对应的订阅容器
        private EventSubscriptions GetOrCreateSubscriptions(Type eventType)
        {
            if (_subscriptions.TryGetValue(eventType, out var subs))
                return subs;

            subs = new EventSubscriptions();
            _subscriptions[eventType] = subs;
            return subs;
        }

        //  安全执行所有同步处理器（拷贝列表防止迭代中修改）
        private void ExecuteSyncHandlers(EventSubscriptions subs, object eventData)
        {
            var syncList = subs.SyncHandlers.ToArray();
            foreach (var handler in syncList)
            {
                try
                {
                    handler.Action?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Error($"[ EventSystem ] Error in sync handler: {e}");
                }
            }
        }

        //  异步处理器：Fire-and-Forget，内部捕捉异常
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

        //  带有 CancellationToken 的异步处理器执行（支持取消）
        private async UniTask HandleAsync(Func<object, UniTask> func, object eventData, CancellationToken token)
        {
            try
            {
                // 执行异步处理器，并绑定取消令牌
                await func(eventData).AttachExternalCancellation(token);
            }
            catch (OperationCanceledException)
            {
                // 取消是正常行为，透传
                throw;
            }
            catch (Exception e)
            {
                Error($"[ EventSystem ] Error in async handler: {e}");
            }
        }

        //  内部弱类型发布（用于 Flush），直接使用 object 分发
        private void PublishInternal(object eventData)
        {
            var eventType = eventData.GetType();
            if (!_subscriptions.TryGetValue(eventType, out var subs)) return;
            ExecuteSyncHandlers(subs, eventData);
            // 异步处理器 Fire-and-Forget
            var asyncList = subs.AsyncHandlers.ToArray();
            foreach (var handler in asyncList)
            {
                FireAsync(handler.Func, eventData).Forget();
            }
        }

        //  安全获取订阅容器（不存在返回null）
        private EventSubscriptions GetSubscriptions(Type eventType)
        {
            _subscriptions.TryGetValue(eventType, out var subs);
            return subs;
        }

        //  内部日志
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
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="group">分组名（可选）</param>
        /// <returns>用于取消订阅的令牌</returns>
        public IDisposable Subscribe<T>(Action<T> handler, string group = null) where T : struct
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            var eventType = typeof(T);
            var subs = GetOrCreateSubscriptions(eventType);
            var h = new EventHandler
            {
                Action = (obj) => handler((T)obj),
                Group = group
            };
            var token = new EventSubscriptionToken(eventType, h);
            h.Token = token;
            subs.SyncHandlers.Add(h);
            return token;
        }

        /// <summary>
        /// 订阅异步事件
        /// <para>Subscribe to an asynchronous event</para>
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="asyncHandler">异步事件处理器</param>
        /// <param name="group">分组名（可选）</param>
        /// <returns>用于取消订阅的令牌</returns>
        public IDisposable Subscribe<T>(Func<T, UniTask> asyncHandler, string group = null) where T : struct
        {
            if (asyncHandler == null)
                throw new ArgumentNullException(nameof(asyncHandler));
            var eventType = typeof(T);
            var subs = GetOrCreateSubscriptions(eventType);
            var ah = new EventAsyncHandler
            {
                Func = (obj) => asyncHandler((T)obj),
                Group = group
            };
            var token = new EventSubscriptionToken(eventType, ah);
            ah.Token = token;
            subs.AsyncHandlers.Add(ah);
            return token;
        }

        /// <summary>
        /// 同步发布事件（不等待异步处理器）
        /// <para>Publish event synchronously (does not wait for async handlers)</para>
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void Publish<T>(T eventData) where T : struct
        {
            var subs = GetSubscriptions(typeof(T));
            if (subs == null) return;
            ExecuteSyncHandlers(subs, eventData);
            // 异步处理器：Fire-and-Forget
            var asyncList = subs.AsyncHandlers.ToArray();
            foreach (var handler in asyncList)
            {
                FireAsync(handler.Func, eventData).Forget();
            }
        }

        /// <summary>
        /// 异步发布事件（等待所有处理器完成，支持取消）
        /// <para>Publish event asynchronously (awaits all handlers, supports cancellation)</para>
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        /// <param name="token">取消令牌</param>
        public async UniTask PublishAsync<T>(T eventData, CancellationToken token = default) where T : struct
        {
            var subs = GetSubscriptions(typeof(T));
            if (subs == null) return;
            // 先执行所有同步处理器
            ExecuteSyncHandlers(subs, eventData);
            // 异步处理器：并行等待全部完成
            var asyncList = subs.AsyncHandlers.ToArray();
            if (asyncList.Length > 0)
            {
                var tasks = new List<UniTask>(asyncList.Length);
                foreach (var handler in asyncList)
                {
                    tasks.Add(HandleAsync(handler.Func, eventData, token));
                }

                await UniTask.WhenAll(tasks);
            }
        }

        /// <summary>
        /// 将事件加入延迟队列
        /// <para>Enqueue event for delayed dispatch</para>
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="eventData">事件数据</param>
        public void Enqueue<T>(T eventData) where T : struct
        {
            lock (_flushLock)
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
            lock (_flushLock)
            {
                if (_delayedEvents.Count == 0) return;
                eventsToPublish = _delayedEvents.ToArray();
                _delayedEvents.Clear();
            }

            foreach (var evt in eventsToPublish)
            {
                PublishInternal(evt);
            }
        }

        /// <summary>
        /// 取消指定事件类型的所有订阅
        /// <para>Unsubscribe all handlers of a given event type</para>
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        public void UnsubscribeAll<T>() where T : struct
        {
            var eventType = typeof(T);
            if (!_subscriptions.TryGetValue(eventType, out var subs))
                return;

            subs.Clear();
            _subscriptions.Remove(eventType);
        }

        /// <summary>
        /// 取消指定分组的所有订阅（不论事件类型）
        /// <para>Unsubscribe all handlers belonging to a specific group (across all event types)</para>
        /// </summary>
        /// <param name="group">分组名称</param>
        public void UnsubscribeGroup(string group)
        {
            if (string.IsNullOrEmpty(group)) return;
            var typesToRemove = new List<Type>();
            foreach (var kvp in _subscriptions)
            {
                var subs = kvp.Value;
                subs.SyncHandlers.RemoveAll(h => h.Group == group);
                subs.AsyncHandlers.RemoveAll(h => h.Group == group);
                if (subs.IsEmpty)
                    typesToRemove.Add(kvp.Key);
            }

            foreach (var type in typesToRemove)
                _subscriptions.Remove(type);
        }

        /// <summary>
        /// 清空延迟事件队列（不发布）
        /// <para>Clear the delayed event queue without publishing</para>
        /// </summary>
        public void ClearDelayedEvents()
        {
            lock (_flushLock)
            {
                _delayedEvents.Clear();
            }
        }

        #endregion
    }
}