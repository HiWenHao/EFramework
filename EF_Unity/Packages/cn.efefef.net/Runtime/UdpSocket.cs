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
        private UdpClient _client;
        private IPEndPoint _remote;

        public bool IsConnected => _client != null;

        public string RemoteEndPoint { get; private set; }

        /// <summary>
        /// 连接远端（DNS 解析 + UDP 通道就绪）。
        /// <para>Connect to the remote (DNS resolve + UDP channel ready).</para>
        /// </summary>
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

        /// <summary> 发送数据 <para>Send data</para> </summary>
        public int Send(byte[] data, int offset, int count)
        {
            if (_client == null) return -1;
            try
            {
                return _client.Send(data, count, _remote);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary> 非阻塞接收 <para>Non-blocking receive</para> </summary>
        public int TryReceive(byte[] buffer, int offset, int capacity)
        {
            if (_client == null) return -1;
            try
            {
                if (_client.Available <= 0) return 0;
                // 已 Connect：直接从默认远端收，无需再解析来源
                IPEndPoint from = null;
                byte[] received = _client.Receive(ref from);
                if (received == null || received.Length == 0) return 0;
                if (received.Length > capacity)
                {
                    // 缓冲区放不下：截断并返回负值提示调用方（KCP 段不应超过 mss+24，正常不会触发）
                    Buffer.BlockCopy(received, 0, buffer, offset, capacity);
                    return -1;
                }
                Buffer.BlockCopy(received, 0, buffer, offset, received.Length);
                return received.Length;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary> 关闭通道 <para>Close the channel</para> </summary>
        public void Close()
        {
            _client?.Close();
            _client = null;
            _remote = null;
            RemoteEndPoint = null;
        }
    }
}
