/*
 * ================================================
 * Describe:      事件系统的泛型异步处理器包装，直接持有强类型委托避免装箱
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 17:11:27
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-18 14:02:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Events
{
    /// <summary>
    /// 泛型异步处理器包装，直接持有强类型委托避免装箱
    /// <para>Generic asynchronous handler wrapper, holding a strongly-typed delegate to avoid boxing</para>
    /// </summary>
    /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
    internal sealed class EfEventAsyncHandler<T> where T : struct
    {
        /// <summary>
        /// 强类型异步委托，直接调用无需装箱拆箱
        /// <para>Strongly-typed async delegate, invoked directly without boxing/unboxing</para>
        /// </summary>
        public Func<T, UniTask> Func;

        /// <summary>
        /// 订阅令牌
        /// <para>Subscription token</para>
        /// </summary>
        public EventSubscriptionToken<T> Token;

        /// <summary>
        /// 分组名称
        /// <para>Group name</para>
        /// </summary>
        public string Group;
    }
}