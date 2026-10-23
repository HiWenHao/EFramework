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
        /// <summary> 序列化消息对象 <para>Serialize a message object</para> </summary>
        byte[] Serialize<T>(T message);

        /// <summary> 反序列化消息对象 <para>Deserialize a message object</para> </summary>
        T Deserialize<T>(byte[] payload, int offset, int count);
    }

    /// <summary>
    /// 已解码的网络消息包（只读，派发时传递）。
    /// <para>Decoded network packet (readonly, passed at dispatch).</para>
    /// </summary>
    public readonly struct NetPacket
    {
        /// <summary> 消息操作码 <para>Message opcode</para> </summary>
        public int Opcode { get; }

        /// <summary> RPC 配对 Id（0 = 单向消息） <para>RPC pairing id (0 = one-way message)</para> </summary>
        public int RpcId { get; }

        /// <summary> 载荷字节（帧内数据的独立拷贝，生命周期安全） <para>Payload bytes (an independent copy, lifetime-safe)</para> </summary>
        public byte[] Payload { get; }

        /// <summary> 载荷长度 <para>Payload length</para> </summary>
        public int PayloadLength { get; }

        public NetPacket(int opcode, int rpcId, byte[] payload, int payloadLength)
        {
            Opcode = opcode;
            RpcId = rpcId;
            Payload = payload;
            PayloadLength = payloadLength;
        }
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
        /// <summary> 帧头长度 <para>Frame header size</para> </summary>
        public const int HeaderSize = 12;

        /// <summary> 编码一帧 <para>Encode one frame</para> </summary>
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
        /// <returns>是否解码成功 <para>Whether decoding succeeded</para></returns>
        public static bool TryDecode(byte[] frame, out int opcode, out int rpcId, out int payloadOffset, out int payloadLength)
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
        public static bool TryDecodePacket(byte[] frame, out NetPacket packet)
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
            packet = new NetPacket(opcode, rpcId, payload, length);
            return true;
        }

        private static void WriteInt32(byte[] data, int offset, int value)
        {
            data[offset] = (byte)value;
            data[offset + 1] = (byte)(value >> 8);
            data[offset + 2] = (byte)(value >> 16);
            data[offset + 3] = (byte)(value >> 24);
        }

        private static int ReadInt32(byte[] data, int offset)
        {
            return (int)data[offset] | ((int)data[offset + 1] << 8) | ((int)data[offset + 2] << 16) | ((int)data[offset + 3] << 24);
        }
    }
}
