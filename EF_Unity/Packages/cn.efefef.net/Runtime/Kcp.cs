/*
 * ================================================
 * Describe:      KCP reliable-UDP protocol core (C# port of the classic kcp.c algorithm).
 * Author:        Alvin5100
 * CreationTime:  2026-10-23 18:45:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-23 18:50:00
 * ScriptVersion: 0.1
 * ===============================================
 *
 * KCP 是一个跑在 UDP 之上的可靠传输协议（ARQ）：以“快速重传 + 激进 RTO”换取比 TCP 更低的延迟，
 * 适合战斗同步、位置广播等弱网实时场景。本文件是经典 kcp.c 算法（skywind3000，MIT）的 C# 移植，
 * 为纯 C# 实现（无 Unity 依赖），便于在 .NET 侧做闭环单元测试。
 *
 * KCP is a reliable ARQ protocol over UDP trading bandwidth for low latency (fast retransmit +
 * aggressive RTO), suited for realtime weak-network gameplay sync. This is a C# port of the
 * classic kcp.c algorithm (skywind3000, MIT), pure C# with no Unity dependency.
 *
 * 调参参考（ET 经验值）/ Recommended tuning (ET battle-tested values):
 *   NoDelay(1, 10, 2, 1)  // 启用 nodelay；间隔 10ms；触发 2 次冗余 ACK 即快重传；关闭拥塞控制
 *   WndSize(256, 256)     // 收发窗口
 *   SetMtu(512)           // 移动弱网 470~512，避免 IP 层分片
 */

using System;
using System.Collections.Generic;

namespace EasyFramework.Systems.Net
{
    #region 协议常量 / Protocol Constants

    /// <summary> KCP 命令字 <para>KCP command types</para> </summary>
    internal static class KcpCmd
    {
        /// <summary> 数据推送（携带用户数据） <para>Push: carries user data</para> </summary>
        public const byte Push = 81;

        /// <summary> 确认（sn = 被确认的序号，ts = 原发送时间戳，用于 RTT 采样） <para>Ack: sn = acked seq, ts = original send time (RTT sampling)</para> </summary>
        public const byte Ack = 82;

        /// <summary> 询问对端窗口大小 <para>Ask peer window size</para> </summary>
        public const byte Wask = 83;

        /// <summary> 告知对端自己的窗口大小 <para>Tell peer my window size</para> </summary>
        public const byte Wins = 84;
    }

    #endregion

    /// <summary>
    /// KCP 会话实例（对应一条逻辑连接，一个 conv）。
    /// <para>KCP control block representing one logical connection (one conv).</para>
    /// </summary>
    public sealed class Kcp
    {
        #region 常量 / Constants

        private const int RtoNdl = 30;          // nodelay 模式下的最小 RTO(ms)
        private const int RtoMin = 100;         // 普通模式最小 RTO(ms)
        private const int RtoDef = 200;         // 默认 RTO(ms)
        private const int RtoMax = 60000;       // 最大 RTO(ms)
        private const int WndSnd = 32;          // 默认发送窗口(段数)
        private const int WndRcv = 32;          // 默认接收窗口(段数)
        private const int MtuDef = 1400;        // 默认 MTU
        private const int Overhead = 24;        // KCP 段头长度
        private const int IntervalDef = 100;   // 默认内部更新间隔(ms)
        private const int DeadLinkDef = 20;    // 超过该重传次数判定链路死亡
        private const int ProbeInit = 7000;     // 窗口探测初始间隔(ms)
        private const int ProbeLimit = 120000; // 窗口探测最大间隔(ms)
        private const int AckListMax = 512;    // 待发送 ACK 队列上限
        private const int AskSend = 1;
        private const int AskTell = 2;

        #endregion

        #region 内部结构 / Internal Types

        /// <summary> KCP 数据段（对应 kcp.c 的 IKCPSEG） <para>KCP segment (IKCPSEG in kcp.c)</para> </summary>
        private sealed class Segment
        {
            public uint Conv;
            public byte Cmd;
            public int Frg;      // 分片序号：本段之后还剩几片（最后一片为 0）
            public int Wnd;      // 发送时携带的接收窗口通告
            public uint Ts;      // 发送时间戳（ACK 回显用于 RTT 采样）
            public uint Sn;      // 序号
            public uint Una;     // 发送时携带的 snd_una（对端可据此清理）
            public int Len;      // 数据长度
            public int ResendTs; // 下次重传的时间点
            public int Rto;      // 该段当前的重传超时
            public int FastAck;  // 被冗余 ACK 跳过的次数
            public int Xmit;     // 已传输次数
            public byte[] Data;
        }

        #endregion

        private readonly uint _conv;
        private readonly Action<byte[], int> _output;   // 下层发送出口（UDP / WX 等）

        // 段对象池：KCP 段是热路径上的高频分配，池化避免 GC 抖动
        private readonly Queue<Segment> _segPool = new Queue<Segment>();

        // 配置 / Config
        private int _mtu = MtuDef;
        private int _mss;
        private int _sndWnd = WndSnd;
        private int _rcvWnd = WndRcv;
        private bool _nodelay;
        private int _interval = IntervalDef;
        private int _fastresend;      // 0 = 关闭快速重传
        private int _nocwnd;          // 1 = 关闭拥塞控制
        private int _deadLink = DeadLinkDef;

        // 发送状态 / Send state
        private uint _sndUna;         // 最小未确认序号
        private uint _sndNxt;         // 下一个待分配序号
        private int _cwnd = 1;        // 拥塞窗口(段数)
        private uint _rmtWnd = WndRcv;// 对端通告的接收窗口

        // 接收状态 / Receive state
        private uint _rcvNxt;         // 下一个期望接收的序号

        // RTT 估计 / RTT estimation
        private int _rxSrtt;
        private int _rxRttval;
        private int _rxRto = RtoDef;

        // 队列 / Queues
        private readonly LinkedList<Segment> _sndQueue = new LinkedList<Segment>(); // 待发送(未入窗口)
        private readonly LinkedList<Segment> _sndBuf = new LinkedList<Segment>();   // 已发待确认
        private readonly LinkedList<Segment> _rcvBuf = new LinkedList<Segment>();   // 收到但未拼齐
        private readonly LinkedList<Segment> _rcvQueue = new LinkedList<Segment>(); // 已拼齐待用户读取
        private readonly List<uint> _ackList = new List<uint>();                     // 待发送 ACK（sn/ts 成对）

        // 探测与统计 / Probe & stats
        private int _probe;
        private int _probeWait;
        private long _tsProbe;
        private long _tsFlushNext;  // 下次 flush 的时刻(TickCount)
        private bool _updated;
        private bool _dead;         // 链路死亡标记

        // 复用缓冲区：避免每次 Flush 分配 / Reused buffer: no per-flush allocation
        private byte[] _flushBuffer;

        /// <summary> 会话标识（建立后不变） <para>Conversation id (immutable after creation)</para> </summary>
        public uint Conv => _conv;

        /// <summary> 链路是否已判定死亡（重传次数超过上限） <para>Whether the link is considered dead (xmit exceeded limit)</para> </summary>
        public bool IsDead => _dead;

        /// <summary> 等待确认的数据段数 <para>Number of unacknowledged segments in send buffer</para> </summary>
        public int WaitSnd => _sndBuf.Count + _sndQueue.Count;

        /// <summary>
        /// 诊断快照（调试面板 / 测试定位用）。
        /// <para>Diagnostic snapshot (for debug panels and test triage).</para>
        /// </summary>
        public string DebugInfo =>
            $"conv={_conv} una={_sndUna} nxt={_sndNxt} rmtwnd={_rmtWnd} cwnd={_cwnd} rcvnxt={_rcvNxt} " +
            $"sque={_sndQueue.Count} sbuf={_sndBuf.Count} rbuf={_rcvBuf.Count} rque={_rcvQueue.Count} " +
            $"ack={_ackList.Count / 2} dead={_dead} rto={_rxRto} srtt={_rxSrtt}";

        /// <summary>
        /// 创建 KCP 实例。
        /// <para>Create a KCP instance.</para>
        /// </summary>
        /// <param name="conv">会话标识，收发双方必须一致 <para>Conversation id; must match on both peers</para></param>
        /// <param name="output">下层发送出口，参数为 (数据, 长度) <para>Lower-layer sink, signature (data, size)</para></param>
        public Kcp(uint conv, Action<byte[], int> output)
        {
            _conv = conv;
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _mss = _mtu - Overhead;
        }

        #region 配置 / Configuration

        /// <summary>
        /// 设置 nodelay 模式。
        /// <para>Configure nodelay mode.</para>
        /// </summary>
        /// <param name="nodelay">1=启用 <para>1 = enable</para></param>
        /// <param name="interval">内部更新间隔(ms) <para>Internal update interval in ms</para></param>
        /// <param name="resend">触发快速重传的冗余 ACK 次数（0=关闭快重传） <para>Duplicate acks to trigger fast retransmit (0 = off)</para></param>
        /// <param name="nc">1=关闭拥塞控制 <para>1 = disable congestion control</para></param>
        public void NoDelay(int nodelay, int interval, int resend, int nc)
        {
            if (nodelay >= 0)
            {
                _nodelay = nodelay > 0;
                _rxRto = _nodelay ? RtoNdl : RtoDef;
            }
            if (interval >= 0)
            {
                _interval = interval > 0 ? interval : IntervalDef;
            }
            if (resend >= 0) _fastresend = resend;
            if (nc >= 0) _nocwnd = nc;
        }

        /// <summary> 设置收发窗口（段数） <para>Set send/receive window (in segments)</para> </summary>
        public void WndSize(int snd, int rcv)
        {
            if (snd > 0) _sndWnd = snd;
            if (rcv > 0)
            {
                _rcvWnd = rcv;
                if (_rcvWnd < 1) _rcvWnd = 1;
            }
        }

        /// <summary>
        /// 设置 MTU（决定分片大小 mss = mtu - 24）。移动弱网建议 470~512，避免 IP 层分片。
        /// <para>Set MTU (mss = mtu - 24). For mobile weak networks use 470~512 to avoid IP fragmentation.</para>
        /// </summary>
        public void SetMtu(int mtu)
        {
            if (mtu < 50 || mtu > 1500) throw new ArgumentOutOfRangeException(nameof(mtu));
            _mtu = mtu;
            _mss = _mtu - Overhead;
        }

        /// <summary> 设置判定链路死亡的重传次数上限 <para>Set the xmit limit that marks the link as dead</para> </summary>
        public void SetDeadLink(int deadLink)
        {
            if (deadLink > 0) _deadLink = deadLink;
        }

        #endregion

        #region 收发 / Send & Receive

        /// <summary>
        /// 发送一段用户数据（KCP 按 mss 自动分片，保证消息边界）。
        /// <para>Send one user message (KCP fragments by mss; message boundary preserved).</para>
        /// </summary>
        /// <returns>0 成功；负数为错误 <para>0 on success; negative on error</para></returns>
        public int Send(byte[] data, int offset, int count)
        {
            if (data == null || offset < 0 || count < 0 || offset + count > data.Length) return -1;

            int fragmentCount = count <= _mss ? 1 : (count + _mss - 1) / _mss;
            if (fragmentCount > 255) return -2;
            if (fragmentCount > _rcvWnd) return -3; // 对端窗口放不下

            int remaining = count;
            int srcOffset = offset;
            for (int i = 0; i < fragmentCount; i++)
            {
                int size = remaining < _mss ? remaining : _mss;
                var seg = RentSegment();
                seg.Conv = _conv;
                seg.Cmd = KcpCmd.Push;
                seg.Frg = fragmentCount - 1 - i;   // 最后一片为 0
                seg.Wnd = SegmentSpaceFree();
                seg.Ts = 0;
                seg.Sn = _sndNxt++;
                seg.Una = _sndUna;
                seg.Len = size;
                seg.ResendTs = 0;
                seg.Rto = 0;
                seg.FastAck = 0;
                seg.Xmit = 0;
                if (seg.Data == null || seg.Data.Length < size)
                    seg.Data = new byte[size];
                Buffer.BlockCopy(data, srcOffset, seg.Data, 0, size);

                _sndQueue.AddLast(seg);
                srcOffset += size;
                remaining -= size;
            }

            return 0;
        }

        /// <summary>
        /// 接收一段完整的用户数据（一条 Send 对应一次 Recv 的返回值）。
        /// <para>Receive one complete user message (one Send maps to one Recv result).</para>
        /// </summary>
        /// <returns>数据长度；无数据返回 -1 <para>Data size; -1 when nothing available</para></returns>
        public int Recv(byte[] buffer, int offset = 0)
        {
            if (buffer == null || offset < 0 || offset > buffer.Length) return -1;

            if (_rcvQueue.Count == 0) return -1;

            int peek = PeekSize();
            if (peek < 0) return -2;
            if (buffer.Length - offset < peek) return -3;

            int recovered = 0;
            while (_rcvQueue.Count > 0)
            {
                var seg = _rcvQueue.First.Value;
                _rcvQueue.RemoveFirst();
                Buffer.BlockCopy(seg.Data, 0, buffer, offset + recovered, seg.Len);
                recovered += seg.Len;
                ReturnSegment(seg);
                if (seg.Frg == 0) break;
            }

            return recovered;
        }

        /// <summary>
        /// 投喂下端收到的 UDP 数据（KCP 在此完成分片重排 / 确认处理）。
        /// <para>Feed raw UDP bytes in (KCP handles reordering and ack processing here).</para>
        /// </summary>
        /// <returns>0 成功；负数为协议错误 <para>0 on success; negative on protocol error</para></returns>
        public int Input(byte[] data, int offset, int size)
        {
            if (data == null || offset < 0 || size < 0 || offset + size > data.Length) return -1;

            int pos = offset;
            int remaining = size;

            while (true)
            {
                if (remaining < Overhead) break;

                uint conv = ReadUInt32(data, pos);
                byte cmd = data[pos + 4];
                int frg = data[pos + 5];
                int wnd = ReadUInt16(data, pos + 6);
                uint ts = ReadUInt32(data, pos + 8);
                uint sn = ReadUInt32(data, pos + 12);
                uint una = ReadUInt32(data, pos + 16);
                int len = (int)ReadUInt32(data, pos + 20);

                if (conv != _conv) return -1;
                remaining -= Overhead;
                if (remaining < len || len < 0) return -2;
                if (cmd != KcpCmd.Push && cmd != KcpCmd.Ack && cmd != KcpCmd.Wask && cmd != KcpCmd.Wins)
                    return -3;

                _rmtWnd = (uint)wnd;
                ParseUna(una);

                if (cmd == KcpCmd.Ack)
                {
                    ParseAck(sn, ts);
                }
                else if (cmd == KcpCmd.Push)
                {
                    // 窗口内的 PUSH 一律登记 ACK（重复段也 ACK，对端才能停止重传）
                    if (Itimediff(sn, _rcvNxt + (uint)_rcvWnd) < 0)
                    {
                        AckPush(sn, ts);
                        if (Itimediff(sn, _rcvNxt) >= 0)
                        {
                            var seg = RentSegment();
                            seg.Conv = conv;
                            seg.Cmd = cmd;
                            seg.Frg = frg;
                            seg.Wnd = wnd;
                            seg.Ts = ts;
                            seg.Sn = sn;
                            seg.Una = una;
                            seg.Len = len;
                            if (seg.Data == null || seg.Data.Length < len)
                                seg.Data = new byte[len];
                            Buffer.BlockCopy(data, pos + Overhead, seg.Data, 0, len);
                            InsertRcvBuf(seg); // 内部按 sn 去重
                        }
                    }
                }
                else if (cmd == KcpCmd.Wask)
                {
                    _probe |= AskTell;
                }
                // Wins 只更新窗口（上面 _rmtWnd 已处理）

                pos += Overhead + len;
                remaining -= len;
            }

            return 0;
        }

        #endregion

        #region 驱动 / Drive

        /// <summary>
        /// 推进 KCP：发送待发数据 / 处理重传 / 刷新 ACK / 窗口探测。由 NetworkManager 的 Update 统一驱动。
        /// <para>Advance KCP: flush pending sends, retransmissions, acks and window probes.
        /// Driven by NetworkManager's Update.</para>
        /// </summary>
        public void Update()
        {
            long current = Now();

            if (_updated == false)
            {
                _updated = true;
                _tsFlushNext = 0;
            }

            // 窗口探测：对端窗口为 0 时定期询问
            if (_rmtWnd == 0)
            {
                if (_probeWait == 0)
                {
                    _probeWait = ProbeInit;
                    _tsProbe = current + _probeWait;
                }
                else if (current - _tsProbe >= 0)
                {
                    _probeWait = Math.Min(_probeWait * 2, ProbeLimit);
                    _tsProbe = current + _probeWait;
                    _probe |= AskSend;
                }
            }
            else
            {
                _tsProbe = 0;
                _probeWait = 0;
            }

            Flush(current);
        }

        /// <summary>
        /// 查询距离下一次需要 Update 的毫秒数。返回 &lt;= 0 表示应立即 Update。
        /// <para>Milliseconds until the next Update; &lt;= 0 means update immediately.</para>
        /// </summary>
        public long Check()
        {
            if (_updated == false) return 0;

            long current = Now();
            long tsFlush = _tsFlushNext == 0 ? current : _tsFlushNext;
            long tsProbe = _tsProbe == 0 ? long.MaxValue : _tsProbe;

            long earliest = tsFlush < tsProbe ? tsFlush : tsProbe;
            return earliest - current;
        }

        #endregion

        #region 内部逻辑 / Internals

        /// <summary> 当前时间（TickCount，与 kcp.c 的 iclock 一致按 32 位回绕处理） <para>Current time (TickCount; 32-bit wrap handled like kcp.c iclock)</para> </summary>
        private static long Now() => Environment.TickCount & 0xFFFFFFFFL;

        /// <summary> 回绕安全的差值 <para>Wrap-safe signed difference</para> </summary>
        private static int Itimediff(uint later, uint earlier) => (int)(later - earlier);

        private static uint ReadUInt32(byte[] data, int offset)
        {
            return (uint)data[offset] | ((uint)data[offset + 1] << 8) | ((uint)data[offset + 2] << 16) | ((uint)data[offset + 3] << 24);
        }

        private static int ReadUInt16(byte[] data, int offset)
        {
            return data[offset] | (data[offset + 1] << 8);
        }

        private static void WriteUInt32(byte[] data, int offset, uint value)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            data[offset + 2] = (byte)(value >> 16);
            data[offset + 3] = (byte)(value >> 24);
        }

        private static void WriteUInt16(byte[] data, int offset, int value)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
        }

        /// <summary> 接收窗口剩余空间（通告给对端） <para>Free space of receive window (advertised to peer)</para> </summary>
        private int SegmentSpaceFree()
        {
            int free = _rcvWnd - _rcvQueue.Count;
            return free < 0 ? 0 : free;
        }

        private Segment RentSegment()
        {
            return _segPool.Count > 0 ? _segPool.Dequeue() : new Segment();
        }

        private void ReturnSegment(Segment seg)
        {
            if (_segPool.Count > 1024) return; // 池已满，直接丢弃
            _segPool.Enqueue(seg);
        }

        /// <summary> 按序号有序插入接收缓冲，重复序号直接丢弃 <para>Insert into receive buffer in sn order; duplicates dropped</para> </summary>
        private void InsertRcvBuf(Segment seg)
        {
            var node = _rcvBuf.First;
            while (node != null)
            {
                var existing = node.Value;
                if (existing.Sn == seg.Sn)
                {
                    ReturnSegment(seg); // 重复段
                    return;
                }
                if (Itimediff(seg.Sn, existing.Sn) < 0) break;
                node = node.Next;
            }

            if (node == null) _rcvBuf.AddLast(seg);
            else _rcvBuf.AddBefore(node, seg);

            // 连续前缀移入待读队列
            while (_rcvBuf.Count > 0)
            {
                var head = _rcvBuf.First.Value;
                if (head.Sn == _rcvNxt && _rcvQueue.Count < _rcvWnd)
                {
                    _rcvBuf.RemoveFirst();
                    _rcvQueue.AddLast(head);
                    _rcvNxt++;
                }
                else break;
            }
        }

        /// <summary> 计算下一段完整消息的长度（Recv 前检查缓冲区用） <para>Peek the size of the next complete message before Recv</para> </summary>
        private int PeekSize()
        {
            if (_rcvQueue.Count == 0) return -1;

            var seg = _rcvQueue.First.Value;
            if (seg.Frg == 0) return seg.Len;

            // 多分片消息：同一消息的后续分片必须已在队列中
            int expected = seg.Frg;
            var node = _rcvQueue.First.Next;
            while (node != null && expected > 0)
            {
                if (node.Value.Frg != expected - 1) return -1;
                expected--;
                node = node.Next;
            }
            if (expected > 0) return -1; // 还没收齐

            int total = 0;
            foreach (var s in _rcvQueue)
            {
                total += s.Len;
                if (s.Frg == 0) break;
            }
            return total;
        }

        /// <summary> 处理对端 una：清理已确认段 <para>Handle peer una: purge acked segments</para> </summary>
        private void ParseUna(uint una)
        {
            if (Itimediff(una, _sndUna) <= 0) return;

            var node = _sndBuf.First;
            while (node != null)
            {
                var seg = node.Value;
                var next = node.Next;
                if (Itimediff(una, seg.Sn) > 0)
                {
                    _sndBuf.Remove(node);
                    ReturnSegment(seg);
                }
                node = next;
            }

            _sndUna = una;

            // 拥塞窗口：累计确认前进时线性增长（关闭拥塞控制时不增长）
            if (_nocwnd == 0 && _cwnd < _sndWnd) _cwnd++;
        }

        /// <summary> 处理 ACK：RTT 采样 / 精确清理发送缓冲 / 冗余计数 / 推进 snd_una <para>Handle ack: RTT sampling, exact-match purge, fast-ack count, advance snd_una</para> </summary>
        private void ParseAck(uint sn, uint ts)
        {
            if (Itimediff(sn, _sndUna) < 0 || Itimediff(sn, _sndNxt) >= 0) return;

            long current = Now();
            int rtt = Itimediff((uint)current, ts);
            if (rtt >= 0)
            {
                if (_rxSrtt == 0)
                {
                    _rxSrtt = rtt;
                    _rxRttval = rtt / 2;
                }
                else
                {
                    int delta = rtt - _rxSrtt;
                    if (delta < 0) delta = -delta;
                    _rxRttval = (3 * _rxRttval + delta) / 4;
                    _rxSrtt = (7 * _rxSrtt + rtt) / 8;
                    if (_rxSrtt < 1) _rxSrtt = 1;
                }
                int rto = _rxSrtt / 8 + 4 * _rxRttval;
                int min = _nodelay ? RtoNdl : RtoMin;
                if (rto < min) rto = min;
                if (rto > RtoMax) rto = RtoMax;
                _rxRto = rto;
            }

            // 个体 ACK：只精确删除对应段；sn 更大的段被“跳过”→ 累计快重传计数
            var node = _sndBuf.First;
            while (node != null)
            {
                var seg = node.Value;
                var next = node.Next;
                if (seg.Sn == sn)
                {
                    _sndBuf.Remove(node);
                    ReturnSegment(seg);
                }
                else if (Itimediff(seg.Sn, sn) > 0)
                {
                    seg.FastAck++;
                }
                node = next;
            }

            // 推进 snd_una = 发送缓冲中的最小未确认序号（缓冲空则 = snd_nxt）
            if (_sndBuf.Count > 0)
            {
                uint firstSn = _sndBuf.First.Value.Sn;
                if (Itimediff(firstSn, _sndUna) > 0) _sndUna = firstSn;
            }
            else if (Itimediff(_sndNxt, _sndUna) > 0)
            {
                _sndUna = _sndNxt;
            }
        }

        /// <summary> 记录一个待发送 ACK <para>Queue an ack to be sent</para> </summary>
        private void AckPush(uint sn, uint ts)
        {
            if (_ackList.Count >= AckListMax * 2) return; // 队列满，丢弃（对端会重传，代价是多余重传）
            _ackList.Add(sn);
            _ackList.Add(ts);
        }

        /// <summary>
        /// 核心刷新：发送窗口内数据 / 重传超时段 / 快重传 / ACK / 探测。
        /// <para>Core flush: send in-window data, timed-out retransmits, fast retransmit, acks, probes.</para>
        /// </summary>
        private void Flush(long current)
        {
            // 1. 窗口允许时把 snd_queue 移入 snd_buf
            // 注意：必须按【队首段自己的 sn】判断而非 snd_nxt——否则一条分片数超过当前窗口的
            // 大消息会一片都发不出去（无 ACK → 窗口永不前进 → 死锁）。
            uint cwnd = (uint)Math.Min(_sndWnd, _rmtWnd);
            if (_nocwnd == 0) cwnd = Math.Min(cwnd, (uint)_cwnd);

            while (_sndQueue.Count > 0)
            {
                var head = _sndQueue.First.Value;
                if (Itimediff(head.Sn, _sndUna + cwnd) >= 0) break; // 该段超出窗口，等 ACK
                _sndQueue.RemoveFirst();
                _sndBuf.AddLast(head);
            }

            // 2. 遍历 snd_buf：发送 / 重传
            if (_flushBuffer == null) _flushBuffer = new byte[_mtu];
            byte[] buffer = _flushBuffer;
            int ptr = 0;

            var node = _sndBuf.First;
            while (node != null)
            {
                var seg = node.Value;
                node = node.Next;

                bool needsend = false;
                if (seg.Xmit == 0)
                {
                    needsend = true;
                    seg.Xmit++;
                    seg.Rto = _rxRto;
                    seg.ResendTs = (int)(current + seg.Rto + Math.Min(seg.Rto, _interval));
                }
                else if (current - seg.ResendTs >= 0)
                {
                    needsend = true;
                    seg.Xmit++;
                    seg.Rto += Math.Max(seg.Rto >> 3, RtoMin);
                    seg.ResendTs = (int)(current + seg.Rto);
                    if (seg.Xmit >= _deadLink) _dead = true;
                }
                else if (_fastresend > 0 && seg.FastAck >= _fastresend)
                {
                    needsend = true;
                    seg.Xmit++;
                    seg.FastAck = 0;
                    seg.ResendTs = (int)(current + seg.Rto);
                }

                if (needsend)
                {
                    seg.Ts = (uint)current;
                    seg.Wnd = SegmentSpaceFree();
                    seg.Una = _rcvNxt; // una = 我方接收水位，对端据此累计释放发送缓冲

                    int need = Overhead + seg.Len;
                    if (ptr + need > _mtu)
                    {
                        _output(buffer, ptr);
                        ptr = 0;
                    }

                    WriteUInt32(buffer, ptr, seg.Conv);
                    buffer[ptr + 4] = seg.Cmd;
                    buffer[ptr + 5] = (byte)seg.Frg;
                    WriteUInt16(buffer, ptr + 6, seg.Wnd);
                    WriteUInt32(buffer, ptr + 8, seg.Ts);
                    WriteUInt32(buffer, ptr + 12, seg.Sn);
                    WriteUInt32(buffer, ptr + 16, seg.Una);
                    WriteUInt32(buffer, ptr + 20, (uint)seg.Len);
                    Buffer.BlockCopy(seg.Data, 0, buffer, ptr + Overhead, seg.Len);
                    ptr += need;
                }
            }

            // 3. 冲刷待发送 ACK（每个 ACK 一个零长段，打包到 MTU 内）
            for (int i = 0; i < _ackList.Count; i += 2)
            {
                if (ptr + Overhead > _mtu)
                {
                    _output(buffer, ptr);
                    ptr = 0;
                }
                WriteUInt32(buffer, ptr, _conv);
                buffer[ptr + 4] = KcpCmd.Ack;
                buffer[ptr + 5] = 0;
                WriteUInt16(buffer, ptr + 6, SegmentSpaceFree());
                WriteUInt32(buffer, ptr + 8, _ackList[i + 1]); // ts = 原发送时间戳
                WriteUInt32(buffer, ptr + 12, _ackList[i]);    // sn = 被确认序号
                WriteUInt32(buffer, ptr + 16, _rcvNxt);        // una = 我方接收水位
                WriteUInt32(buffer, ptr + 20, 0);
                ptr += Overhead;
            }
            _ackList.Clear();

            // 4. 窗口探测
            if (_probe != 0)
            {
                if (ptr + Overhead > _mtu)
                {
                    _output(buffer, ptr);
                    ptr = 0;
                }
                WriteUInt32(buffer, ptr, _conv);
                buffer[ptr + 4] = (_probe & AskSend) != 0 ? KcpCmd.Wask : KcpCmd.Wins;
                buffer[ptr + 5] = 0;
                WriteUInt16(buffer, ptr + 6, SegmentSpaceFree());
                WriteUInt32(buffer, ptr + 8, (uint)current);
                WriteUInt32(buffer, ptr + 12, 0);
                WriteUInt32(buffer, ptr + 16, _rcvNxt); // una = 我方接收水位
                WriteUInt32(buffer, ptr + 20, 0);
                ptr += Overhead;
            }
            _probe = 0;

            if (ptr > 0)
            {
                _output(buffer, ptr);
            }

            _tsFlushNext = current + _interval;
        }

        #endregion
    }
}
