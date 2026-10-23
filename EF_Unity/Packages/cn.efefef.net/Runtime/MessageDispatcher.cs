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
using Cysharp.Threading.Tasks;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 消息分发器：按 Opcode 路由到处理器，并按 RpcId 配对请求/响应。
    /// <para>Message dispatcher: routes by opcode and pairs requests/responses by rpcId.</para>
    /// </summary>
    public sealed class MessageDispatcher
    {
        /// <summary> 待确认的 RPC 调用 <para>A pending RPC call</para> </summary>
        private sealed class PendingCall
        {
            public int RpcId;
            public Action<NetPacket> OnResponse;
            public Action OnTimeout;
            public int DeadlineTick; // 0 = 永不超时（与 Environment.TickCount 同域，自然回绕安全）
        }

        private readonly Dictionary<int, Action<NetPacket>> _handlers = new Dictionary<int, Action<NetPacket>>();
        private readonly Dictionary<int, PendingCall> _pendingCalls = new Dictionary<int, PendingCall>();
        private int _rpcIdSeed = 1;

        /// <summary>
        /// 序列化器（类型化 Handler 与 RPC 响应需要；不使用类型化功能时可为 null）。
        /// <para>Serializer for typed handlers and RPC responses; may be null when unused.</para>
        /// </summary>
        public IMessageSerializer Serializer { get; set; }

        /// <summary> RPC 超时回调（参数为 rpcId） <para>RPC timeout callback (rpcId)</para> </summary>
        public event Action<int> RpcTimedOut;

        /// <summary>
        /// 注册原始处理器（直接接收 <see cref="NetPacket"/>）。
        /// <para>Register a raw handler (receives NetPacket directly).</para>
        /// </summary>
        public void RegisterHandler(int opcode, Action<NetPacket> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[opcode] = handler;
        }

        /// <summary>
        /// 注册类型化处理器：Opcode 取自 <see cref="IMessageHandler{TMessage}.Opcode"/>，
        /// 载荷经 <see cref="Serializer"/> 反序列化后回调。
        /// <para>Register a typed handler: opcode from IMessageHandler&lt;TMessage&gt;.Opcode;
        /// payload deserialized via Serializer before the callback.</para>
        /// </summary>
        /// <param name="handler">处理器实例；传 null 时按无参构造创建 <para>Handler instance; created via parameterless ctor when null</param>
        public void RegisterHandler<TMessage, THandler>(THandler handler = null)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>, new()
        {
            handler ??= new THandler();
            int opcode = handler.Opcode;
            if (opcode <= 0)
                throw new ArgumentException($"Handler {typeof(THandler).Name} has no valid opcode. Add [NetOpcode(n)] to {typeof(TMessage).Name}.");

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
        public void RegisterHandler(IMessageHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _handlers[handler.Opcode] = handler.Handle;
        }

        /// <summary> 注销处理器 <para>Unregister a handler</para> </summary>
        public bool UnregisterHandler(int opcode)
        {
            return _handlers.Remove(opcode);
        }

        /// <summary> 分配一个新的 RpcId（单调递增，会话内唯一） <para>Allocate a new rpcId (monotonic, unique per session)</para> </summary>
        public int NextRpcId()
        {
            return _rpcIdSeed++;
        }

        /// <summary>
        /// 登记一个待配对响应。
        /// <para>Register a pending response to be paired.</para>
        /// </summary>
        /// <param name="rpcId">请求分配的 RpcId <para>RpcId allocated for the request</para></param>
        /// <param name="onResponse">响应到达时的回调 <para>Callback when the response arrives</para></param>
        /// <param name="onTimeout">超时回调（仅当设置了超时） <para>Timeout callback (only when a timeout is set)</para></param>
        /// <param name="timeoutMilliseconds">超时毫秒数（&lt;=0 表示不超时） <para>Timeout in ms (&lt;=0 means never)</para></param>
        public void AddPendingCall(int rpcId, Action<NetPacket> onResponse, Action onTimeout = null, long timeoutMilliseconds = 0)
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

        /// <summary> 取消待配对响应 <para>Cancel a pending response</para> </summary>
        public bool RemovePendingCall(int rpcId)
        {
            return _pendingCalls.Remove(rpcId);
        }

        /// <summary>
        /// 派发一个已解码的消息包：rpcId 非 0 时优先配对挂起的 RPC，否则路由到处理器。
        /// <para>Dispatch a decoded packet: non-zero rpcId completes a pending RPC first, else routes to handlers.</para>
        /// </summary>
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
        /// <param name="currentTick">当前时间戳（Environment.TickCount） <para>Current tick (Environment.TickCount)</param>
        public void CheckTimeouts(int currentTick)
        {
            if (_pendingCalls.Count == 0) return;

            List<int> expired = null;
            foreach (var kvp in _pendingCalls)
            {
                if (kvp.Value.DeadlineTick == 0) continue;
                if (currentTick - kvp.Value.DeadlineTick >= 0)
                {
                    expired ??= new List<int>();
                    expired.Add(kvp.Key);
                }
            }

            if (expired == null) return;
            foreach (int rpcId in expired)
            {
                if (_pendingCalls.Remove(rpcId, out var pending))
                {
                    pending.OnTimeout?.Invoke();
                    RpcTimedOut?.Invoke(rpcId);
                }
            }
        }

        /// <summary> 清空所有处理器与挂起调用 <para>Clear all handlers and pending calls</para> </summary>
        public void Clear()
        {
            _handlers.Clear();
            _pendingCalls.Clear();
        }
    }
}
