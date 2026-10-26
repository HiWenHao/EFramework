/*
 * ================================================
 * Describe:      Net module opcode definitions.
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
    /// 网络消息操作码（Opcode）约定。
    /// <para>Network message opcode conventions.</para>
    /// </summary>
    /// <remarks>
    /// 分段规则 / Ranges:
    /// <para>1 ~ 999：框架保留（心跳、探测等内置消息）。</para>
    /// <para>1000 以上：业务消息，由使用方自行扩展（可另建静态类或用 <see cref="NetOpcodeAttribute"/> 标注消息类）。</para>
    /// </remarks>
    public static class NetOpcode
    {
        #region Fields

        /// <summary>
        /// Ping（客户端 → 服务端，携带客户端时间戳）
        /// <para>Ping (client to server, carries client timestamp)</para>
        /// </summary>
        public const int Ping = 1;

        /// <summary>
        /// Pong（服务端 → 客户端，回显时间戳）
        /// <para>Pong (server to client, echoes timestamp)</para>
        /// </summary>
        public const int Pong = 2;

        /// <summary>
        /// 断开连接通知（双向，携带原因码）
        /// <para>Disconnect notification (both directions, carries reason code)</para>
        /// </summary>
        public const int Disconnect = 3;

        /// <summary>
        /// 业务消息起始值，自定义 Opcode 请从该值之后开始
        /// <para>First business opcode; define custom opcodes after this value</para>
        /// </summary>
        public const int BusinessBegin = 1000;

        #endregion
    }
}