/*
 * ================================================
 * Describe:      This script is used to .
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 17:17:15
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 17:17:15
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Systems.Event
{
    /// <summary>
    /// 事件服务接口
    /// </summary>
    internal interface IEventService
    {
        /// <summary>订阅同步事件，返回可Dispose的令牌，Dispose即取消订阅</summary>
        IDisposable Subscribe<T>(Action<T> handler, string group = null) where T : struct;

        /// <summary>订阅异步事件，返回可Dispose的令牌，Dispose即取消订阅</summary>
        IDisposable Subscribe<T>(Func<T, UniTask> asyncHandler, string group = null) where T : struct;

        /// <summary>同步发布事件，所有同步处理器立即执行，异步处理器Fire-and-Forget（不等待）</summary>
        void Publish<T>(T eventData) where T : struct;

        /// <summary>异步发布事件，等待所有处理器（同步+异步）完成，支持CancellationToken</summary>
        UniTask PublishAsync<T>(T eventData, CancellationToken token = default) where T : struct;

        /// <summary>将事件加入延迟队列，稍后调用Flush时统一发布</summary>
        void Enqueue<T>(T eventData) where T : struct;

        /// <summary>立即发布所有已入队的延迟事件</summary>
        void Flush();

        /// <summary>取消指定事件类型的所有订阅</summary>
        void UnsubscribeAll<T>() where T : struct;

        /// <summary>取消指定分组的所有订阅（不论事件类型）</summary>
        void UnsubscribeGroup(string group);

        /// <summary>清空延迟队列（不发布）</summary>
        void ClearDelayedEvents();
    }
}