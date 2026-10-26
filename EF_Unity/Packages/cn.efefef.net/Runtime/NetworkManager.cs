/*
 * ================================================
 * Describe:      Net module manager (EFC-driven, drives sessions and publishes events).
 * Author:        Alvin5100
 * CreationTime:  2026-10-23 18:45:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-23 18:45:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Collections.Generic;
using EasyFramework.Managers;
using EasyFramework.Managers.Event;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 网络管理器: 统一驱动所有会话的收发，断线/错误经 [ EventManager ] 广播。
    /// <para>Network manager: drives all sessions' send/receive,
    /// broadcasts disconnects/errors through [ EventManager ].</para>
    /// </summary>
    /// <remarks>
    /// 一般只有一条会话（连一个游戏服）；多会话场景（如同时连登录服与战斗服）下，
    /// 可用 <see cref="RegisterHandler(int, int, Action{NetPacket})"/> 按会话注册处理器，
    /// 或用 <see cref="GetSession"/> 按 Id 取会话分别收发。
    /// <para>Usually a single session (one game server). With several sessions (e.g. a login server
    /// and a battle server at once), register session-scoped handlers via
    /// RegisterHandler(int, int, Action{NetPacket}) or fetch sessions by id via GetSession.</para>
    /// </remarks>
    [Manager]
    [Dependency(typeof(EventManager))]
    public sealed class NetworkManager : MonoSingleton<NetworkManager>, ISingleton, IUpdate
    {
        #region Fields

        // 下一个 KCP 会话号（conv，双方必须一致）
        private uint _nextConv = 1;

        // 下一个会话 Id
        private int _nextSessionId = 1;

        // 活跃会话
        private readonly List<NetworkSession> _sessions = new List<NetworkSession>();

        // sessionId → 会话
        private readonly Dictionary<int, NetworkSession> _sessionMap = new Dictionary<int, NetworkSession>();

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
        /// 是否暂停驱动（暂停期间不收发、不检查超时）
        /// <para>Whether the driving is paused (no send/receive, no timeout check)</para>
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 活跃会话数量
        /// <para>Number of active sessions</para>
        /// </summary>
        public int SessionCount => _sessions.Count;

        /// <summary>
        /// 活跃会话列表（只读快照；多会话场景下按 <see cref="NetworkSession.SessionId"/> 区分）
        /// <para>Active session list (read-only snapshot; distinguish by NetworkSession.SessionId
        /// when several sessions exist)</para>
        /// </summary>
        public IReadOnlyList<NetworkSession> Sessions => _sessions;

        #endregion

        #region Methods

        // 初始化（EFC 注册时调用）
        void ISingleton.Init()
        {
            IsPaused = false;
            _dispatcher.RpcTimedOut -= OnRpcTimedOut;
            _dispatcher.RpcTimedOut += OnRpcTimedOut;
        }

        // 每帧驱动：推进所有会话 + 检查 RPC 超时
        void IUpdate.Update(float elapse, float realElapse)
        {
            if (IsPaused) return;

            for (int i = _sessions.Count - 1; i >= 0; i--)
            {
                var session = _sessions[i];
                if (!session.IsConnected)
                {
                    // 会话已被关闭：移除并广播断线
                    RemoveSession(session, "Socket closed");
                    continue;
                }

                session.Update();

                if (session.IsDead)
                {
                    RemoveSession(session, "Link dead");
                }
            }

            _dispatcher.CheckTimeouts(Environment.TickCount);
        }

        // 退出（EFC 注销时调用）：关闭所有会话
        void ISingleton.Quit()
        {
            _dispatcher.RpcTimedOut -= OnRpcTimedOut;
            CloseAll("Network Manager quit");
        }

        // 移除会话
        private void RemoveSession(NetworkSession session, string reason)
        {
            if (!_sessionMap.Remove(session.SessionId)) return;
            _sessions.Remove(session);
            session.Close();

            // 注销该会话的会话级处理器（否则字典随会话数无限增长）
            _dispatcher.UnregisterSessionHandlers(session.SessionId);

            EventManager.Instance?.Publish(new NetDisconnectedEvent(session.SessionId, reason));
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
            ISocket socket = socketFactory != null ? socketFactory() : new UdpSocket();
            if (!socket.Connect(host, port))
            {
                D.Error($"[ NetworkManager ] Connect failed: {host}:{port}");
                socket.Close();
                return null;
            }

            int sessionId = _nextSessionId++;
            uint conv = _nextConv++;
            var session = new NetworkSession(sessionId, conv, socket, _dispatcher, settings);
            _sessions.Add(session);
            _sessionMap[sessionId] = session;

            EventManager.Instance?.Publish(new NetConnectedEvent(sessionId, session.RemoteEndPoint));
            return session;
        }

        /// <summary>
        /// 按 ID 获取会话
        /// <para>Get a session by id</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <returns>会话；不存在返回 null<para>Session; null when absent</para></returns>
        public NetworkSession GetSession(int sessionId) => _sessionMap.GetValueOrDefault(sessionId);

        /// <summary>
        /// 尝试按 ID 获取会话
        /// <para>Try to get a session by id</para>
        /// </summary>
        /// <param name="sessionId">会话 Id<para>Session id</para></param>
        /// <param name="session">取到的会话；不存在为 null<para>The session; null when absent</para></param>
        /// <returns>是否存在<para>Whether it exists</para></returns>
        public bool TryGetSession(int sessionId, out NetworkSession session)
        {
            return _sessionMap.TryGetValue(sessionId, out session);
        }

        /// <summary>
        /// 关闭指定会话
        /// <para>Close a session by id</para>
        /// </summary>
        /// <param name="sessionId">会话ID<para>Session id</para></param>
        /// <param name="reason">原因简述<para>Reason</para></param>
        public void CloseSession(int sessionId, string reason = "Closed by user")
        {
            if (_sessionMap.TryGetValue(sessionId, out var session))
                RemoveSession(session, reason);
        }

        /// <summary>
        /// 关闭所有会话
        /// <para>Close all sessions</para>
        /// </summary>
        /// <param name="reason">原因简述<para>Reason</para></param>
        public void CloseAll(string reason)
        {
            for (int i = _sessions.Count - 1; i >= 0; i--)
                RemoveSession(_sessions[i], reason);
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

        #endregion
    }
}
