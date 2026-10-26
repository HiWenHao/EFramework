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
        #region Properties

        /// <summary>
        /// 消息操作码
        /// <para>Message opcode</para>
        /// </summary>
        public int Opcode { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 构造消息操作码标注。
        /// <para>Create the opcode marker.</para>
        /// </summary>
        /// <param name="opcode">消息操作码<para>Message opcode</para></param>
        public NetOpcodeAttribute(int opcode)
        {
            Opcode = opcode;
        }

        #endregion
    }

    /// <summary>
    /// 原始消息处理器（直接拿到 <see cref="NetPacket"/>，无需序列化器）。
    /// <para>Raw message handler (receives NetPacket directly; no serializer needed).</para>
    /// </summary>
    public interface IMessageHandler
    {
        #region Properties

        /// <summary>
        /// 处理的消息操作码
        /// <para>Handled opcode</para>
        /// </summary>
        int Opcode { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 处理消息
        /// <para>Handle the message</para>
        /// </summary>
        /// <param name="packet">已解码的消息包<para>Decoded packet</para></param>
        void Handle(NetPacket packet);

        #endregion
    }

    /// <summary>
    /// 类型化消息处理器（载荷经 <see cref="IMessageSerializer"/> 反序列化后回调）。
    /// <para>Typed message handler (payload deserialized via IMessageSerializer before callback).</para>
    /// </summary>
    /// <typeparam name="TMessage">消息类型<para>Message type</para></typeparam>
    public interface IMessageHandler<in TMessage>
    {
        #region Properties

        /// <summary>
        /// 处理的消息操作码
        /// <para>Handled opcode</para>
        /// </summary>
        int Opcode { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 处理已反序列化的消息
        /// <para>Handle the deserialized message</para>
        /// </summary>
        /// <param name="message">已反序列化的消息<para>Deserialized message</para></param>
        void Handle(TMessage message);

        #endregion
    }

    /// <summary>
    /// 会话感知的类型化消息处理器：除了消息本身，还带来源会话 Id。
    /// <para>Session-aware typed message handler: receives the source session id besides the message.</para>
    /// </summary>
    /// <remarks>
    /// 多会话并存时（如同时连着登录服与战斗服），用它区分消息来自哪条会话。
    /// <para>Use it when several sessions coexist (e.g. login server and battle server at once)
    /// to tell which session a message came from.</para>
    /// </remarks>
    /// <typeparam name="TMessage">消息类型<para>Message type</para></typeparam>
    public interface ISessionAwareMessageHandler<TMessage>
    {
        #region Methods

        /// <summary>
        /// 处理已反序列化的消息（附带来源会话 Id）
        /// <para>Handle the deserialized message with its source session id</para>
        /// </summary>
        /// <param name="message">已反序列化的消息<para>Deserialized message</para></param>
        /// <param name="sessionId">来源会话 Id（0 表示未指定）<para>Source session id (0 means unspecified)</para></param>
        void Handle(TMessage message, int sessionId);

        #endregion
    }

    /// <summary>
    /// 类型化消息处理器基类：Opcode 默认取自消息类上的 <see cref="NetOpcodeAttribute"/>。
    /// <para>Typed handler base: opcode resolved from NetOpcodeAttribute on the message class by default.</para>
    /// </summary>
    /// <typeparam name="TMessage">消息类型<para>Message type</para></typeparam>
    public abstract class MessageHandler<TMessage> : IMessageHandler<TMessage> where TMessage : class
    {
        #region Properties

        /// <summary>
        /// 消息操作码（来自 <see cref="NetOpcodeAttribute"/>；缺失返回 0，注册时会报错）。
        /// <para>Opcode from NetOpcodeAttribute; 0 when absent (registration will error).</para>
        /// </summary>
        public virtual int Opcode
        {
            get
            {
                var attrs = typeof(TMessage).GetCustomAttributes(typeof(NetOpcodeAttribute), false);
                return attrs is NetOpcodeAttribute[] { Length: > 0 } array ? array[0].Opcode : 0;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 处理已反序列化的消息
        /// <para>Handle the deserialized message</para>
        /// </summary>
        /// <param name="message">已反序列化的消息<para>Deserialized message</para></param>
        public abstract void Handle(TMessage message);

        #endregion
    }

    /// <summary>
    /// 会话感知的类型化消息处理器基类：Opcode 取自消息类上的 <see cref="NetOpcodeAttribute"/>，
    /// 回调额外携带来源会话 Id。
    /// <para>Session-aware typed handler base: opcode from NetOpcodeAttribute; the callback also
    /// receives the source session id.</para>
    /// </summary>
    /// <typeparam name="TMessage">消息类型<para>Message type</para></typeparam>
    public abstract class SessionAwareMessageHandler<TMessage> : IMessageHandler<TMessage>, ISessionAwareMessageHandler<TMessage>
        where TMessage : class
    {
        #region Properties

        /// <summary>
        /// 消息操作码（来自 <see cref="NetOpcodeAttribute"/>；缺失返回 0，注册时会报错）。
        /// <para>Opcode from NetOpcodeAttribute; 0 when absent (registration will error).</para>
        /// </summary>
        public virtual int Opcode
        {
            get
            {
                var attrs = typeof(TMessage).GetCustomAttributes(typeof(NetOpcodeAttribute), false);
                return attrs is NetOpcodeAttribute[] { Length: > 0 } array ? array[0].Opcode : 0;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 处理已反序列化的消息（会话 Id 未知时走此重载）。
        /// <para>Handle the deserialized message (used when the session id is unknown).</para>
        /// </summary>
        /// <param name="message">已反序列化的消息<para>Deserialized message</para></param>
        public virtual void Handle(TMessage message)
        {
            Handle(message, 0);
        }

        /// <summary>
        /// 处理已反序列化的消息（附带来源会话 Id）
        /// <para>Handle the deserialized message with its source session id</para>
        /// </summary>
        /// <param name="message">已反序列化的消息<para>Deserialized message</para></param>
        /// <param name="sessionId">来源会话 Id（0 表示未指定）<para>Source session id (0 means unspecified)</para></param>
        public abstract void Handle(TMessage message, int sessionId);

        #endregion
    }
}