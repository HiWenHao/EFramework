/*
 * ================================================
 * Describe:      Net module handler bases and opcode attribute.
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
    /// 标注消息类的 Opcode（供类型化注册时自动取码）。
    /// <para>Marks a message class with its opcode (auto-resolved on typed registration).</para>
    /// </summary>
    /// <example>
    /// [NetOpcode(1001)] public class LoginReq { public string Token; }
    /// </example>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class NetOpcodeAttribute : Attribute
    {
        /// <summary> 消息操作码 <para>Message opcode</para> </summary>
        public int Opcode { get; }

        public NetOpcodeAttribute(int opcode)
        {
            Opcode = opcode;
        }
    }

    /// <summary>
    /// 原始消息处理器（直接拿到 <see cref="NetPacket"/>，无需序列化器）。
    /// <para>Raw message handler (receives NetPacket directly; no serializer needed).</para>
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary> 处理的消息操作码 <para>Handled opcode</para> </summary>
        int Opcode { get; }

        /// <summary> 处理消息 <para>Handle the message</para> </summary>
        void Handle(NetPacket packet);
    }

    /// <summary>
    /// 类型化消息处理器（载荷经 <see cref="IMessageSerializer"/> 反序列化后回调）。
    /// <para>Typed message handler (payload deserialized via IMessageSerializer before callback).</para>
    /// </summary>
    /// <typeparam name="TMessage">消息类型 <para>Message type</para></typeparam>
    public interface IMessageHandler<in TMessage>
    {
        /// <summary> 处理的消息操作码 <para>Handled opcode</para> </summary>
        int Opcode { get; }

        /// <summary> 处理已反序列化的消息 <para>Handle the deserialized message</para> </summary>
        void Handle(TMessage message);
    }

    /// <summary>
    /// 类型化消息处理器基类：Opcode 默认取自消息类上的 <see cref="NetOpcodeAttribute"/>。
    /// <para>Typed handler base: opcode resolved from NetOpcodeAttribute on the message class by default.</para>
    /// </summary>
    public abstract class MessageHandler<TMessage> : IMessageHandler<TMessage> where TMessage : class
    {
        /// <summary>
        /// 消息操作码（来自 <see cref="NetOpcodeAttribute"/>；缺失返回 0，注册时会报错）。
        /// <para>Opcode from NetOpcodeAttribute; 0 when absent (registration will error).</para>
        /// </summary>
        public virtual int Opcode
        {
            get
            {
                var attrs = typeof(TMessage).GetCustomAttributes(typeof(NetOpcodeAttribute), false);
                return attrs is NetOpcodeAttribute[] array && array.Length > 0 ? array[0].Opcode : 0;
            }
        }

        /// <summary> 处理已反序列化的消息 <para>Handle the deserialized message</para> </summary>
        public abstract void Handle(TMessage message);
    }
}
