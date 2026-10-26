/*
 * ================================================
 * Describe:      Net module message dispatcher (opcode routing + RPC pairing).
 * Author:        Alvin5100
 * CreationTime:  2026-10-23 18:45:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-23 18:45:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// RPC 挂起调用的终止原因。
    /// <para>Termination reason of a pending RPC call.</para>
    /// </summary>
    public enum NetRpcFailure
    {
        /// <summary>
        /// 等待响应超时
        /// <para>Timed out while waiting for the response</para>
        /// </summary>
        Timeout = 0,

        /// <summary>
        /// 会话已关闭，响应不会再到达
        /// <para>The session was closed; no response can arrive anymore</para>
        /// </summary>
        SessionClosed = 1,
    }

    /// <summary>
    /// 消息分发器：按 Opcode 路由到处理器，并按 RpcId 配对请求/响应。
    /// <para>Message dispatcher: routes by opcode and pairs requests/responses by rpcId.</para>
    /// </summary>
    /// <remarks>
    /// 支持会话级路由：处理器可注册为「任意会话」（sessionId = 0）或「指定会话」，
    /// 派发时优先命中指定会话的处理器，其次回退到任意会话的处理器。
    /// <para>Supports session-scoped routing: a handler is registered for any session (sessionId = 0)
    /// or for one specific session; dispatch prefers the session-specific handler and falls back
    /// to the any-session one.</para>
    /// </remarks>
    public sealed class MessageDispatcher
    {
        // 待确认的 RPC 调用
        private sealed class PendingCall
        {
            // 请求分配的配对号
            public int RpcId;

            // 发起方会话 Id（0 = 不限定来源会话）
            public int SessionId;

            // 响应到达时的回调
            public Action<NetPacket> OnResponse;

            // 失败回调（超时 / 会话关闭）
            public Action<NetRpcFailure> OnFailure;

            // 0 = 永不超时（与 Environment.TickCount 同域，自然回绕安全）
            public int DeadlineTick;
        }

        #region Fields

        // 处理器表：sessionId（0 = 任意会话）→ opcode → 处理器
        private readonly Dictionary<int, Dictionary<int, Action<NetPacket>>> _handlers =
            new Dictionary<int, Dictionary<int, Action<NetPacket>>>();

        // rpcId → 待配对响应
        private readonly Dictionary<int, PendingCall> _pendingCalls = new Dictionary<int, PendingCall>();

        // rpcId 种子（自增）
        private int _rpcIdSeed = 1;

        /// <summary>
        /// RPC 超时回调（参数为 rpcId 与来源会话 Id）
        /// <para>RPC timeout callback (rpcId, source session id)</para>
        /// </summary>
        public event Action<int, int> RpcTimedOut;

        #endregion

        #region Properties

        /// <summary>
        /// 序列化器（类型化 Handler 与 RPC 响应需要；不使用类型化功能时可为 null）。
        /// <para>Serializer for typed handlers and RPC responses; may be null when unused.</para>
        /// </summary>
        public IMessageSerializer Serializer { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// 注册处理器（对任意会话生效）。
        /// <para>Register a handler (applies to any session).</para>
        /// </summary>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="handler">处理回调<para>Handle callback</para></param>
        public void RegisterHandler(int opcode, Action<NetPacket> handler)
        {
            SetHandler(0, opcode, handler);
        }

        /// <summary>
        /// 注册处理器（仅对指定会话生效，优先于同 opcode 的任意会话处理器）。
        /// <para>Register a handler for one session only (takes precedence over the any-session handler).</para>
        /// </summary>
        /// <param name="sessionId">目标会话 Id<para>Target session id</para></param>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="handler">处理回调<para>Handle callback</para></param>
        public void RegisterHandler(int sessionId, int opcode, Action<NetPacket> handler)
        {
            SetHandler(sessionId, opcode, handler);
        }

        /// <summary>
        /// 注册类型化处理器：Opcode 取自 <see cref="IMessageHandler{TMessage}.Opcode"/>，
        /// 载荷经 <see cref="Serializer"/> 反序列化后回调（对任意会话生效）。
        /// <para>Register a typed handler: opcode from IMessageHandler&lt;TMessage&gt;.Opcode;
        /// payload deserialized via Serializer before the callback (applies to any session).</para>
        /// </summary>
        /// <typeparam name="TMessage">消息类型<para>Message type</para></typeparam>
        /// <typeparam name="THandler">处理器类型<para>Handler type</para></typeparam>
        /// <param name="handler">处理器实例；传 null 时按无参构造创建<para>Handler instance; created via parameterless ctor when null</para></param>
        public void RegisterHandler<TMessage, THandler>(THandler handler = null)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>, new()
        {
            RegisterHandlerInternal<TMessage, THandler>(0, handler);
        }

        /// <summary>
        /// 注册类型化处理器（仅对指定会话生效）。
        /// <para>Register a typed handler for one session only.</para>
        /// </summary>
        /// <typeparam name="TMessage">消息类型<para>Message type</para></typeparam>
        /// <typeparam name="THandler">处理器类型<para>Handler type</para></typeparam>
        /// <param name="sessionId">目标会话 Id<para>Target session id</para></param>
        /// <param name="handler">处理器实例；传 null 时按无参构造创建<para>Handler instance; created via parameterless ctor when null</para></param>
        public void RegisterHandler<TMessage, THandler>(int sessionId, THandler handler = null)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>, new()
        {
            RegisterHandlerInternal<TMessage, THandler>(sessionId, handler);
        }

        /// <summary>
        /// 注册原始处理器（实现 <see cref="IMessageHandler"/> 的对象，对任意会话生效）。
        /// <para>Register a raw handler object implementing IMessageHandler (applies to any session).</para>
        /// </summary>
        /// <param name="handler">处理器对象<para>Handler object</para></param>
        public void RegisterHandler(IMessageHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            SetHandler(0, handler.Opcode, handler.Handle);
        }

        /// <summary>
        /// 注册原始处理器（实现 <see cref="IMessageHandler"/> 的对象，仅对指定会话生效）。
        /// <para>Register a raw handler object implementing IMessageHandler for one session only.</para>
        /// </summary>
        /// <param name="sessionId">目标会话 Id<para>Target session id</para></param>
        /// <param name="handler">处理器对象<para>Handler object</para></param>
        public void RegisterHandler(int sessionId, IMessageHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            SetHandler(sessionId, handler.Opcode, handler.Handle);
        }

        /// <summary>
        /// 注销处理器（任意会话）。
        /// <para>Unregister a handler (any session).</para>
        /// </summary>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <returns>是否注销成功<para>Whether unregistered</para></returns>
        public bool UnregisterHandler(int opcode)
        {
            return UnregisterHandlerInternal(0, opcode);
        }

        /// <summary>
        /// 注销处理器（指定会话）。
        /// <para>Unregister a handler for one session.</para>
        /// </summary>
        /// <param name="sessionId">目标会话 Id<para>Target session id</para></param>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <returns>是否注销成功<para>Whether unregistered</para></returns>
        public bool UnregisterHandler(int sessionId, int opcode)
        {
            return UnregisterHandlerInternal(sessionId, opcode);
        }

        /// <summary>
        /// 注销指定会话的全部处理器（会话关闭时调用，避免字典随会话数无限增长）。
        /// <para>Unregister all handlers of a session (called when the session closes so the
        /// handler table does not grow with the session count).</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <returns>被注销的处理器数<para>Number of unregistered handlers</para></returns>
        public int UnregisterSessionHandlers(int sessionId)
        {
            if (sessionId == 0) return 0;
            if (!_handlers.TryGetValue(sessionId, out var byOpcode)) return 0;

            int count = byOpcode.Count;
            _handlers.Remove(sessionId);
            return count;
        }

        /// <summary>
        /// 分配一个新的 RpcId（单调递增，全局唯一）。
        /// <para>Allocate a new rpcId (monotonic, globally unique).</para>
        /// </summary>
        /// <returns>新的 RpcId<para>New rpcId</para></returns>
        public int NextRpcId()
        {
            return _rpcIdSeed++;
        }

        /// <summary>
        /// 登记一个待配对响应。
        /// <para>Register a pending response to be paired.</para>
        /// </summary>
        /// <param name="rpcId">请求分配的 RpcId<para>RpcId allocated for the request</para></param>
        /// <param name="onResponse">响应到达时的回调<para>Callback when the response arrives</para></param>
        /// <param name="onFailure">失败回调（超时 / 会话关闭）<para>Failure callback (timeout / session closed)</para></param>
        /// <param name="timeoutMilliseconds">超时毫秒数（&lt;=0 表示不超时）<para>Timeout in ms (&lt;=0 means never)</para></param>
        /// <param name="sessionId">发起方会话 Id（0 = 不限定来源会话）<para>Source session id (0 means unrestricted)</para></param>
        public void AddPendingCall(int rpcId, Action<NetPacket> onResponse, Action<NetRpcFailure> onFailure = null,
            long timeoutMilliseconds = 0, int sessionId = 0)
        {
            if (onResponse == null) throw new ArgumentNullException(nameof(onResponse));

            // int 相加自然回绕（unchecked），与 Environment.TickCount 同域，跨 24.8 天边界也安全
            int deadline = timeoutMilliseconds > 0
                ? unchecked(Environment.TickCount + (int)timeoutMilliseconds)
                : 0;
            _pendingCalls[rpcId] = new PendingCall
            {
                RpcId = rpcId,
                SessionId = sessionId,
                OnResponse = onResponse,
                OnFailure = onFailure,
                DeadlineTick = deadline
            };
        }

        /// <summary>
        /// 取消待配对响应。
        /// <para>Cancel a pending response.</para>
        /// </summary>
        /// <param name="rpcId">请求分配的 RpcId<para>RpcId allocated for the request</para></param>
        /// <returns>是否取消成功<para>Whether cancelled</para></returns>
        public bool RemovePendingCall(int rpcId)
        {
            return _pendingCalls.Remove(rpcId);
        }

        /// <summary>
        /// 取消指定会话的全部挂起调用（会话关闭时调用，避免等待方一直挂起到超时）。
        /// <para>Cancel all pending calls of a session (called when the session closes so waiters
        /// do not hang until timeout).</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <returns>被取消的调用数<para>Number of cancelled calls</para></returns>
        public int CancelPendingCalls(int sessionId)
        {
            if (_pendingCalls.Count == 0) return 0;

            List<int> cancelled = null;
            foreach (var kvp in _pendingCalls)
            {
                if (kvp.Value.SessionId != sessionId) continue;
                cancelled ??= new List<int>();
                cancelled.Add(kvp.Key);
            }

            if (cancelled == null) return 0;

            foreach (int rpcId in cancelled)
            {
                if (!_pendingCalls.Remove(rpcId, out var pending)) continue;
                pending.OnFailure?.Invoke(NetRpcFailure.SessionClosed);
            }
            return cancelled.Count;
        }

        /// <summary>
        /// 派发一个已解码的消息包：rpcId 非 0 时优先配对挂起的 RPC，否则路由到处理器。
        /// <para>Dispatch a decoded packet: non-zero rpcId completes a pending RPC first, else routes to handlers.</para>
        /// </summary>
        /// <param name="packet">已解码的消息包<para>Decoded packet</para></param>
        public void Dispatch(NetPacket packet)
        {
            if (packet.RpcId != 0
                && _pendingCalls.TryGetValue(packet.RpcId, out var pending)
                && (pending.SessionId == 0 || pending.SessionId == packet.SessionId))
            {
                _pendingCalls.Remove(packet.RpcId);
                pending.OnResponse(packet);
                return;
            }

            if (TryGetHandler(packet.SessionId, packet.Opcode, out var handler))
            {
                handler(packet);
            }
        }

        /// <summary>
        /// 检查 RPC 超时（由 NetworkManager 每帧驱动）。
        /// <para>Check RPC timeouts (driven by NetworkManager each frame).</para>
        /// </summary>
        /// <param name="currentTick">当前时间戳（Environment.TickCount）<para>Current tick (Environment.TickCount)</para></param>
        public void CheckTimeouts(int currentTick)
        {
            if (_pendingCalls.Count == 0) return;

            List<int> expired = null;
            foreach (var kvp in _pendingCalls)
            {
                if (kvp.Value.DeadlineTick == 0) continue;
                if (currentTick - kvp.Value.DeadlineTick < 0) continue;
                expired ??= new List<int>();
                expired.Add(kvp.Key);
            }

            if (expired == null) return;
            foreach (int rpcId in expired)
            {
                if (!_pendingCalls.Remove(rpcId, out var pending)) continue;
                pending.OnFailure?.Invoke(NetRpcFailure.Timeout);
                RpcTimedOut?.Invoke(rpcId, pending.SessionId);
            }
        }

        /// <summary>
        /// 清空所有处理器与挂起调用
        /// <para>Clear all handlers and pending calls</para>
        /// </summary>
        public void Clear()
        {
            _handlers.Clear();
            _pendingCalls.Clear();
        }

        // 注册或覆盖一个处理器
        private void SetHandler(int sessionId, int opcode, Action<NetPacket> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            if (!_handlers.TryGetValue(sessionId, out var byOpcode))
            {
                byOpcode = new Dictionary<int, Action<NetPacket>>();
                _handlers[sessionId] = byOpcode;
            }
            byOpcode[opcode] = handler;
        }

        // 注销一个处理器
        private bool UnregisterHandlerInternal(int sessionId, int opcode)
        {
            if (!_handlers.TryGetValue(sessionId, out var byOpcode)) return false;
            if (!byOpcode.Remove(opcode)) return false;
            if (byOpcode.Count == 0) _handlers.Remove(sessionId);
            return true;
        }

        // 类型化处理器注册（sessionId = 0 表示任意会话）
        private void RegisterHandlerInternal<TMessage, THandler>(int sessionId, THandler handler)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>, new()
        {
            handler ??= new THandler();
            int opcode = handler.Opcode;
            if (opcode <= 0)
                throw new ArgumentException(
                    $"Handler {typeof(THandler).Name} has no valid opcode. Add [NetOpcode(n)] to {typeof(TMessage).Name}.");

            SetHandler(sessionId, opcode, packet =>
            {
                if (packet.PayloadLength <= 0) return;
                var message = Serializer?.Deserialize<TMessage>(packet.Payload, 0, packet.PayloadLength);
                if (message == null) return;

                // 感知会话的处理器额外拿到来源会话 Id
                if (handler is ISessionAwareMessageHandler<TMessage> aware)
                    aware.Handle(message, packet.SessionId);
                else
                    handler.Handle(message);
            });
        }

        // 取处理器：先指定会话，再任意会话
        private bool TryGetHandler(int sessionId, int opcode, out Action<NetPacket> handler)
        {
            handler = null;

            if (sessionId != 0
                && _handlers.TryGetValue(sessionId, out var scoped)
                && scoped.TryGetValue(opcode, out handler))
            {
                return true;
            }

            if (_handlers.TryGetValue(0, out var global)
                && global.TryGetValue(opcode, out handler))
            {
                return true;
            }

            return false;
        }

        #endregion
    }
}
