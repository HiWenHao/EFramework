/*
 * ================================================
 * Describe:      Net module message pipe (frame codec) and serializer contract.
 * Author:        Alvin5100
 * CreationTime:  2026-10-23 18:45:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-23 18:45:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 消息序列化契约：把消息对象与字节载荷互转。
    /// <para>Message serialization contract between message objects and byte payloads.</para>
    /// </summary>
    /// <remarks>
    /// 框架不内置具体序列化实现（Protobuf / MessagePack / JSON 由使用方按需注入），
    /// 保证包体零额外依赖；在启动时赋值给 <see cref="MessageDispatcher.Serializer"/> 即可启用类型化 Handler 与 RPC。
    /// <para>No built-in implementation (inject Protobuf / MessagePack / JSON as needed) to keep the
    /// package dependency-free; assign to MessageDispatcher.Serializer at startup.</para>
    /// </remarks>
    public interface IMessageSerializer
    {
        #region Methods

        /// <summary>
        /// 序列化消息对象
        /// <para>Serialize a message object</para>
        /// </summary>
        /// <param name="message">消息对象<para>Message object</para></param>
        /// <returns>序列化后的字节<para>Serialized bytes</para></returns>
        byte[] Serialize<T>(T message);

        /// <summary>
        /// 反序列化消息对象
        /// <para>Deserialize a message object</para>
        /// </summary>
        /// <param name="payload">载荷字节<para>Payload bytes</para></param>
        /// <param name="offset">起始偏移<para>Start offset</para></param>
        /// <param name="count">字节数<para>Byte count</para></param>
        /// <returns>反序列化后的消息对象<para>Deserialized message object</para></returns>
        T Deserialize<T>(byte[] payload, int offset, int count);

        #endregion
    }

    /// <summary>
    /// 已解码的网络消息包（只读，派发时传递）。
    /// <para>Decoded network packet (readonly, passed at dispatch).</para>
    /// </summary>
    /// <remarks>
    /// <see cref="SessionId"/> 为 0 表示消息来源未指定（不限定会话）。
    /// <para>A SessionId of 0 means the message source is unspecified (session-agnostic).</para>
    /// </remarks>
    public readonly struct NetPacket
    {
        #region Properties

        /// <summary>
        /// 消息来源会话 Id（0 表示未指定）
        /// <para>Source session id (0 means unspecified)</para>
        /// </summary>
        public int SessionId { get; }

        /// <summary>
        /// 消息操作码
        /// <para>Message opcode</para>
        /// </summary>
        public int Opcode { get; }

        /// <summary>
        /// RPC 配对 Id（0 = 单向消息）
        /// <para>RPC pairing id (0 = one-way message)</para>
        /// </summary>
        public int RpcId { get; }

        /// <summary>
        /// 载荷字节（帧内数据的独立拷贝，生命周期安全）
        /// <para>Payload bytes (an independent copy, lifetime-safe)</para>
        /// </summary>
        public byte[] Payload { get; }

        /// <summary>
        /// 载荷长度
        /// <para>Payload length</para>
        /// </summary>
        public int PayloadLength { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 创建消息包。
        /// <para>Create a packet.</para>
        /// </summary>
        /// <param name="sessionId">来源会话 Id（0 表示未指定）<para>Source session id (0 means unspecified)</para></param>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="rpcId">RPC 配对 Id<para>RPC pairing id</para></param>
        /// <param name="payload">载荷字节<para>Payload bytes</para></param>
        /// <param name="payloadLength">载荷长度<para>Payload length</para></param>
        public NetPacket(int sessionId, int opcode, int rpcId, byte[] payload, int payloadLength)
        {
            SessionId = sessionId;
            Opcode = opcode;
            RpcId = rpcId;
            Payload = payload;
            PayloadLength = payloadLength;
        }

        #endregion
    }

    /// <summary>
    /// 消息帧编解码：头部 12 字节 + 载荷。
    /// <para>Message frame codec: 12-byte header + payload.</para>
    /// </summary>
    /// <remarks>
    /// 帧格式 / Frame layout:
    /// <para>| opcode(int32) | rpcId(int32) | payloadLength(int32) | payload(bytes...) |</para>
    /// <para>rpcId = 0 表示单向消息；非 0 表示请求/响应配对（响应回显同一 rpcId）。</para>
    /// <para>rpcId = 0 means one-way; non-zero pairs a request with its response (echoed back).</para>
    /// </remarks>
    public static class MessagePipe
    {
        #region Fields

        /// <summary>
        /// 帧头长度
        /// <para>Frame header size</para>
        /// </summary>
        public const int HeaderSize = 12;

        #endregion

        #region Methods

        // 小端序写入 int32
        private static void WriteInt32(byte[] data, int offset, int value)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            data[offset + 2] = (byte)(value >> 16);
            data[offset + 3] = (byte)(value >> 24);
        }

        // 小端序读取 int32
        private static int ReadInt32(byte[] data, int offset)
        {
            return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
        }

        /// <summary>
        /// 编码一帧
        /// <para>Encode one frame</para>
        /// </summary>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        /// <param name="rpcId">RPC 配对 Id<para>RPC pairing id</para></param>
        /// <param name="payload">载荷字节<para>Payload bytes</para></param>
        /// <returns>编码后的帧<para>Encoded frame</para></returns>
        public static byte[] Encode(int opcode, int rpcId, byte[] payload)
        {
            int payloadLength = payload?.Length ?? 0;
            var frame = new byte[HeaderSize + payloadLength];
            WriteInt32(frame, 0, opcode);
            WriteInt32(frame, 4, rpcId);
            WriteInt32(frame, 8, payloadLength);
            if (payloadLength > 0)
                Buffer.BlockCopy(payload, 0, frame, HeaderSize, payloadLength);
            return frame;
        }

        /// <summary>
        /// 尝试解码一帧（零拷贝：通过偏移量指向帧内载荷）。
        /// <para>Try to decode one frame (zero-copy: payload referenced by offset).</para>
        /// </summary>
        /// <param name="frame">帧字节<para>Frame bytes</para></param>
        /// <param name="opcode">输出消息操作码<para>Output message opcode</para></param>
        /// <param name="rpcId">输出 RPC 配对 Id<para>Output RPC pairing id</para></param>
        /// <param name="payloadOffset">输出载荷偏移<para>Output payload offset</para></param>
        /// <param name="payloadLength">输出载荷长度<para>Output payload length</para></param>
        /// <returns>是否解码成功<para>Whether decoding succeeded</para></returns>
        public static bool TryDecode(byte[] frame, out int opcode, out int rpcId, out int payloadOffset,
            out int payloadLength)
        {
            opcode = 0;
            rpcId = 0;
            payloadOffset = 0;
            payloadLength = 0;

            if (frame == null || frame.Length < HeaderSize) return false;

            opcode = ReadInt32(frame, 0);
            rpcId = ReadInt32(frame, 4);
            payloadLength = ReadInt32(frame, 8);

            if (payloadLength < 0 || HeaderSize + payloadLength > frame.Length) return false;

            payloadOffset = HeaderSize;
            return true;
        }

        /// <summary>
        /// 把帧解码为 <see cref="NetPacket"/>（载荷独立拷贝，派发路径使用）。
        /// <para>Decode a frame into NetPacket (payload copied; used on the dispatch path).</para>
        /// </summary>
        /// <param name="frame">帧字节<para>Frame bytes</para></param>
        /// <param name="packet">输出消息包<para>Output packet</para></param>
        /// <returns>是否解码成功<para>Whether decoding succeeded</para></returns>
        public static bool TryDecodePacket(byte[] frame, out NetPacket packet)
        {
            return TryDecodePacket(0, frame, out packet);
        }

        /// <summary>
        /// 把帧解码为 <see cref="NetPacket"/> 并标记来源会话。
        /// <para>Decode a frame into NetPacket and tag it with the source session.</para>
        /// </summary>
        /// <param name="sessionId">来源会话 Id<para>Source session id</para></param>
        /// <param name="frame">帧字节<para>Frame bytes</para></param>
        /// <param name="packet">输出消息包<para>Output packet</para></param>
        /// <returns>是否解码成功<para>Whether decoding succeeded</para></returns>
        public static bool TryDecodePacket(int sessionId, byte[] frame, out NetPacket packet)
        {
            packet = default;
            if (!TryDecode(frame, out int opcode, out int rpcId, out int offset, out int length))
                return false;

            byte[] payload = null;
            if (length > 0)
            {
                payload = new byte[length];
                Buffer.BlockCopy(frame, offset, payload, 0, length);
            }

            packet = new NetPacket(sessionId, opcode, rpcId, payload, length);
            return true;
        }

        #endregion
    }
}
