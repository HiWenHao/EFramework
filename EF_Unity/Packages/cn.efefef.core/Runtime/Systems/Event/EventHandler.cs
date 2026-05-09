/*
 * ================================================
 * Describe:      Strongly typed event system with group management and async support.
 * Author:        Alvin5100
 * CreationTime:  2026-05-07 16:26:46
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-05-07 16:26:46
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.Event
{
    /// <summary>
    /// 同步处理器包装
    /// <para>Wrapper for synchronous handler</para>
    /// </summary>
    internal class EventHandler
    {
        /// <summary>
        /// 委托（已装箱）
        /// <para>Boxed delegate</para>
        /// </summary>
        public Action<object> Action;

        /// <summary>
        /// 订阅令牌，用于取消订阅
        /// <para>Subscription token for unsubscription</para>
        /// </summary>
        public IDisposable Token;

        /// <summary>
        /// 分组名称，用于批量取消
        /// <para>Group name for batch unsubscription</para>
        /// </summary>
        public string Group;
    }
}