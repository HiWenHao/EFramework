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
    /// 握手结果事件（Token 校验完成时发布）。
    /// <para>Published when the token handshake completes.</para>
    /// </summary>
    public readonly struct NetHandshakeResultEvent
    {
        #region Properties

        /// <summary>
        /// 会话 ID
        /// <para>Session id</para>
        /// </summary>
        public int SessionId { get; }

        /// <summary>
        /// 是否成功
        /// <para>Whether the handshake succeeded</para>
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// 结果码（0 = 成功，非 0 为服务端定义的原因码）
        /// <para>Result code (0 = success, non-zero is a server-defined reason code)</para>
        /// </summary>
        public int ResultCode { get; }

        /// <summary>
        /// 失败原因（成功时为空）
        /// <para>Failure reason (empty on success)</para>
        /// </summary>
        public string Message { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 构造握手结果事件。
        /// <para>Create a handshake result event.</para>
        /// </summary>
        /// <param name="sessionId">会话 ID<para>Session id</para></param>
        /// <param name="success">是否成功<para>Whether it succeeded</para></param>
        /// <param name="resultCode">结果码<para>Result code</para></param>
        /// <param name="message">失败原因<para>Failure reason</para></param>
        public NetHandshakeResultEvent(int sessionId, bool success, int resultCode, string message)
        {
            SessionId = sessionId;
            Success = success;
            ResultCode = resultCode;
            Message = message;
        }

        #endregion
    }

    /// <summary>
    /// 重连排队事件（断线后按退避策略等待重连时发布）。
    /// <para>Published when a reconnect is queued after a drop (backoff pending).</para>
    /// </summary>
    public readonly struct NetReconnectingEvent
    {
        #region Properties

        /// <summary>
        /// 会话 ID
        /// <para>Session id</para>
        /// </summary>
        public int SessionId { get; }

        /// <summary>
        /// 第几次重试（从 1 开始）
        /// <para>Which attempt this is (starting at 1)</para>
        /// </summary>
        public int Attempt { get; }

        /// <summary>
        /// 本次重连前的退避时长（秒）
        /// <para>Backoff delay before this attempt, in seconds</para>
        /// </summary>
        public float Delay { get; }

        /// <summary>
        /// 断开原因
        /// <para>Reason for the drop</para>
        /// </summary>
        public string Reason { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 构造重连排队事件。
        /// <para>Create a reconnect-scheduled event.</para>
        /// </summary>
        /// <param name="sessionId">会话 ID<para>Session id</para></param>
        /// <param name="attempt">第几次重试<para>Which attempt</para></param>
        /// <param name="delay">退避时长（秒）<para>Backoff delay in seconds</para></param>
        /// <param name="reason">断开原因<para>Reason for the drop</para></param>
        public NetReconnectingEvent(int sessionId, int attempt, float delay, string reason)
        {
            SessionId = sessionId;
            Attempt = attempt;
            Delay = delay;
            Reason = reason;
        }

        #endregion
    }

    /// <summary>
    /// 连接状态迁移事件。
    /// <para>Connection state transition event.</para>
    /// </summary>
    public readonly struct NetStateChangedEvent
    {
        #region Properties

        /// <summary>
        /// 会话 ID
        /// <para>Session id</para>
        /// </summary>
        public int SessionId { get; }

        /// <summary>
        /// 旧状态
        /// <para>Previous state</para>
        /// </summary>
        public NetConnectionState OldState { get; }

        /// <summary>
        /// 新状态
        /// <para>New state</para>
        /// </summary>
        public NetConnectionState NewState { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 构造状态迁移事件。
        /// <para>Create a state transition event.</para>
        /// </summary>
        /// <param name="sessionId">会话 ID<para>Session id</para></param>
        /// <param name="oldState">旧状态<para>Previous state</para></param>
        /// <param name="newState">新状态<para>New state</para></param>
        public NetStateChangedEvent(int sessionId, NetConnectionState oldState, NetConnectionState newState)
        {
            SessionId = sessionId;
            OldState = oldState;
            NewState = newState;
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