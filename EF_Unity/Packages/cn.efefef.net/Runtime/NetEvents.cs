/*
 * ================================================
 * Describe:      Net module lifecycle events (published through EventManager).
 * Author:        Alvin5100
 * CreationTime:  2026-10-23 18:45:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-23 18:45:00
 * ScriptVersion: 0.1
 * ===============================================
 */

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 会话连接成功事件（KCP 通道建立、可以开始收发时发布）。
    /// <para>Published when a session is connected and ready to send/receive.</para>
    /// </summary>
    public readonly struct NetConnectedEvent
    {
        /// <summary> 会话 Id <para>Session id</para> </summary>
        public int SessionId { get; }

        /// <summary> 远端地址（ip:port） <para>Remote endpoint (ip:port)</para> </summary>
        public string RemoteEndPoint { get; }

        public NetConnectedEvent(int sessionId, string remoteEndPoint)
        {
            SessionId = sessionId;
            RemoteEndPoint = remoteEndPoint;
        }
    }

    /// <summary>
    /// 会话断开事件（主动关闭、远端断开或链路死亡时发布）。
    /// <para>Published when a session is closed, dropped by the remote, or the link is dead.</para>
    /// </summary>
    public readonly struct NetDisconnectedEvent
    {
        /// <summary> 会话 Id <para>Session id</para> </summary>
        public int SessionId { get; }

        /// <summary> 断开原因 <para>Disconnect reason</para> </summary>
        public string Reason { get; }

        public NetDisconnectedEvent(int sessionId, string reason)
        {
            SessionId = sessionId;
            Reason = reason;
        }
    }

    /// <summary>
    /// 网络错误事件（编解码失败、RPC 超时等可恢复错误时发布）。
    /// <para>Published on recoverable errors such as codec failure or RPC timeout.</para>
    /// </summary>
    public readonly struct NetErrorEvent
    {
        /// <summary> 会话 Id（0 表示与具体会话无关） <para>Session id (0 means not session-specific)</para> </summary>
        public int SessionId { get; }

        /// <summary> 错误描述 <para>Error description</para> </summary>
        public string Message { get; }

        public NetErrorEvent(int sessionId, string message)
        {
            SessionId = sessionId;
            Message = message;
        }
    }
}
