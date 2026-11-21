/*
 * ================================================
 * Describe:      可Dispose的泛型订阅令牌，Dispose时移除自身
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 17:15:01
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-18 14:02:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;

namespace EasyFramework.Events
{
    /// <summary>
    /// 可Dispose的泛型订阅令牌，Dispose时移除自身
    /// <para>Disposable generic subscription token that removes itself upon disposal</para>
    /// </summary>
    /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
    internal sealed class EventSubscriptionToken<T> : IDisposable where T : struct
    {
        private readonly Type _eventType;
        private readonly EfEventHandler<T> _handler;
        private readonly EfEventAsyncHandler<T> _asyncHandler;
        private bool _disposed;

        public EventSubscriptionToken(Type eventType, EfEventHandler<T> handler)
        {
            _eventType = eventType;
            _handler = handler;
            _asyncHandler = null;
        }

        public EventSubscriptionToken(Type eventType, EfEventAsyncHandler<T> asyncHandler)
        {
            _eventType = eventType;
            _handler = null;
            _asyncHandler = asyncHandler;
        }

        /// <summary>
        /// 移除对应处理器
        /// <para>Remove the corresponding handler</para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            if (_handler != null)
                EventManager.Instance.RemoveHandler<T>(_eventType, _handler);
            else if (_asyncHandler != null)
                EventManager.Instance.RemoveAsyncHandler<T>(_eventType, _asyncHandler);
        }

        /// <summary>
        /// 使令牌失效，此后 Dispose 不再回查管理器（供批量取消与 Quit 调用）
        /// <para>Invalidate the token so subsequent Dispose will not query the manager (used by bulk cancellation and Quit)</para>
        /// </summary>
        internal void Invalidate()
        {
            _disposed = true;
        }
    }
}
