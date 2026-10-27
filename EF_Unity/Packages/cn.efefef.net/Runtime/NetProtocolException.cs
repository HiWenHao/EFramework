/*
 * ================================================
 * Describe:      Net module protocol exception type.
 * Author:        Alvin5100
 * CreationTime:  2026-10-26 18:10:00
 * ModifyAuthor:  Alvin5100
 * ModifyTime:    2026-10-26 18:10:00
 * ScriptVersion: 0.1
 * ===============================================
 */

using System;

namespace EasyFramework.Systems.Net
{
    /// <summary>
    /// 网络协议异常（会话未连接、载荷非法、会话中途关闭等）。
    /// <para>Network protocol exception (not connected, malformed payload, session closed midway).</para>
    /// </summary>
    public sealed class NetProtocolException : Exception
    {
        #region Methods

        /// <summary>
        /// 构造协议异常。
        /// <para>Create a protocol exception.</para>
        /// </summary>
        /// <param name="message">异常描述<para>Exception message</para></param>
        public NetProtocolException(string message) : base(message)
        {
        }

        #endregion
    }
}
