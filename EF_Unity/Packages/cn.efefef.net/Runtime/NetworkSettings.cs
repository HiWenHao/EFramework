/*
 * ================================================
 * Describe:      Net module tuning settings (KCP parameters + lifecycle policy).
 * Author:        Alvin5100
 * CreationTime:  2026-10-23 18:45:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-26 17:40:00
 * ScriptVersion: 0.2
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 网络调参数（KCP 经验值默认，弱网可整体换一套）+ 连接生命周期策略。
    /// <para>Network tuning (KCP battle-tested defaults; swap wholesale for weak networks)
    /// plus the connection lifecycle policy.</para>
    /// </summary>
    /// <remarks>
    /// 保持纯 C#（不依赖 UnityEngine），以便在 .NET 侧独立做单元测试。
    /// <para>Stays pure C# (no UnityEngine dependency) so it can be unit-tested on the .NET side.</para>
    /// </remarks>
    public sealed class NetworkSettings
    {
        #region Fields

        // 退避抖动用的随机源（纯 C#，避免依赖 UnityEngine.Random）
        private static readonly Random JitterRandom = new Random();

        #endregion

        #region Fields

        /// <summary>
        /// 启用 nodelay（1=启用）
        /// <para>Enable nodelay (1 = on)</para>
        /// </summary>
        public bool NoDelay = true;

        /// <summary>
        /// 内部更新间隔(ms)，战斗同步建议 10
        /// <para>Internal interval in ms; 10 for battle sync</para>
        /// </summary>
        public int Interval = 10;

        /// <summary>
        /// 触发快速重传的冗余 ACK 次数（0=关闭快重传）
        /// <para>Duplicate acks to trigger fast retransmit (0 = off)</para>
        /// </summary>
        public int FastResend = 2;

        /// <summary>
        /// 关闭拥塞控制（1=关闭，追最低延迟）
        /// <para>Disable congestion control (1 = off, chase lowest latency)</para>
        /// </summary>
        public bool NoCwnd = true;

        /// <summary>
        /// 发送窗口(段数)
        /// <para>Send window in segments</para>
        /// </summary>
        public int SndWnd = 256;

        /// <summary>
        /// 接收窗口(段数)
        /// <para>Receive window in segments</para>
        /// </summary>
        public int RcvWnd = 256;

        /// <summary>
        /// MTU（移动弱网 470~512，避免 IP 分片）
        /// <para>MTU (470~512 on mobile weak networks to avoid IP fragmentation)</para>
        /// </summary>
        public int Mtu = 512;

        /// <summary>
        /// 重传次数超过该值判定链路死亡
        /// <para>Link considered dead after this many retransmits</para>
        /// </summary>
        public int DeadLink = 20;

        /// <summary>
        /// 心跳间隔（秒，&lt;=0 关闭心跳）
        /// <para>Heartbeat interval in seconds (&lt;=0 disables the heartbeat)</para>
        /// </summary>
        public float HeartbeatInterval = 5f;

        /// <summary>
        /// 心跳超时（秒，超过此时长未收到任何响应即判定链路死亡）
        /// <para>Heartbeat timeout in seconds (no response within it means the link is dead)</para>
        /// </summary>
        public float HeartbeatTimeout = 15f;

        /// <summary>
        /// 断线后是否自动重连
        /// <para>Whether to reconnect automatically after a drop</para>
        /// </summary>
        public bool AutoReconnect = true;

        /// <summary>
        /// 重连初始退避（秒）
        /// <para>Initial reconnect backoff in seconds</para>
        /// </summary>
        public float ReconnectInitialDelay = 1f;

        /// <summary>
        /// 重连最大退避（秒）
        /// <para>Maximum reconnect backoff in seconds</para>
        /// </summary>
        public float ReconnectMaxDelay = 30f;

        /// <summary>
        /// 重连退避倍率（每次失败后 delay *= 该值）
        /// <para>Backoff multiplier (delay *= this after each failure)</para>
        /// </summary>
        public float ReconnectBackoffFactor = 2f;

        /// <summary>
        /// 重连退避随机抖动（0~1，避免大量客户端同时重连造成惊群）
        /// <para>Reconnect backoff jitter (0~1; avoids a thundering herd of reconnects)</para>
        /// </summary>
        public float ReconnectJitter = 0.2f;

        /// <summary>
        /// 握手超时（秒，超时判定握手失败）
        /// <para>Handshake timeout in seconds (failure when exceeded)</para>
        /// </summary>
        public float HandshakeTimeout = 10f;

        /// <summary>
        /// 握手失败后是否继续重连（false = 握手失败即放弃，交由业务处理）
        /// <para>Whether to keep retrying after a handshake failure (false = give up and let business handle it)</para>
        /// </summary>
        public bool HandshakeFailureRetries = true;

        #endregion

        #region Properties

        /// <summary>
        /// 默认参数实例
        /// <para>Default parameter instance</para>
        /// </summary>
        public static NetworkSettings Default { get; } = new NetworkSettings();

        /// <summary>
        /// 是否启用心跳保活
        /// <para>Whether the heartbeat keep-alive is enabled</para>
        /// </summary>
        public bool HeartbeatEnabled => HeartbeatInterval > 0f && HeartbeatTimeout > 0f;

        #endregion

        #region Methods

        /// <summary>
        /// 应用参数到 KCP 实例。
        /// <para>Apply parameters to the KCP instance.</para>
        /// </summary>
        /// <param name="kcp">目标 KCP 实例<para>Target KCP instance</para></param>
        public void ApplyTo(Kcp kcp)
        {
            kcp.NoDelay(NoDelay ? 1 : 0, Interval, FastResend, NoCwnd ? 1 : 0);
            kcp.WndSize(SndWnd, RcvWnd);
            kcp.SetMtu(Mtu);
            kcp.SetDeadLink(DeadLink);
        }

        /// <summary>
        /// 计算第 attempt 次重连的退避时长（秒，含随机抖动）。
        /// <para>Compute the backoff delay (seconds, jitter included) for a reconnect attempt.</para>
        /// </summary>
        /// <param name="attempt">已重试次数（从 1 开始）<para>Attempts already made (starting at 1)</para></param>
        /// <returns>本次重连前应等待的秒数<para>Seconds to wait before this attempt</para></returns>
        public float GetReconnectDelay(int attempt)
        {
            float delay = ReconnectInitialDelay;
            for (int i = 1; i < attempt; i++)
            {
                delay *= ReconnectBackoffFactor;
                if (delay >= ReconnectMaxDelay) break;
            }

            if (delay > ReconnectMaxDelay) delay = ReconnectMaxDelay;

            // 抖动：delay *= (1 ± jitter)，避免惊群
            if (ReconnectJitter > 0f)
            {
                float offset = ((float)JitterRandom.NextDouble() * 2f - 1f) * ReconnectJitter;
                delay *= 1f + offset;
                if (delay < 0.1f) delay = 0.1f;
            }

            return delay;
        }

        #endregion
    }
}
