/*
 * ================================================
 * Describe:      Net module manager (EFC-driven, drives connections and publishes events).
 * Author:        Alvin5100
 * CreationTime:  2026-10-23 18:45:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-26 18:00:00
 * ScriptVersion: 0.5
 * ===============================================
 */

using System;
using System.Collections.Generic;
using System.Text;
using EasyFramework.Managers;
using EasyFramework.Managers.Event;
using Cysharp.Threading.Tasks;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 网络管理器: 统一驱动所有连接的收发与生命周期，断线/错误经 [ EventManager ] 广播。
    /// <para>Network manager: drives all connections' send/receive and lifecycle, broadcasts
    /// disconnects/errors through [ EventManager ].</para>
    /// </summary>
    /// <remarks>
    /// <para>一般只有一条连接（连一个游戏服）；多连接场景（如同时连登录服与战斗服）下，
    /// 可用 <see cref="RegisterHandler(int, int, Action{NetPacket})"/> 按会话注册处理器，
    /// 或用 <see cref="GetSession"/> 按 Id 取会话分别收发。</para>
    /// <para>Usually a single connection (one game server). With several connections (e.g. a login server
    /// and a battle server at once), register session-scoped handlers via
    /// RegisterHandler(int, int, Action{NetPacket}) or fetch sessions by id via GetSession.</para>
    /// <para><b>Order 说明</b>：网络收包必须早于业务逻辑处理，否则本帧收到的数据要等下一帧
    /// 才能被业务消费（真机 60fps 下白白多等 16.7ms）。故取 88000 —— 落在框架预留的外部包区间
    /// （10000~89000）内，排在所有框架管理器（89100+）之后、所有业务层管理器（0）之前。</para>
    /// <para><b>Order note</b>: inbound packets must be processed before business logic, otherwise
    /// data received this frame is only visible to business code one frame later (+16.7ms at 60fps).
    /// 88000 sits in the reserved external-package band (10000~89000): after all framework managers
    /// (89100+) and before all business managers (0).</para>
    /// </remarks>
    [Manager(Order = 88000)]
    [Dependency(typeof(EventManager))]
    public sealed class NetworkManager : MonoSingleton<NetworkManager>, ISingleton, IUpdate
    {
        #region Fields

        // 下一个 KCP 会话号（conv，双方必须一致）
        private uint _nextConv = 1;

        // 下一个会话 Id
        private int _nextSessionId = 1;

        // 全部逻辑连接
        private readonly List<NetConnection> _connections = new List<NetConnection>();

        // sessionId → 连接
        private readonly Dictionary<int, NetConnection> _connectionMap = new Dictionary<int, NetConnection>();

        // 全局消息路由
        private readonly MessageDispatcher _dispatcher = new MessageDispatcher();

        #endregion

        #region Properties

        /// <summary>
        /// 全局消息分发器（业务在此注册 Handler / 设置序列化器）
        /// <para>Global dispatcher (register handlers / serializer here)</para>
        /// </summary>
        public MessageDispatcher Dispatcher => _dispatcher;

        /// <summary>
        /// 序列化器（透传给分发器）
        /// <para>Serializer (forwarded to the dispatcher)</para>
        /// </summary>
        public IMessageSerializer Serializer
        {
            get => _dispatcher.Serializer;
            set => _dispatcher.Serializer = value;
        }

        /// <summary>
        /// 是否暂停驱动（暂停期间不收发、不检查超时、不心跳）
        /// <para>Whether the driving is paused (no send/receive, no timeout check, no heartbeat)</para>
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 逻辑连接数量
        /// <para>Number of logical connections</para>
        /// </summary>
        public int ConnectionCount => _connections.Count;

        /// <summary>
        /// 逻辑连接列表（只读）
        /// <para>Logical connection list (read-only)</para>
        /// </summary>
        public IReadOnlyList<NetConnection> Connections => _connections;

        /// <summary>
        /// 当前活跃会话数量（已就绪的连接数）
        /// <para>Number of active sessions (ready connections)</para>
        /// </summary>
        public int SessionCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _connections.Count; i++)
                {
                    if (_connections[i].State == NetConnectionState.Connected) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 当前活跃会话列表（只读快照；多会话场景下按 <see cref="NetworkSession.SessionId"/> 区分）
        /// <para>Active session list (read-only snapshot; distinguish by NetworkSession.SessionId
        /// when several sessions exist)</para>
        /// </summary>
        public IReadOnlyList<NetworkSession> Sessions
        {
            get
            {
                var list = new List<NetworkSession>(_connections.Count);
                for (int i = 0; i < _connections.Count; i++)
                {
                    var session = _connections[i].Session;
                    if (session != null) list.Add(session);
                }
                return list;
            }
        }

        #endregion

        #region Methods

        // 初始化（EFC 注册时调用）
        void ISingleton.Init()
        {
            IsPaused = false;
            _dispatcher.RpcTimedOut -= OnRpcTimedOut;
            _dispatcher.RpcTimedOut += OnRpcTimedOut;
        }

        // 每帧驱动：推进所有连接 + 检查 RPC 超时
        void IUpdate.Update(float elapse, float realElapse)
        {
            if (IsPaused) return;

            for (int i = _connections.Count - 1; i >= 0; i--)
            {
                var connection = _connections[i];
                connection.Update();

                // 连接已终态关闭 → 移出管理
                if (connection.State == NetConnectionState.Closed)
                {
                    RemoveConnection(connection);
                }
            }

            _dispatcher.CheckTimeouts(Environment.TickCount);
        }

        // 退出（EFC 注销时调用）：关闭所有连接
        void ISingleton.Quit()
        {
            _dispatcher.RpcTimedOut -= OnRpcTimedOut;
            CloseAll("Network Manager quit");
        }

        // 移除连接（并注销其会话级处理器）
        private void RemoveConnection(NetConnection connection)
        {
            if (!_connectionMap.Remove(connection.SessionId)) return;
            _connections.Remove(connection);
            _dispatcher.UnregisterSessionHandlers(connection.SessionId);
        }

        // RPC 超时
        private void OnRpcTimedOut(int rpcId, int sessionId)
        {
            EventManager.Instance?.Publish(new NetErrorEvent(sessionId, $"RPC timeout ( rpcId = {rpcId} )."));
        }

        /// <summary>
        /// 创建并连接一个会话（UDP + KCP）。
        /// <para>Create and connect a session (UDP + KCP).</para>
        /// </summary>
        /// <param name="host">服务端地址<para>Server host</para></param>
        /// <param name="port">服务端端口<para>Server port</para></param>
        /// <param name="settings">调参数（null 用默认）<para>Tuning parameters (null for defaults).</para></param>
        /// <param name="socketFactory">自定义传输层工厂（null 用 <see cref="UdpSocket"/>；微信平台在此注入 WX 实现）<para>Custom transport factory (null uses UdpSocket; inject the WX implementation here on WeChat).</para></param>
        /// <returns>已连接的会话；失败返回 null<para>Connected session; null on failure</para></returns>
        public NetworkSession Connect(string host, int port, NetworkSettings settings = null, Func<ISocket> socketFactory = null)
        {
            return Connect(host, port, settings, socketFactory, null);
        }

        /// <summary>
        /// 创建并连接一个会话（UDP + KCP），可带握手 Token。
        /// <para>Create and connect a session (UDP + KCP) with an optional handshake token.</para>
        /// </summary>
        /// <param name="host">服务端地址<para>Server host</para></param>
        /// <param name="port">服务端端口<para>Server port</para></param>
        /// <param name="settings">调参数（null 用默认）<para>Tuning parameters (null for defaults).</para></param>
        /// <param name="socketFactory">自定义传输层工厂（null 用 <see cref="UdpSocket"/>）<para>Custom transport factory (null uses UdpSocket).</para></param>
        /// <param name="token">握手 Token（null/空 = 跳过握手，直接就绪）<para>Handshake token (null/empty skips the handshake and goes straight to ready)</para></param>
        /// <returns>已连接的会话；失败返回 null<para>Connected session; null on failure</para></returns>
        public NetworkSession Connect(string host, int port, NetworkSettings settings, Func<ISocket> socketFactory,
            byte[] token)
        {
            int sessionId = _nextSessionId++;

            var connection = new NetConnection(sessionId, host, port, _dispatcher, AllocateConv,
                settings, socketFactory, token);
            _connections.Add(connection);
            _connectionMap[sessionId] = connection;

            HookConnection(connection);
            connection.Start();

            return connection.Session;
        }

        /// <summary>
        /// 创建并连接一个会话（带字符串 Token 握手，UTF-8 编码）。
        /// <para>Create and connect a session with a string handshake token (UTF-8 encoded).</para>
        /// </summary>
        /// <param name="host">服务端地址<para>Server host</para></param>
        /// <param name="port">服务端端口<para>Server port</para></param>
        /// <param name="token">握手 Token（null/空 = 跳过握手）<para>Handshake token (null/empty skips the handshake)</para></param>
        /// <param name="settings">调参数（null 用默认）<para>Tuning parameters (null for defaults).</para></param>
        /// <param name="socketFactory">自定义传输层工厂（null 用 <see cref="UdpSocket"/>）<para>Custom transport factory (null uses UdpSocket).</para></param>
        /// <returns>已连接的会话；失败返回 null<para>Connected session; null on failure</para></returns>
        public NetworkSession Connect(string host, int port, string token, NetworkSettings settings = null,
            Func<ISocket> socketFactory = null)
        {
            byte[] tokenBytes = string.IsNullOrEmpty(token) ? null : Encoding.UTF8.GetBytes(token);
            return Connect(host, port, settings, socketFactory, tokenBytes);
        }

        /// <summary>
        /// 按 ID 获取会话（重连后会话对象会重建，请按需重新获取）
        /// <para>Get a session by id (the session object is rebuilt on reconnect; re-fetch as needed)</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <returns>会话；不存在返回 null<para>Session; null when absent</para></returns>
        public NetworkSession GetSession(int sessionId)
        {
            return _connectionMap.TryGetValue(sessionId, out var connection) ? connection.Session : null;
        }

        /// <summary>
        /// 按 ID 获取连接
        /// <para>Get a connection by id</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <returns>连接；不存在返回 null<para>Connection; null when absent</para></returns>
        public NetConnection GetConnection(int sessionId)
        {
            return _connectionMap.GetValueOrDefault(sessionId);
        }

        /// <summary>
        /// 尝试按 ID 获取会话
        /// <para>Try to get a session by id</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <param name="session">取到的会话；不存在为 null<para>The session; null when absent</para></param>
        /// <returns>是否存在<para>Whether it exists</para></returns>
        public bool TryGetSession(int sessionId, out NetworkSession session)
        {
            session = GetSession(sessionId);
            return session != null;
        }

        /// <summary>
        /// 关闭指定连接（终态，不再重连）
        /// <para>Close a connection by id (terminal; no more reconnects)</para>
        /// </summary>
        /// <param name="sessionId">会话ID<para>Session id</para></param>
        /// <param name="reason">原因简述<para>Reason</para></param>
        public void CloseSession(int sessionId, string reason = "Closed by user")
        {
            if (_connectionMap.TryGetValue(sessionId, out var connection))
            {
                connection.Close(reason);
                RemoveConnection(connection);
                EventManager.Instance?.Publish(new NetDisconnectedEvent(sessionId, reason));
            }
        }

        /// <summary>
        /// 关闭所有连接
        /// <para>Close all connections</para>
        /// </summary>
        /// <param name="reason">原因简述<para>Reason</para></param>
        public void CloseAll(string reason)
        {
            for (int i = _connections.Count - 1; i >= 0; i--)
            {
                var connection = _connections[i];
                connection.Close(reason);
                RemoveConnection(connection);
                EventManager.Instance?.Publish(new NetDisconnectedEvent(connection.SessionId, reason));
            }
        }

        /// <summary>
        /// 更新握手 Token（短时效 Token 过期时调用，下次重连生效）
        /// <para>Update the handshake token (call when a short-lived token expires; applies to the next reconnect)</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <param name="token">新 Token（null/空 = 之后重连将跳过握手）<para>New token (null/empty makes the next reconnect skip the handshake)</para></param>
        /// <returns>是否找到对应连接<para>Whether the connection was found</para></returns>
        public bool SetToken(int sessionId, byte[] token)
        {
            if (!_connectionMap.TryGetValue(sessionId, out var connection)) return false;
            connection.Token = token;
            return true;
        }

        /// <summary>
        /// 发送单向消息到指定会话（自动走当前会话，重连后无需重新获取引用）。
        /// <para>Send a one-way message to a session (uses the current session; survives reconnects).</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="payload">载荷字节<para>Payload bytes</para></param>
        /// <returns>是否发送成功<para>Whether it was sent</para></returns>
        public bool Send(int sessionId, int opcode, byte[] payload = null)
        {
            var session = GetSession(sessionId);
            if (session == null) return false;
            session.Send(opcode, payload);
            return true;
        }

        /// <summary>
        /// 发起 RPC 请求并等待响应（自动走当前会话）。
        /// <para>Send an RPC request and await the response (uses the current session).</para>
        /// </summary>
        /// <typeparam name="TResponse">响应消息类型<para>Response message type</para></typeparam>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="payload">载荷字节<para>Payload bytes</para></param>
        /// <param name="timeoutSeconds">超时秒数<para>Timeout in seconds</para></param>
        /// <returns>响应消息<para>Response message</para></returns>
        public UniTask<TResponse> Call<TResponse>(int sessionId, int opcode, byte[] payload, float timeoutSeconds = 10f)
        {
            var session = GetSession(sessionId);
            if (session == null)
                return UniTask.FromException<TResponse>(new NetProtocolException($"Session {sessionId} does not exist."));
            return session.Call<TResponse>(opcode, payload, timeoutSeconds);
        }

        /// <summary>
        /// 注册原始消息处理器（对所有会话生效）。
        /// <para>Register a raw handler (applies to all sessions).</para>
        /// </summary>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="handler">处理回调<para>Handle callback</para></param>
        public void RegisterHandler(int opcode, Action<NetPacket> handler)
        {
            _dispatcher.RegisterHandler(opcode, handler);
        }

        /// <summary>
        /// 注册原始消息处理器（仅指定会话生效，优先于同 opcode 的全局处理器）。
        /// <para>Register a raw handler for one session only (takes precedence over the global one).</para>
        /// </summary>
        /// <param name="sessionId">目标会话 Id<para>Target session id</para></param>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="handler">处理回调<para>Handle callback</para></param>
        public void RegisterHandler(int sessionId, int opcode, Action<NetPacket> handler)
        {
            _dispatcher.RegisterHandler(sessionId, opcode, handler);
        }

        /// <summary>
        /// 注册原始消息处理器（实现 <see cref="IMessageHandler"/> 的对象，对所有会话生效）。
        /// <para>Register a raw handler object implementing IMessageHandler (applies to all sessions).</para>
        /// </summary>
        /// <param name="handler">处理器对象<para>Handler object</para></param>
        public void RegisterHandler(IMessageHandler handler)
        {
            _dispatcher.RegisterHandler(handler);
        }

        /// <summary>
        /// 注册原始消息处理器（实现 <see cref="IMessageHandler"/> 的对象，仅指定会话生效）。
        /// <para>Register a raw handler object implementing IMessageHandler for one session only.</para>
        /// </summary>
        /// <param name="sessionId">目标会话 Id<para>Target session id</para></param>
        /// <param name="handler">处理器对象<para>Handler object</para></param>
        public void RegisterHandler(int sessionId, IMessageHandler handler)
        {
            _dispatcher.RegisterHandler(sessionId, handler);
        }

        /// <summary>
        /// 注册类型化消息处理器（便捷转发到 <see cref="Dispatcher"/>；对所有会话生效）。
        /// <para>Register a typed handler (forwards to Dispatcher; applies to all sessions).</para>
        /// </summary>
        /// <typeparam name="TMessage">消息类型<para>Message type</para></typeparam>
        /// <typeparam name="THandler">处理器类型<para>Handler type</para></typeparam>
        /// <param name="handler">处理器实例；传 null 时按无参构造创建<para>Handler instance; created via parameterless ctor when null</para></param>
        public void RegisterHandler<TMessage, THandler>(THandler handler = null)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>, new()
        {
            _dispatcher.RegisterHandler<TMessage, THandler>(handler);
        }

        /// <summary>
        /// 注册类型化消息处理器（仅指定会话生效）。
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
            _dispatcher.RegisterHandler<TMessage, THandler>(sessionId, handler);
        }

        /// <summary>
        /// 注销原始消息处理器（所有会话）。
        /// <para>Unregister a raw handler (all sessions).</para>
        /// </summary>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <returns>是否注销成功<para>Whether unregistered</para></returns>
        public bool UnregisterHandler(int opcode)
        {
            return _dispatcher.UnregisterHandler(opcode);
        }

        /// <summary>
        /// 注销原始消息处理器（指定会话）。
        /// <para>Unregister a raw handler for one session.</para>
        /// </summary>
        /// <param name="sessionId">目标会话 Id<para>Target session id</para></param>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <returns>是否注销成功<para>Whether unregistered</para></returns>
        public bool UnregisterHandler(int sessionId, int opcode)
        {
            return _dispatcher.UnregisterHandler(sessionId, opcode);
        }

        // 分配一个全局唯一的 conv
        private uint AllocateConv() => _nextConv++;

        // 订阅连接事件并转发为框架事件
        private void HookConnection(NetConnection connection)
        {
            int sessionId = connection.SessionId;

            connection.NetConnected += conn =>
            {
                EventManager.Instance?.Publish(new NetConnectedEvent(sessionId, conn.RemoteEndPoint));
            };

            connection.NetDisconnected += (conn, reason) =>
            {
                EventManager.Instance?.Publish(new NetDisconnectedEvent(sessionId, reason));
            };

            connection.NetReconnectScheduled += (conn, attempt, delay, reason) =>
            {
                EventManager.Instance?.Publish(new NetReconnectingEvent(sessionId, attempt, delay, reason));
            };

            connection.NetHandshakeResult += (conn, success, code, message) =>
            {
                EventManager.Instance?.Publish(new NetHandshakeResultEvent(sessionId, success, code, message));
            };

            connection.StateChanged += (conn, oldState, newState) =>
            {
                EventManager.Instance?.Publish(new NetStateChangedEvent(sessionId, oldState, newState));
            };
        }

        #endregion
    }
}
