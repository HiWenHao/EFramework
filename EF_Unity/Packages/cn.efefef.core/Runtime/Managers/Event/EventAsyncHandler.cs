/*
 * ================================================
 * Describe:      事件系统的异步处理器包装
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 17:11:27
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 17:11:27
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Managers.Event
{
    /// <summary>
    /// 异步处理器包装
    /// <para>Wrapper for asynchronous handler</para>
    /// </summary>
    internal class EventAsyncHandler
    {
        /// <summary>
        /// 返回UniTask的委托
        /// <para>Delegate returning UniTask</para>
        /// </summary>
        public Func<object, UniTask> Func;

        /// <summary>
        /// 订阅令牌
        /// <para>Subscription token</para>
        /// </summary>
        public IDisposable Token;

        /// <summary>
        /// 分组名称
        /// <para>Group name</para>
        /// </summary>
        public string Group;
    }
}