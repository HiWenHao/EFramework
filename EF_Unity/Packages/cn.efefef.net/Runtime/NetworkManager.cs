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
using EasyFramework;
using EasyFramework.Managers;
using EasyFramework.Managers.Event;
using UnityEngine;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 网络管理器：经 EFC 注册，统一驱动所有会话的收发，断线/错误经 EventManager 广播。
    /// <para>Network manager: registered with EFC, drives all sessions' send/receive,
    /// broadcasts disconnects/errors through EventManager.</para>
    /// </summary>
    /// <remarks>
    /// Order 约定：10000 以上保留给框架自带管理器（Time 89900 / Event 99800 等），外部包用默认 0。
    /// <para>Order convention: values above 10000 are reserved for built-in framework managers;
    /// external packages use the default 0.</para>
    /// </remarks>
    [Manager]
    [Dependency(typeof(EventManager))]
    public sealed class NetworkManager : MonoSingleton<NetworkManager>, ISingleton, IUpdate
    {
        private readonly List<NetworkSession> _sessions = new List<NetworkSession>();
        private readonly Dictionary<int, NetworkSession> _sessionMap = new Dictionary<int, NetworkSession>();
        private readonly MessageDispatcher _dispatcher = new MessageDispatcher();
        private int _nextSessionId = 1;
        private uint _nextConv = 1;

        /// <summary> 全局消息分发器（业务在此注册 Handler / 设置序列化器） <para>Global dispatcher (register handlers / serializer here)</para> </summary>
        public MessageDispatcher Dispatcher => _dispatcher;

        /// <summary> 序列化器（透传给分发器） <para>Serializer (forwarded to the dispatcher)</para> </summary>
        public IMessageSerializer Serializer
        {
            get => _dispatcher.Serializer;
            set => _dispatcher.Serializer = value;
        }

        /// <summary> 是否暂停轮询 <para>Whether paused</para> </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// 初始化（EFC 注册时调用）。
        /// <para>Initialize (called on EFC registration).</para>
        /// </summary>
        public void Init()
        {
            IsPaused = false;
            _dispatcher.RpcTimedOut -= OnRpcTimedOut;
            _dispatcher.RpcTimedOut += OnRpcTimedOut;
        }

        /// <summary>
        /// 退出（EFC 注销时调用）：关闭所有会话。
        /// <para>Quit (called on EFC unregister): close all sessions.</para>
        /// </summary>
        public void Quit()
        {
            _dispatcher.RpcTimedOut -= OnRpcTimedOut;
            CloseAll("Manager quit");
        }

        /// <summary>
        /// 每帧驱动：推进所有会话 + 检查 RPC 超时。
        /// <para>Per-frame drive: advance all sessions and check RPC timeouts.</para>
        /// </summary>
        public void Update(float elapse, float realElapse)
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

        /// <summary>
        /// 创建并连接一个会话（UDP + KCP）。
        /// <para>Create and connect a session (UDP + KCP).</para>
        /// </summary>
        /// <param name="host">服务端地址 <para>Server host</param>
        /// <param name="port">服务端端口 <para>Server port</param>
        /// <param name="settings">调参数（null 用默认） <para>Tuning parameters (null for defaults)</param>
        /// <param name="socketFactory">自定义传输层工厂（null 用 <see cref="UdpSocket"/>；微信平台在此注入 WX 实现） <para>Custom transport factory (null uses UdpSocket; inject the WX implementation here on WeChat)</param>
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

        /// <summary> 按 Id 获取会话 <para>Get a session by id</para> </summary>
        public NetworkSession GetSession(int sessionId)
        {
            return _sessionMap.TryGetValue(sessionId, out var session) ? session : null;
        }

        /// <summary> 关闭指定会话 <para>Close a session by id</para> </summary>
        public void CloseSession(int sessionId, string reason = "Closed by user")
        {
            if (_sessionMap.TryGetValue(sessionId, out var session))
                RemoveSession(session, reason);
        }

        /// <summary> 关闭所有会话 <para>Close all sessions</para> </summary>
        public void CloseAll(string reason)
        {
            for (int i = _sessions.Count - 1; i >= 0; i--)
                RemoveSession(_sessions[i], reason);
        }

        /// <summary>
        /// 注册原始消息处理器（便捷转发到 <see cref="Dispatcher"/>）。
        /// <para>Register a raw handler (forwards to Dispatcher).</para>
        /// </summary>
        public void RegisterHandler(int opcode, Action<NetPacket> handler)
        {
            _dispatcher.RegisterHandler(opcode, handler);
        }

        /// <summary>
        /// 注册类型化消息处理器（便捷转发到 <see cref="Dispatcher"/>）。
        /// <para>Register a typed handler (forwards to Dispatcher).</para>
        /// </summary>
        public void RegisterHandler<TMessage, THandler>(THandler handler = null)
            where TMessage : class
            where THandler : class, IMessageHandler<TMessage>, new()
        {
            _dispatcher.RegisterHandler<TMessage, THandler>(handler);
        }

        private void RemoveSession(NetworkSession session, string reason)
        {
            if (!_sessionMap.Remove(session.SessionId)) return;
            _sessions.Remove(session);
            session.Close();
            EventManager.Instance?.Publish(new NetDisconnectedEvent(session.SessionId, reason));
        }

        private void OnRpcTimedOut(int rpcId)
        {
            EventManager.Instance?.Publish(new NetErrorEvent(0, $"RPC timeout (rpcId={rpcId})."));
        }
    }
}
