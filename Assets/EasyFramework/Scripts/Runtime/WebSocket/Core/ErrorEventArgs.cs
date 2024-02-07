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
    /// Represents the event data for the <see cref="IWebSocket.OnError"/> event.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   That event occurs when the <see cref="IWebSocket"/> gets an error.
    ///   </para>
    ///   <para>
    ///   If you would like to get the error message, you should access
    ///   the <see cref="Message"/> property.
    ///   </para>
    ///   <para>
    ///   And if the error is due to an exception, you can get it by accessing
    ///   the <see cref="Exception"/> property.
    ///   </para>
    /// </remarks>
    public class ErrorEventArgs : EventArgs
    {
        #region Internal Constructors

        internal ErrorEventArgs(string message)
          : this(message, null)
        {
        }

        internal ErrorEventArgs(string message, Exception exception)
        {
            this.Message = message;
            this.Exception = exception;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the exception that caused the error.
        /// </summary>
        /// <value>
        /// An <see cref="System.Exception"/> instance that represents the cause of
        /// the error if it is due to an exception; otherwise, <see langword="null"/>.
        /// </value>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the error message.
        /// </value>
        public string Message { get; private set; }

        #endregion
    }
}
