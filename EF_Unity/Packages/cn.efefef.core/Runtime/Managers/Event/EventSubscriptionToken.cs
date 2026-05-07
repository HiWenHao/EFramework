/*
 * ================================================
 * Describe:      可Dispose的订阅令牌，Dispose时移除自身
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 17:15:01
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 17:15:01
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Managers.Event
{
    /// <summary>
    /// 可Dispose的订阅令牌，Dispose时移除自身
    /// <para>Disposable subscription token that removes itself upon disposal</para>
    /// </summary>
    internal class EventSubscriptionToken : IDisposable
    {
        private readonly Type _eventType;
        private readonly EventHandler _handler;
        private readonly EventAsyncHandler _asyncHandler;
        private bool _disposed;

        public EventSubscriptionToken(Type eventType, EventHandler handler)
        {
            _eventType = eventType;
            _handler = handler;
            _asyncHandler = null;
        }

        public EventSubscriptionToken(Type eventType, EventAsyncHandler asyncHandler)
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
            if (_disposed) return;
            _disposed = true;
            if (_handler != null)
                EventSystem.Instance.RemoveHandler(_eventType, _handler);
            else if (_asyncHandler != null)
                EventSystem.Instance.RemoveAsyncHandler(_eventType, _asyncHandler);
        }
    }
}