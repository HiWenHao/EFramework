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
using System.Text;

namespace EasyFramework.UnityWebSocket
{
    public class MessageEventArgs : EventArgs
    {
        private byte[] _rawData;
        private string _data;

        internal MessageEventArgs(Opcode opcode, byte[] rawData)
        {
            Opcode = opcode;
            _rawData = rawData;
        }

        internal MessageEventArgs(Opcode opcode, string data)
        {
            Opcode = opcode;
            _data = data;
        }

        /// <summary>
        /// Gets the opcode for the message.
        /// </summary>
        /// <value>
        /// <see cref="Opcode.Text"/>, <see cref="Opcode.Binary"/>.
        /// </value>
        internal Opcode Opcode { get; private set; }

        /// <summary>
        /// Gets the message data as a <see cref="string"/>.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the message data if its type is
        /// text and if decoding it to a string has successfully done;
        /// otherwise, <see langword="null"/>.
        /// </value>
        public string Data
        {
            get
            {
                SetData();
                return _data;
            }
        }

        /// <summary>
        /// Gets the message data as an array of <see cref="byte"/>.
        /// </summary>
        /// <value>
        /// An array of <see cref="byte"/> that represents the message data.
        /// </value>
        public byte[] RawData
        {
            get
            {
                SetRawData();
                return _rawData;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the message type is binary.
        /// </summary>
        /// <value>
        /// <c>true</c> if the message type is binary; otherwise, <c>false</c>.
        /// </value>
        public bool IsBinary
        {
            get
            {
                return Opcode == Opcode.Binary;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the message type is text.
        /// </summary>
        /// <value>
        /// <c>true</c> if the message type is text; otherwise, <c>false</c>.
        /// </value>
        public bool IsText
        {
            get
            {
                return Opcode == Opcode.Text;
            }
        }

        private void SetData()
        {
            if (_data != null) return;

            if (RawData == null)
            {
                return;
            }

            _data = Encoding.UTF8.GetString(RawData);
        }

        private void SetRawData()
        {
            if (_rawData != null) return;

            if (_data == null)
            {
                return;
            }

            _rawData = Encoding.UTF8.GetBytes(_data);
        }
    }
}