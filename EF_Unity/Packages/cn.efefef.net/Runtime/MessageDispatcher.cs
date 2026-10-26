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
    /// 消息分发器：按 Opcode 路由到处理器，并按 RpcId 配对请求/响应。
    /// <para>Message dispatcher: routes by opcode and pairs requests/responses by rpcId.</para>
    /// </summary>
    public sealed class MessageDispatcher
    {
        // 待确认的 RPC 调用
        private sealed class PendingCall
        {
            // 请求分配的配对号
            public int RpcId;

            // 响应到达时的回调
            public Action<NetPacket> OnResponse;

            // 超时回调（未设置则不触发）
            public Action OnTimeout;

            // 0 = 永不超时（与 Environment.TickCount 同域，自然回绕安全）
            public int DeadlineTick;
        }

        #region Fields

        // opcode → 处理器
        private readonly Dictionary<int, Action<NetPacket>> _handlers = new Dictionary<int, Action<NetPacket>>();

        // rpcId → 待配对响应
        private readonly Dictionary<int, PendingCall> _pendingCalls = new Dictionary<int, PendingCall>();

        // rpcId 种子（自增）
        private int _rpcIdSeed = 1;

        /// <summary>
        /// RPC 超时回调（参数为 rpcId）
        /// <para>RPC timeout callback (rpcId)</para>
        /// </summary>
        public event Action<int> RpcTimedOut;

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
        /// 注册原始处理器（直接接收 <see cref="NetPacket"/>）。
        /// <para>Register a raw handler (receives NetPacket directly).</para>
        /// </summary>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="handler">处理回调<para>Handle callback</para></param>
        public void RegisterHandler(int opcode, Action<NetPacket> handler)
        {
            _handlers[opcode] = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <summary>
        /// 注册类型化处理器：Opcode 取自 <see cref="IMessageHandler{TMessage}.Opcode"/>，
        /// 载荷经 <see cref="Serializer"/> 反序列化后回调。
        /// <para>Register a typed handler: opcode from IMessageHandler&lt;TMessage&gt;.Opcode;
        /// payload deserialized via Serializer before the callback.</para>
        /// </summary>
        /// <param name="handler">处理器实例；传 null 时按无参构造创建<para>Handler instance; created via parameterless ctor when null</para></param>
        public void RegisterHandler<TMessage, THandler>(THandler handler = null)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>, new()
        {
            handler ??= new THandler();
            int opcode = handler.Opcode;
            if (opcode <= 0)
                throw new ArgumentException(
                    $"Handler {typeof(THandler).Name} has no valid opcode. Add [NetOpcode(n)] to {typeof(TMessage).Name}.");

            _handlers[opcode] = packet =>
            {
                if (packet.PayloadLength <= 0) return;
                var message = Serializer?.Deserialize<TMessage>(packet.Payload, 0, packet.PayloadLength);
                if (message != null) handler.Handle(message);
            };
        }

        /// <summary>
        /// 注册原始处理器（实现 <see cref="IMessageHandler"/> 的对象）。
        /// <para>Register a raw handler object implementing IMessageHandler.</para>
        /// </summary>
        /// <param name="handler">处理器对象<para>Handler object</para></param>
        public void RegisterHandler(IMessageHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[handler.Opcode] = handler.Handle;
        }

        /// <summary>
        /// 注销处理器
        /// <para>Unregister a handler</para>
        /// </summary>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <returns>是否注销成功<para>Whether unregistered</para></returns>
        public bool UnregisterHandler(int opcode)
        {
            return _handlers.Remove(opcode);
        }

        /// <summary>
        /// 分配一个新的 RpcId（单调递增，会话内唯一）
        /// <para>Allocate a new rpcId (monotonic, unique per session)</para>
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
        /// <param name="onTimeout">超时回调（仅当设置了超时）<para>Timeout callback (only when a timeout is set)</para></param>
        /// <param name="timeoutMilliseconds">超时毫秒数（&lt;=0 表示不超时）<para>Timeout in ms (&lt;=0 means never)</para></param>
        public void AddPendingCall(int rpcId, Action<NetPacket> onResponse, Action onTimeout = null,
            long timeoutMilliseconds = 0)
        {
            if (onResponse == null) throw new ArgumentNullException(nameof(onResponse));

            // int 相加自然回绕（unchecked），与 Environment.TickCount 同域，跨 24.8 天边界也安全
            int deadline = timeoutMilliseconds > 0
                ? unchecked(Environment.TickCount + (int)timeoutMilliseconds)
                : 0;
            _pendingCalls[rpcId] = new PendingCall
            {
                RpcId = rpcId,
                OnResponse = onResponse,
                OnTimeout = onTimeout,
                DeadlineTick = deadline
            };
        }

        /// <summary>
        /// 取消待配对响应
        /// <para>Cancel a pending response</para>
        /// </summary>
        /// <param name="rpcId">请求分配的 RpcId<para>RpcId allocated for the request</para></param>
        /// <returns>是否取消成功<para>Whether cancelled</para></returns>
        public bool RemovePendingCall(int rpcId)
        {
            return _pendingCalls.Remove(rpcId);
        }

        /// <summary>
        /// 派发一个已解码的消息包：rpcId 非 0 时优先配对挂起的 RPC，否则路由到处理器。
        /// <para>Dispatch a decoded packet: non-zero rpcId completes a pending RPC first, else routes to handlers.</para>
        /// </summary>
        /// <param name="packet">已解码的消息包<para>Decoded packet</para></param>
        public void Dispatch(NetPacket packet)
        {
            if (packet.RpcId != 0 && _pendingCalls.TryGetValue(packet.RpcId, out var pending))
            {
                _pendingCalls.Remove(packet.RpcId);
                pending.OnResponse(packet);
                return;
            }

            if (_handlers.TryGetValue(packet.Opcode, out var handler))
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
                pending.OnTimeout?.Invoke();
                RpcTimedOut?.Invoke(rpcId);
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

        #endregion
    }
}