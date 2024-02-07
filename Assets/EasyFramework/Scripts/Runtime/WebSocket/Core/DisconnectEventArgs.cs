/* 
 * ================================================
 * Describe:      This script is used to control the websocket managers.     Thanks to the author: psygames, can join his QQ group (1126457634) get the latest version.
 * Author:        psygames
 * CreationTime:  2016-06-25 00:00:00
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-02-07 17:21:26
 * Version:       0.1 
 * ===============================================
 */
using System;

namespace EasyFramework.UnityWebSocket
{
    /// <summary>
    /// Represents the event data for the <see cref="IWebSocket.OnClose"/> event.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   That event occurs when the WebSocket connection has been closed.
    ///   </para>
    ///   <para>
    ///   If you would like to get the reason for the close, you should access
    ///   the <see cref="Code"/> or <see cref="Reason"/> property.
    ///   </para>
    /// </remarks>
    public class DisconnectEventArgs : EventArgs
    {
        #region Internal Constructors

        internal DisconnectEventArgs()
        {
        }

        internal DisconnectEventArgs(ushort code)
          : this(code, null)
        {
        }

        internal DisconnectEventArgs(CloseStatusCode code)
          : this((ushort)code, null)
        {
        }

        internal DisconnectEventArgs(CloseStatusCode code, string reason)
          : this((ushort)code, reason)
        {
        }

        internal DisconnectEventArgs(ushort code, string reason)
        {
            Code = code;
            Reason = reason;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the status code for the close.
        /// </summary>
        /// <value>
        /// A <see cref="ushort"/> that represents the status code for the close if any.
        /// </value>
        public ushort Code { get; private set; }

        /// <summary>
        /// Gets the reason for the close.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the reason for the close if any.
        /// </value>
        public string Reason { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the connection has been closed cleanly.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection has been closed cleanly; otherwise, <c>false</c>.
        /// </value>
        public bool WasClean { get; internal set; }

        /// <summary>
        /// Enum value same as Code
        /// </summary>
        public CloseStatusCode StatusCode
        {
            get
            {
                if (Enum.IsDefined(typeof(CloseStatusCode), Code))
                    return (CloseStatusCode)Code;
                return CloseStatusCode.Unknown;
            }
        }

        #endregion
    }
}
