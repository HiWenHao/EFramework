/*
 * ================================================
 * Describe:      Net module session (one logical connection: KCP + socket + pipe).
 * Author:        Alvin5100
 * CreationTime:  2026-10-23 18:45:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-23 18:45:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 网络会话：一条逻辑连接 = 一个 KCP 实例 + 一个传输层 + 帧编解码。
    /// <para>Network session: one logical connection = one KCP instance + one transport + frame codec.</para>
    /// </summary>
    /// <remarks>
    /// 会话不感知 Unity，可在 .NET 侧独立测试；生命周期由 <see cref="NetworkManager"/> 统一驱动。
    /// <para>The session is Unity-agnostic and testable on the .NET side; its lifecycle is
    /// driven by NetworkManager.</para>
    /// </remarks>
    public sealed class NetworkSession
    {
        #region Fields

        // 传输层
        private readonly ISocket _socket;

        // 消息路由（与其它会话共用）
        private readonly MessageDispatcher _dispatcher;

        // 可靠层实例
        private readonly Kcp _kcp;

        // socket → kcp.Input
        private readonly byte[] _socketRecvBuffer;

        // kcp.Recv → 帧解析（大消息时扩容，故非 readonly）
        private byte[] _kcpRecvBuffer;

        #endregion

        #region Properties

        /// <summary>
        /// 会话 Id（管理器内唯一）
        /// <para>Session id (unique within the manager)</para>
        /// </summary>
        public int SessionId { get; }

        /// <summary>
        /// 传输层实例
        /// <para>Transport instance</para>
        /// </summary>
        public ISocket Socket => _socket;

        /// <summary>
        /// 消息分发器
        /// <para>Message dispatcher</para>
        /// </summary>
        public MessageDispatcher Dispatcher => _dispatcher;

        /// <summary>
        /// KCP 实例
        /// <para>KCP instance</para>
        /// </summary>
        public Kcp Kcp => _kcp;

        /// <summary>
        /// 是否已连接
        /// <para>Whether connected</para>
        /// </summary>
        public bool IsConnected => _socket.IsConnected;

        /// <summary>
        /// 远端地址（ip:port）
        /// <para>Remote endpoint (ip:port)</para>
        /// </summary>
        public string RemoteEndPoint => _socket.RemoteEndPoint ?? string.Empty;

        /// <summary>
        /// 链路是否已死亡（重传超限，需重建会话）
        /// <para>Whether the link is dead (xmit exceeded; rebuild the session)</para>
        /// </summary>
        public bool IsDead => _kcp.IsDead;

        #endregion

        #region Methods

        /// <summary>
        /// 创建会话（未连接；调用 <see cref="ConnectAsync"/> 建立连接）。
        /// <para>Create a session (not connected; call ConnectAsync to establish).</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <param name="conv">KCP 会话号（收发双方必须一致）<para>KCP conversation id (must match on both peers)</para></param>
        /// <param name="socket">传输层实例<para>Transport instance</para></param>
        /// <param name="dispatcher">消息分发器<para>Message dispatcher</para></param>
        /// <param name="settings">调参数（null 用默认）<para>Tuning parameters (null for defaults).</para></param>
        public NetworkSession(int sessionId, uint conv, ISocket socket, MessageDispatcher dispatcher, NetworkSettings settings = null)
        {
            SessionId = sessionId;
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            settings ??= NetworkSettings.Default;

            int mtu = settings.Mtu;
            _kcp = new Kcp(conv, (data, size) => _socket.Send(data, 0, size));
            settings.ApplyTo(_kcp);

            // UDP 包可能大于 mss+24（对方 mtu 可能不同），缓冲区按 64KB 上限留足
            _socketRecvBuffer = new byte[65536];
            _kcpRecvBuffer = new byte[mtu * 4];
        }

        /// <summary>
        /// 异步连接远端（预留握手扩展点：后续在此等待 KCP 首包 / Token 校验）。
        /// <para>Connect to the remote asynchronously (handshake extension point:
        /// wait for the first KCP packet / token validation later).</para>
        /// </summary>
        /// <param name="host">远端地址<para>Remote host</para></param>
        /// <param name="port">远端端口<para>Remote port</para></param>
        /// <returns>是否连接成功<para>Whether connected</para></returns>
        public UniTask<bool> ConnectAsync(string host, int port)
        {
            bool connected = _socket.Connect(host, port);
            return UniTask.FromResult(connected);
        }

        /// <summary>
        /// 发送单向消息（rpcId = 0）。
        /// <para>Send a one-way message (rpcId = 0).</para>
        /// </summary>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="payload">载荷字节<para>Payload bytes</para></param>
        public void Send(int opcode, byte[] payload = null)
        {
            if (!_socket.IsConnected) return;
            byte[] frame = MessagePipe.Encode(opcode, 0, payload);
            _kcp.Send(frame, 0, frame.Length);
        }

        /// <summary>
        /// 发送 RPC 请求（自动分配 rpcId，等待响应由 <see cref="Call{TResponse}"/> 完成）。
        /// <para>Send an RPC request (rpcId auto-allocated; the response completes Call{TResponse}).</para>
        /// </summary>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="rpcId">请求配对号<para>Request pairing id</para></param>
        /// <param name="payload">载荷字节<para>Payload bytes</para></param>
        public void Send(int opcode, int rpcId, byte[] payload)
        {
            if (!_socket.IsConnected) return;
            byte[] frame = MessagePipe.Encode(opcode, rpcId, payload);
            _kcp.Send(frame, 0, frame.Length);
        }

        /// <summary>
        /// 发起 RPC 请求并等待响应（超时抛出 <see cref="TimeoutException"/>；会话关闭时抛出
        /// <see cref="NetProtocolException"/>）。
        /// <para>Send an RPC request and await the response (throws TimeoutException on timeout;
        /// throws NetProtocolException when the session closes).</para>
        /// </summary>
        /// <typeparam name="TResponse">响应消息类型<para>Response message type</para></typeparam>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="payload">载荷字节<para>Payload bytes</para></param>
        /// <param name="timeoutSeconds">超时秒数<para>Timeout in seconds</para></param>
        /// <returns>响应消息<para>Response message</para></returns>
        public UniTask<TResponse> Call<TResponse>(int opcode, byte[] payload, float timeoutSeconds = 10f)
        {
            if (!_socket.IsConnected)
                return UniTask.FromException<TResponse>(new NetProtocolException("Session is not connected."));

            int rpcId = _dispatcher.NextRpcId();
            var tcs = new UniTaskCompletionSource<TResponse>();

            _dispatcher.AddPendingCall(rpcId, packet =>
            {
                var serializer = _dispatcher.Serializer;
                if (serializer == null)
                {
                    tcs.TrySetException(new NetProtocolException("Serializer is not set; cannot decode RPC response."));
                    return;
                }
                if (packet.PayloadLength <= 0)
                {
                    tcs.TrySetException(new NetProtocolException($"Empty RPC response payload (opcode={packet.Opcode}, rpcId={rpcId})."));
                    return;
                }
                try
                {
                    var response = serializer.Deserialize<TResponse>(packet.Payload, 0, packet.PayloadLength);
                    tcs.TrySetResult(response);
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            }, failure =>
            {
                switch (failure)
                {
                    case NetRpcFailure.Timeout:
                        tcs.TrySetException(new TimeoutException($"RPC timeout (opcode={opcode}, rpcId={rpcId}, {timeoutSeconds}s)."));
                        break;
                    case NetRpcFailure.SessionClosed:
                        tcs.TrySetException(new NetProtocolException($"Session closed before RPC response (opcode={opcode}, rpcId={rpcId})."));
                        break;
                }
            }, (long)(timeoutSeconds * 1000f), SessionId);

            Send(opcode, rpcId, payload);

            return tcs.Task;
        }

        /// <summary>
        /// 驱动会话：socket → KCP → 帧解析 → 分发。由 <see cref="NetworkManager"/> 每帧调用。
        /// <para>Drive the session: socket → KCP → frame parse → dispatch. Called by NetworkManager each frame.</para>
        /// </summary>
        public void Update()
        {
            if (!_socket.IsConnected) return;

            // 1. 下端数据喂给 KCP（分片重排 / 确认处理在此完成）
            while (true)
            {
                int received = _socket.TryReceive(_socketRecvBuffer, 0, _socketRecvBuffer.Length);
                if (received <= 0) break;
                if (_kcp.Input(_socketRecvBuffer, 0, received) < 0) break;
            }

            // 2. 推进 KCP（发送 / 重传 / ACK / 探测）
            _kcp.Update();

            // 3. 取出完整消息帧并分发（一条 Send 对应一次 Recv）
            while (true)
            {
                int size = _kcp.Recv(_kcpRecvBuffer, 0);
                if (size == -3)
                {
                    // 缓冲区小于下一条完整消息：扩容后重试（消息仍留在队列，无丢失）
                    Array.Resize(ref _kcpRecvBuffer, _kcpRecvBuffer.Length * 2);
                    continue;
                }
                if (size <= 0) break;
                if (MessagePipe.TryDecodePacket(SessionId, _kcpRecvBuffer, out var packet))
                    _dispatcher.Dispatch(packet);
            }
        }

        /// <summary>
        /// 关闭会话（并取消本会话挂起的 RPC，等待方立即收到失败通知）。
        /// <para>Close the session (and cancel its pending RPCs so waiters fail immediately).</para>
        /// </summary>
        public void Close()
        {
            _dispatcher.CancelPendingCalls(SessionId);
            _socket.Close();
        }

        #endregion
    }

    /// <summary>
    /// 网络协议异常（会话未连接、载荷非法、会话中途关闭等）。
    /// <para>Network protocol exception (not connected, malformed payload, session closed midway).</para>
    /// </summary>
    public sealed class NetProtocolException : Exception
    {
        #region Methods

        /// <summary>
        /// 构造协议异常。
        /// <para>Create a protocol exception.</para>
        /// </summary>
        /// <param name="message">异常描述<para>Exception message</para></param>
        public NetProtocolException(string message) : base(message)
        {
        }

        #endregion
    }
}
