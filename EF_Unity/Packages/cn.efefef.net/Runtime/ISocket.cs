/*
 * ================================================
 * Describe:      Net module transport abstraction (socket layer).
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
    /// 传输层抽象：屏蔽具体 UDP 实现，便于平台替换。
    /// <para>Transport abstraction hiding the concrete UDP implementation for platform swapping.</para>
    /// </summary>
    /// <remarks>
    /// 平台替换点 / Platform swap points:
    /// <para>· 桌面/移动：<see cref="UdpSocket"/>（System.Net.Sockets.UdpClient）。</para>
    /// <para>· 微信小游戏：自行实现本接口，内部走 WX.CreateUDPSocket（禁 System.Net.Sockets）。</para>
    /// <para>· WebGL：同样经 WX UDP 或 WebSocket；KCP 收发由主线程驱动。</para>
    /// </remarks>
    public interface ISocket
    {
        #region Properties

        /// <summary>
        /// 是否已连接（已绑定远端）
        /// <para>Whether connected (remote bound)</para>
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 远端地址描述（ip:port），未连接时为空
        /// <para>Remote endpoint (ip:port); empty when not connected</para>
        /// </summary>
        string RemoteEndPoint { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 绑定远端并建立 UDP 通道。
        /// <para>Bind the remote endpoint and open the UDP channel.</para>
        /// </summary>
        /// <param name="host">远端地址<para>Remote host</para></param>
        /// <param name="port">远端端口<para>Remote port</para></param>
        /// <returns>是否成功<para>Whether succeeded</para></returns>
        bool Connect(string host, int port);

        /// <summary>
        /// 发送数据。
        /// <para>Send data.</para>
        /// </summary>
        /// <param name="data">数据缓冲<para>Data buffer</para></param>
        /// <param name="offset">起始偏移<para>Start offset</para></param>
        /// <param name="count">发送字节数<para>Byte count to send</para></param>
        /// <returns>实际发送字节数；失败返回 -1<para>Bytes sent; -1 on failure</para></returns>
        int Send(byte[] data, int offset, int count);

        /// <summary>
        /// 尝试接收一包数据（非阻塞）。
        /// <para>Try to receive one packet (non-blocking).</para>
        /// </summary>
        /// <param name="buffer">接收缓冲<para>Receive buffer</para></param>
        /// <param name="offset">写入起始偏移<para>Write start offset</para></param>
        /// <param name="capacity">接收缓冲剩余容量<para>Remaining buffer capacity</para></param>
        /// <returns>收到的字节数；无数据返回 0；出错返回 -1<para>Bytes received; 0 when nothing available; -1 on error</para></returns>
        int TryReceive(byte[] buffer, int offset, int capacity);

        /// <summary>
        /// 关闭通道
        /// <para>Close the channel</para>
        /// </summary>
        void Close();

        #endregion
    }
}