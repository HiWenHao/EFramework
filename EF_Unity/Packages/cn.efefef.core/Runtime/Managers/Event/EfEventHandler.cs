/*
 * ================================================
 * Describe:      泛型同步处理器包装，直接持有强类型委托避免装箱
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 16:26:46
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-11-18 14:02:00
 * ScriptVersion: 0.3
 * ===============================================
 */

using System;

namespace EasyFramework.Events
{
    /// <summary>
    /// 泛型同步处理器包装，直接持有强类型委托避免装箱
    /// <para>Generic synchronous handler wrapper, holding a strongly-typed delegate to avoid boxing</para>
    /// </summary>
    /// <typeparam name="T">事件类型<para>Event type</para></typeparam>
    internal sealed class EfEventHandler<T> where T : struct
    {
        /// <summary>
        /// 强类型委托，直接调用无需装箱拆箱
        /// <para>Strongly-typed delegate, invoked directly without boxing/unboxing</para>
        /// </summary>
        public Action<T> Action;

        /// <summary>
        /// 订阅令牌，用于取消订阅
        /// <para>Subscription token for unsubscription</para>
        /// </summary>
        public EventSubscriptionToken<T> Token;

        /// <summary>
        /// 分组名称，用于批量取消
        /// <para>Group name for batch unsubscription</para>
        /// </summary>
        public string Group;
    }
}
