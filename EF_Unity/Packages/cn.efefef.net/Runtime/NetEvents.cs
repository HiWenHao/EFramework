/*
 * ================================================
 * Describe:      网络模块生命周期事件（通过 EventManager 发布）
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
        #region Properties

        /// <summary>
        /// 会话 ID
        /// <para>Session id</para>
        /// </summary>
        public int SessionId { get; }

        /// <summary>
        /// 远端地址（ip:port）
        /// <para>Remote endpoint (ip:port)</para>
        /// </summary>
        public string RemoteEndPoint { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 构造连接成功事件。
        /// <para>Create a connected event.</para>
        /// </summary>
        public NetConnectedEvent(int sessionId, string remoteEndPoint)
        {
            SessionId = sessionId;
            RemoteEndPoint = remoteEndPoint;
        }

        #endregion
    }

    /// <summary>
    /// 会话断开事件（主动关闭、远端断开或链路死亡时发布）。
    /// <para>Published when a session is closed, dropped by the remote, or the link is dead.</para>
    /// </summary>
    public readonly struct NetDisconnectedEvent
    {
        #region Properties

        /// <summary>
        /// 会话 ID
        /// <para>Session id</para>
        /// </summary>
        public int SessionId { get; }

        /// <summary>
        /// 断开原因
        /// <para>Disconnect reason</para>
        /// </summary>
        public string Reason { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 构造断开事件。
        /// <para>Create a disconnected event.</para>
        /// </summary>
        public NetDisconnectedEvent(int sessionId, string reason)
        {
            SessionId = sessionId;
            Reason = reason;
        }

        #endregion
    }

    /// <summary>
    /// 网络错误事件（编解码失败、RPC 超时等可恢复错误时发布）。
    /// <para>Published on recoverable errors such as codec failure or RPC timeout.</para>
    /// </summary>
    public readonly struct NetErrorEvent
    {
        #region Properties

        /// <summary>
        /// 会话 ID（0 表示与具体会话无关）
        /// <para>Session id (0 means not session-specific)</para>
        /// </summary>
        public int SessionId { get; }

        /// <summary>
        /// 错误描述
        /// <para>Error description</para>
        /// </summary>
        public string Message { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 构造网络错误事件。
        /// <para>Create a network error event.</para>
        /// </summary>
        public NetErrorEvent(int sessionId, string message)
        {
            SessionId = sessionId;
            Message = message;
        }

        #endregion
    }
}