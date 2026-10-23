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
        private readonly ISocket _socket;
        private readonly MessageDispatcher _dispatcher;
        private readonly Kcp _kcp;
        private readonly byte[] _socketRecvBuffer; // socket → kcp.Input
        private byte[] _kcpRecvBuffer;             // kcp.Recv → 帧解析（大消息时扩容，故非 readonly）

        /// <summary> 会话 Id（管理器内唯一） <para>Session id (unique within the manager)</para> </summary>
        public int SessionId { get; }

        /// <summary> 传输层实例 <para>Transport instance</para> </summary>
        public ISocket Socket => _socket;

        /// <summary> 消息分发器 <para>Message dispatcher</para> </summary>
        public MessageDispatcher Dispatcher => _dispatcher;

        /// <summary> KCP 实例 <para>KCP instance</para> </summary>
        public Kcp Kcp => _kcp;

        /// <summary> 是否已连接 <para>Whether connected</para> </summary>
        public bool IsConnected => _socket.IsConnected;

        /// <summary> 远端地址（ip:port） <para>Remote endpoint (ip:port)</para> </summary>
        public string RemoteEndPoint => _socket.RemoteEndPoint ?? string.Empty;

        /// <summary> 链路是否已死亡（重传超限，需重建会话） <para>Whether the link is dead (xmit exceeded; rebuild the session)</para> </summary>
        public bool IsDead => _kcp.IsDead;

        /// <summary>
        /// 创建会话（未连接；调用 <see cref="ConnectAsync"/> 建立连接）。
        /// <para>Create a session (not connected; call ConnectAsync to establish).</para>
        /// </summary>
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
        /// <returns>是否连接成功 <para>Whether connected</para></returns>
        public UniTask<bool> ConnectAsync(string host, int port)
        {
            bool connected = _socket.Connect(host, port);
            return UniTask.FromResult(connected);
        }

        /// <summary>
        /// 发送单向消息（rpcId = 0）。
        /// <para>Send a one-way message (rpcId = 0).</para>
        /// </summary>
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
        public void Send(int opcode, int rpcId, byte[] payload)
        {
            if (!_socket.IsConnected) return;
            byte[] frame = MessagePipe.Encode(opcode, rpcId, payload);
            _kcp.Send(frame, 0, frame.Length);
        }

        /// <summary>
        /// 发起 RPC 请求并等待响应（超时抛出 <see cref="TimeoutException"/>）。
        /// <para>Send an RPC request and await the response (throws TimeoutException on timeout).</para>
        /// </summary>
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
            }, () => tcs.TrySetException(new TimeoutException($"RPC timeout (opcode={opcode}, rpcId={rpcId}, {timeoutSeconds}s).")),
                (long)(timeoutSeconds * 1000f));

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
                if (MessagePipe.TryDecodePacket(_kcpRecvBuffer, out var packet))
                    _dispatcher.Dispatch(packet);
            }
        }

        /// <summary> 关闭会话 <para>Close the session</para> </summary>
        public void Close()
        {
            _socket.Close();
        }
    }

    /// <summary>
    /// 网络协议异常（帧格式错误 / RPC 响应异常等）。
    /// <para>Network protocol exception (bad frame format, abnormal RPC response, etc.).</para>
    /// </summary>
    public sealed class NetProtocolException : Exception
    {
        public NetProtocolException(string message) : base(message)
        {
        }
    }
}
