/*
 * ================================================
 * Describe:      Net module connection lifecycle (handshake + heartbeat + reconnect).
 * Author:        Alvin5100
 * CreationTime:  2026-10-26 17:45:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-26 17:45:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;
using System.Diagnostics;
using System.Text;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 连接生命周期状态。
    /// <para>Connection lifecycle state.</para>
    /// </summary>
    public enum NetConnectionState
    {
        /// <summary>
        /// 空闲（尚未发起连接）
        /// <para>Idle (no connection attempt yet)</para>
        /// </summary>
        Idle = 0,

        /// <summary>
        /// 正在建立传输通道
        /// <para>Establishing the transport channel</para>
        /// </summary>
        Connecting = 1,

        /// <summary>
        /// 等待握手结果
        /// <para>Waiting for the handshake result</para>
        /// </summary>
        Handshaking = 2,

        /// <summary>
        /// 已就绪，可正常收发业务消息
        /// <para>Ready; business messages can flow</para>
        /// </summary>
        Connected = 3,

        /// <summary>
        /// 断线后等待重连（退避计时中）
        /// <para>Waiting to reconnect after a drop (backoff timer running)</para>
        /// </summary>
        Reconnecting = 4,

        /// <summary>
        /// 已关闭（终态，不再重连）
        /// <para>Closed (terminal; no more reconnects)</para>
        /// </summary>
        Closed = 5,
    }

    /// <summary>
    /// 逻辑连接：管理一条「对外连接」的完整生命周期 —— 建立 → 握手 → 保活 → 断线 → 退避重连。
    /// <para>Logical connection: owns the full lifecycle of one outbound link — establish, handshake,
    /// keep-alive, drop, and backoff-based reconnect.</para>
    /// </summary>
    /// <remarks>
    /// <para><b>会话 Id 稳定性</b>：重连会重建底层会话（新的 socket / conv），但 <see cref="SessionId"/>
    /// 保持不变 —— 这样按会话注册的处理器（<c>RegisterHandler(sessionId, opcode, handler)</c>）在重连后依然有效。</para>
    /// <para><b>Session id stability</b>: a reconnect rebuilds the underlying session (new socket / conv)
    /// but keeps <see cref="SessionId"/> stable, so session-scoped handlers stay registered across reconnects.</para>
    /// <para>本类不依赖 Unity，由 <see cref="NetworkManager"/> 每帧驱动。</para>
    /// <para>Unity-free; driven each frame by <see cref="NetworkManager"/>.</para>
    /// </remarks>
    public sealed class NetConnection
    {
        #region Fields

        // 目标地址
        private readonly string _host;
        private readonly int _port;

        // 调参与生命周期策略
        private readonly NetworkSettings _settings;

        // 传输层工厂（null 用默认 UdpSocket；微信平台注入 WX 实现）
        private readonly Func<ISocket> _socketFactory;

        // 消息分发器
        private readonly MessageDispatcher _dispatcher;

        // conv 分配器（由管理器统一分配，保证全局唯一）
        private readonly Func<uint> _convAllocator;

        // 握手 Token（null/空 = 跳过握手）
        private byte[] _token;

        // 当前底层会话（重连时重建）
        private NetworkSession _session;

        // 当前状态
        private NetConnectionState _state = NetConnectionState.Idle;

        // 重连相关
        private int _reconnectAttempt;
        private double _nextRetryTime;
        private bool _reconnectPending;

        // 心跳相关
        private double _lastHeartbeatTime;

        // 握手相关
        private double _handshakeStartTime;
        private bool _handshakePending;

        // 本次连接是否已对外发布过「已连接」事件（避免重复发布）
        private bool _announcedConnected;

        // 用户主动关闭（终态，不再重连）
        private bool _closedByUser;

        #endregion

        #region Properties

        /// <summary>
        /// 会话 Id（管理器内唯一，重连后保持不变）
        /// <para>Session id (unique within the manager; stable across reconnects)</para>
        /// </summary>
        public int SessionId { get; }

        /// <summary>
        /// 当前状态
        /// <para>Current state</para>
        /// </summary>
        public NetConnectionState State => _state;

        /// <summary>
        /// 当前底层会话（未连接时为 null）
        /// <para>Current underlying session (null when not connected)</para>
        /// </summary>
        public NetworkSession Session => _session;

        /// <summary>
        /// 远端地址描述
        /// <para>Remote endpoint description</para>
        /// </summary>
        public string RemoteEndPoint => _session?.RemoteEndPoint ?? $"{_host}:{_port}";

        /// <summary>
        /// 当前退避档位（每次失败递增，连接成功后归零 —— 决定下一次退避时长）
        /// <para>Current backoff step (increments on each failure, resets on success — it drives the next delay)</para>
        /// </summary>
        public int ReconnectAttempt => _reconnectAttempt;

        /// <summary>
        /// 累计重连次数（连接成功后不清零，用于统计与诊断）
        /// <para>Lifetime reconnect count (not reset on success; for statistics and diagnostics)</para>
        /// </summary>
        public int TotalReconnectCount { get; private set; }

        /// <summary>
        /// 距下次重连的剩余秒数（不在重连态时为 0）
        /// <para>Seconds until the next reconnect (0 when not reconnecting)</para>
        /// </summary>
        public float NextRetryIn => _reconnectPending ? (float)Math.Max(0d, _nextRetryTime - Now()) : 0f;

        /// <summary>
        /// 握手 Token（可随时替换，重连时使用最新值 —— 适配短时效 Token 刷新）
        /// <para>Handshake token (replaceable at any time; the latest value is used on reconnect —
        /// supports short-lived token refresh)</para>
        /// </summary>
        public byte[] Token
        {
            get => _token;
            set => _token = value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// 创建连接（尚未发起连接，调用 <see cref="Start"/> 开始）。
        /// <para>Create a connection (not yet started; call <see cref="Start"/> to begin).</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <param name="host">目标地址<para>Target host</para></param>
        /// <param name="port">目标端口<para>Target port</para></param>
        /// <param name="dispatcher">消息分发器<para>Message dispatcher</para></param>
        /// <param name="convAllocator">conv 分配器（保证全局唯一）<para>Conv allocator (globally unique)</para></param>
        /// <param name="settings">调参与生命周期策略（null 用默认）<para>Tuning and lifecycle policy (null for defaults)</para></param>
        /// <param name="socketFactory">传输层工厂（null 用默认）<para>Transport factory (null for the default)</para></param>
        /// <param name="token">握手 Token（null/空 = 跳过握手）<para>Handshake token (null/empty skips the handshake)</para></param>
        public NetConnection(int sessionId, string host, int port, MessageDispatcher dispatcher,
            Func<uint> convAllocator, NetworkSettings settings = null, Func<ISocket> socketFactory = null,
            byte[] token = null)
        {
            SessionId = sessionId;
            _host = host;
            _port = port;
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _convAllocator = convAllocator ?? throw new ArgumentNullException(nameof(convAllocator));
            _settings = settings ?? NetworkSettings.Default;
            _socketFactory = socketFactory;
            _token = token;

            // 握手结果由连接自己路由（会话级注册，重连后依然有效）
            _dispatcher.RegisterHandler(sessionId, NetOpcode.HandshakeResult, OnHandshakePacket);
        }

        // 收到握手结果包 → 解析并交给状态机
        private void OnHandshakePacket(NetPacket packet)
        {
            int resultCode = 0;
            string message = string.Empty;
            if (packet.PayloadLength > 0)
            {
                resultCode = packet.Payload[0];
                if (packet.PayloadLength > 1)
                    message = Encoding.UTF8.GetString(packet.Payload, 1, packet.PayloadLength - 1);
            }
            OnHandshakeResult(resultCode, message);
        }

        /// <summary>
        /// 发起连接（立即尝试一次；失败则按退避策略排队重试）。
        /// <para>Start the connection (one immediate attempt; failures queue a backoff retry).</para>
        /// </summary>
        public void Start()
        {
            if (_state != NetConnectionState.Idle) return;
            TryConnect();
        }

        /// <summary>
        /// 每帧驱动：收发 → 心跳 → 握手超时 → 重连调度。由 <see cref="NetworkManager"/> 调用。
        /// <para>Per-frame drive: send/receive, heartbeat, handshake timeout, reconnect scheduling.
        /// Called by <see cref="NetworkManager"/>.</para>
        /// </summary>
        public void Update()
        {
            if (_state == NetConnectionState.Closed) return;

            // 1. 收发
            // 注意：_session.Update() 内部会同步派发消息，握手结果处理可能在此期间
            // 关闭/重建会话（_session 被置空或替换）—— 必须用局部引用并在返回后重新校验。
            var session = _session;
            if (session != null)
            {
                session.Update();

                // 会话已被回调链关闭或替换 → 本帧后续逻辑不再适用
                if (_session != session) return;

                if (!session.IsConnected)
                {
                    HandleLinkLost("Socket closed");
                    return;
                }

                if (session.IsDead)
                {
                    HandleLinkLost("Link dead");
                    return;
                }
            }

            // 2. 握手超时
            if (_handshakePending && Now() - _handshakeStartTime > _settings.HandshakeTimeout)
            {
                _handshakePending = false;
                HandleLinkLost("Handshake timeout");
                return;
            }

            // 3. 心跳保活
            if (_state == NetConnectionState.Connected && _settings.HeartbeatEnabled)
            {
                double now = Now();

                // 活性判定：以「最近一次收到任何消息」为准（不是心跳的往返）
                // 注意：必须取会话的接收时刻，不能用本地缓存的 _lastResponseTime ——
                // 后者只在连接建立时赋值，会导致连接存活满 HeartbeatTimeout 后被误判为死链。
                double lastResponse = _session?.LastReceiveTime ?? now;
                if (now - lastResponse > _settings.HeartbeatTimeout)
                {
                    HandleLinkLost("Heartbeat timeout");
                    return;
                }

                // 到点发心跳
                if (now - _lastHeartbeatTime >= _settings.HeartbeatInterval)
                {
                    _lastHeartbeatTime = now;
                    SendHeartbeat();
                }
            }

            // 4. 重连调度
            if (_reconnectPending && Now() >= _nextRetryTime)
            {
                _reconnectPending = false;
                TryConnect();
            }
        }

        /// <summary>
        /// 主动关闭（终态，不再重连）。
        /// <para>Close explicitly (terminal; no more reconnects).</para>
        /// </summary>
        /// <param name="reason">原因简述<para>Reason</para></param>
        public void Close(string reason)
        {
            if (_state == NetConnectionState.Closed) return;

            _closedByUser = true;
            _reconnectPending = false;
            _handshakePending = false;
            CloseSession();
            SetState(NetConnectionState.Closed);
        }

        /// <summary>
        /// 处理服务端下发的握手结果。
        /// <para>Handle a handshake result pushed by the server.</para>
        /// </summary>
        /// <param name="resultCode">结果码（0 = 成功）<para>Result code (0 = success)</para></param>
        /// <param name="message">失败原因（可空）<para>Failure reason (may be empty)</para></param>
        public void OnHandshakeResult(int resultCode, string message)
        {
            if (!_handshakePending) return;
            _handshakePending = false;

            if (resultCode == 0)
            {
                NetHandshakeResult?.Invoke(this, true, 0, string.Empty);
                OnConnected();
                return;
            }

            string reason = string.IsNullOrEmpty(message) ? $"Handshake rejected (code={resultCode})" : message;
            NetHandshakeResult?.Invoke(this, false, resultCode, reason);

            // 握手失败：按策略决定继续重试还是放弃
            if (_settings.HandshakeFailureRetries)
            {
                HandleLinkLost(reason);
            }
            else
            {
                _closedByUser = true;
                CloseSession();
                SetState(NetConnectionState.Closed);
            }
        }

        /// <summary>
        /// 链路丢失（内部判定或服务端通知）：断开当前会话并按退避策略排队重连。
        /// <para>Link lost (detected internally or notified by the server): drop the current session
        /// and queue a backoff reconnect.</para>
        /// </summary>
        /// <param name="reason">原因简述<para>Reason</para></param>
        public void HandleLinkLost(string reason)
        {
            if (_state == NetConnectionState.Closed) return;

            bool wasConnected = _announcedConnected;
            _announcedConnected = false;
            _handshakePending = false;

            CloseSession();

            if (wasConnected)
                NetDisconnected?.Invoke(this, reason);

            if (_closedByUser || !_settings.AutoReconnect)
            {
                SetState(NetConnectionState.Closed);
                return;
            }

            _reconnectAttempt++;
            TotalReconnectCount++;
            float delay = _settings.GetReconnectDelay(_reconnectAttempt);
            _nextRetryTime = Now() + delay;
            _reconnectPending = true;
            SetState(NetConnectionState.Reconnecting);
            NetReconnectScheduled?.Invoke(this, _reconnectAttempt, delay, reason);
        }

        /// <summary>
        /// 尝试建立连接（创建 socket 与会话，必要时发起握手）。
        /// <para>Attempt to establish the connection (create the socket and session, handshake if needed).</para>
        /// </summary>
        private void TryConnect()
        {
            SetState(NetConnectionState.Connecting);

            ISocket socket = _socketFactory != null ? _socketFactory() : new UdpSocket();
            if (!socket.Connect(_host, _port))
            {
                socket.Close();

                // 传输层都建不起来 → 视为一次失败尝试，走退避重试
                ScheduleReconnect("Socket connect failed");
                return;
            }

            // 每次连接尝试用新的 conv（服务端按 conv 区分会话，复用旧 conv 会导致状态错乱）
            _session = new NetworkSession(SessionId, _convAllocator(), socket, _dispatcher, _settings);

            double now = Now();
            _lastHeartbeatTime = now;

            if (_token != null && _token.Length > 0)
            {
                // 需要握手：发握手请求并等待结果
                _handshakeStartTime = now;
                _handshakePending = true;
                SetState(NetConnectionState.Handshaking);
                _session.Send(NetOpcode.Handshake, _token);
            }
            else
            {
                // 无 Token：跳过握手，直接就绪
                OnConnected();
            }
        }

        // 会话就绪
        private void OnConnected()
        {
            _reconnectAttempt = 0;
            _announcedConnected = true;
            SetState(NetConnectionState.Connected);
            NetConnected?.Invoke(this);
        }

        // 发送心跳
        private void SendHeartbeat()
        {
            if (_session == null) return;

            // 载荷携带客户端时间戳，服务端原样回显 → 客户端据此算 RTT
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _session.Send(NetOpcode.Ping, BitConverter.GetBytes(timestamp));
        }

        // 排队一次重连（退避计时）
        private void ScheduleReconnect(string reason)
        {
            _reconnectAttempt++;
            TotalReconnectCount++;
            float delay = _settings.GetReconnectDelay(_reconnectAttempt);
            _nextRetryTime = Now() + delay;
            _reconnectPending = true;
            SetState(NetConnectionState.Reconnecting);
            NetReconnectScheduled?.Invoke(this, _reconnectAttempt, delay, reason);
        }

        // 关闭并释放当前会话
        private void CloseSession()
        {
            if (_session == null) return;
            _session.Close();
            _session = null;
        }

        // 迁移状态并广播
        private void SetState(NetConnectionState state)
        {
            if (_state == state) return;
            NetConnectionState old = _state;
            _state = state;
            StateChanged?.Invoke(this, old, state);
        }

        // 当前时刻（秒，单调时钟，不受系统时间调整影响）
        private static double Now() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

        #endregion

        #region 事件 / Events

        /// <summary>
        /// 会话就绪（握手通过或无需握手）
        /// <para>The session is ready (handshake passed, or no handshake was required)</para>
        /// </summary>
        public event Action<NetConnection> NetConnected;

        /// <summary>
        /// 链路断开（已就绪后掉线）
        /// <para>The link dropped after being ready</para>
        /// </summary>
        public event Action<NetConnection, string> NetDisconnected;

        /// <summary>
        /// 排队了一次重连（参数：连接、重试次数、退避秒数、原因）
        /// <para>A reconnect was queued (connection, attempt, backoff seconds, reason)</para>
        /// </summary>
        public event Action<NetConnection, int, float, string> NetReconnectScheduled;

        /// <summary>
        /// 握手结果（参数：连接、是否成功、结果码、原因）
        /// <para>Handshake result (connection, success, result code, reason)</para>
        /// </summary>
        public event Action<NetConnection, bool, int, string> NetHandshakeResult;

        /// <summary>
        /// 状态迁移（参数：连接、旧状态、新状态）
        /// <para>State transition (connection, old state, new state)</para>
        /// </summary>
        public event Action<NetConnection, NetConnectionState, NetConnectionState> StateChanged;

        #endregion
    }
}
