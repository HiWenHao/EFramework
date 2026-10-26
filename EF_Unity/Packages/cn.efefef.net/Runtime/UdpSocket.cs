/*
 * ================================================
 * Describe:      UdpClient based ISocket implementation (desktop / mobile).
 * Author:        Alvin5100
 * CreationTime:  2026-10-23 18:45:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-23 18:45:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;
using System.Net;
using System.Net.Sockets;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 基于 <see cref="UdpClient"/> 的传输层实现（桌面 / 移动平台）。
    /// <para>UdpClient-based transport (desktop / mobile).</para>
    /// </summary>
    /// <remarks>
    /// 注意：微信小游戏平台禁用了 System.Net.Sockets，届时改为 WX.CreateUDPSocket 实现 <see cref="ISocket"/>。
    /// <para>Note: System.Net.Sockets is unavailable on WeChat mini-games; implement ISocket
    /// over WX.CreateUDPSocket there.</para>
    /// </remarks>
    public sealed class UdpSocket : ISocket
    {
        #region Fields

        // UDP 套接字（已连接时只与远端通信）
        private UdpClient _client;

        // 远端地址
        private IPEndPoint _remote;

        #endregion

        #region Properties

        /// <summary>
        /// 是否已连接
        /// <para>Whether connected</para>
        /// </summary>
        public bool IsConnected => _client != null;

        /// <summary>
        /// 远端地址（ip:port）
        /// <para>Remote endpoint (ip:port)</para>
        /// </summary>
        public string RemoteEndPoint { get; private set; }

        /// <summary>
        /// 最近一次 IO 错误信息（诊断用）
        /// <para>Latest IO error message (diagnostics)</para>
        /// </summary>
        public string LastError { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// 连接远端（DNS 解析 + UDP 通道就绪）。
        /// <para>Connect to the remote (DNS resolve + UDP channel ready).</para>
        /// </summary>
        /// <param name="host">远端地址<para>Remote host</para></param>
        /// <param name="port">远端端口<para>Remote port</para></param>
        /// <returns>是否成功<para>Whether succeeded</para></returns>
        public bool Connect(string host, int port)
        {
            Close();

            try
            {
                _remote = new IPEndPoint(Dns.GetHostAddresses(host)[0], port);
                _client = new UdpClient();
                _client.Connect(_remote);
                RemoteEndPoint = _remote.ToString();
                return true;
            }
            catch (Exception)
            {
                Close();
                return false;
            }
        }

        /// <summary>
        /// 发送数据
        /// <para>Send data</para>
        /// </summary>
        /// <param name="data">数据缓冲<para>Data buffer</para></param>
        /// <param name="offset">起始偏移<para>Start offset</para></param>
        /// <param name="count">发送字节数<para>Byte count to send</para></param>
        /// <returns>实际发送字节数；失败返回 -1<para>Bytes sent; -1 on failure</para></returns>
        public int Send(byte[] data, int offset, int count)
        {
            if (_client == null) return -1;
            try
            {
                // 已连接的 UdpClient 必须用二参重载（Send(data, bytes)）：
                // 三参重载 Send(data, bytes, endpoint) 在已连接套接字上会抛异常（此前被静默吞掉 → 包发不出去）
                if (offset == 0) return _client.Send(data, count);

                var copy = new byte[count];
                Buffer.BlockCopy(data, offset, copy, 0, count);
                return _client.Send(copy, count);
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return -1;
            }
        }

        /// <summary>
        /// 非阻塞接收
        /// <para>Non-blocking receive</para>
        /// </summary>
        /// <param name="buffer">接收缓冲<para>Receive buffer</para></param>
        /// <param name="offset">写入起始偏移<para>Write start offset</para></param>
        /// <param name="capacity">接收缓冲剩余容量<para>Remaining buffer capacity</para></param>
        /// <returns>收到的字节数；无数据返回 0；出错返回 -1<para>Bytes received; 0 when nothing available; -1 on error</para></returns>
        public int TryReceive(byte[] buffer, int offset, int capacity)
        {
            if (_client == null) return -1;
            try
            {
                if (_client.Available <= 0) return 0;

                // 已连接：只会收到对端的数据
                IPEndPoint from = null;
                byte[] received = _client.Receive(ref from);
                if (received.Length == 0) return 0;
                if (received.Length > capacity)
                {
                    // 缓冲区放不下：截断并返回负值提示调用方（KCP 段不应超过 mss+24，正常不会触发）
                    Buffer.BlockCopy(received, 0, buffer, offset, capacity);
                    LastError = $"Incoming datagram {received.Length}B exceeds buffer {capacity}B";
                    return -1;
                }

                Buffer.BlockCopy(received, 0, buffer, offset, received.Length);
                return received.Length;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                return -1;
            }
        }

        /// <summary>
        /// 关闭通道
        /// <para>Close the channel</para>
        /// </summary>
        public void Close()
        {
            _client?.Close();
            _client = null;
            _remote = null;
            RemoteEndPoint = null;
        }

        #endregion
    }
}