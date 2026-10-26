/*
 * ================================================
 * Describe:      Net module tuning settings (KCP parameters).
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
    /// 网络调参数（KCP 经验值默认，弱网可整体换一套）。
    /// <para>Network tuning parameters (KCP battle-tested defaults; swap wholesale for weak networks).</para>
    /// </summary>
    public sealed class NetworkSettings
    {
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

        #endregion

        #region Properties

        /// <summary>
        /// 默认参数实例
        /// <para>Default parameter instance</para>
        /// </summary>
        public static NetworkSettings Default { get; } = new NetworkSettings();

        #endregion

        #region Methods

        /// <summary>
        /// 应用参数到 KCP 实例。
        /// <para>Apply parameters to a KCP instance.</para>
        /// </summary>
        /// <param name="kcp">目标 KCP 实例<para>Target KCP instance</para></param>
        public void ApplyTo(Kcp kcp)
        {
            kcp.NoDelay(NoDelay ? 1 : 0, Interval, FastResend, NoCwnd ? 1 : 0);
            kcp.WndSize(SndWnd, RcvWnd);
            kcp.SetMtu(Mtu);
            kcp.SetDeadLink(DeadLink);
        }

        #endregion
    }
}