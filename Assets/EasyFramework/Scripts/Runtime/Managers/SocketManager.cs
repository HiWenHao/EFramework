/* 
 * ================================================
 * Describe:      This script is used to control the all managers.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-17 16:31:29
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2022-06-17 16:31:29
 * Version:       0.1 
 * ===============================================
 */
using BestHTTP;
using BestHTTP.WebSocket;
using System;

namespace EasyFramework.Managers
{
    public class SocketManager : Singleton<SocketManager>, IManager
    {
        int IManager.ManagerLevel => EF.Projects.AppConst.ManagerLevels.IndexOf("SocketManager");

        const string Address = "Please changed you address path.";

        private WebSocket m_webSocket;

        void ISingleton.Init()
        {
            if (Address.Contains("Please changed you address path."))
            {
                return;
            }
            m_webSocket = new WebSocket(new Uri(Address));
#if !BESTHTTP_DISABLE_PROXY && !UNITY_WEBGL
            if (HTTPManager.Proxy != null)
                m_webSocket.InternalRequest.Proxy = new HTTPProxy(HTTPManager.Proxy.Address, HTTPManager.Proxy.Credentials, false);
#endif
            m_webSocket.OnOpen += OnOpen;
            m_webSocket.OnMessage += OnMessageReceived;
            m_webSocket.OnBinary += OnOnBinaryReceived;
            m_webSocket.OnClosed += OnClosed;
            m_webSocket.OnError += OnError;

            m_webSocket.Open();
            D.Log("Opening Web Socket...");
        }

        void ISingleton.Quit()
        {
            if (m_webSocket == null)
                return;

            m_webSocket.Close();
            m_webSocket.OnOpen = null;
            m_webSocket.OnMessage = null;
            m_webSocket.OnBinary = null;
            m_webSocket.OnClosed = null;
            m_webSocket.OnError = null;
            m_webSocket = null;            
        }

        public void Send(string msg)
        {
            if (string.IsNullOrEmpty(msg))
            {
                D.Error("You send message is null or empty, please check params.");
                return;
            }
            D.Log("Send message:  " + msg);
            m_webSocket.Send(msg);
        }

        #region WebSocket Event Handlers
        /// <summary>
        /// Called when the web socket is open, and we are ready to send and receive data. 当网络连接开启，并且准备好收发数据时调用
        /// </summary>
        void OnOpen(WebSocket ws)
        {
            D.Log("-WebSocket Open long-link is succeed!");
        }

        /// <summary>
        /// Called when we received a text message from the server.当接收文本流数据时调用
        /// </summary>
        void OnMessageReceived(WebSocket ws, string message)
        {
            D.Log($"Message received:          {message}");
        }

        /// <summary>
        /// Called when we received a binary stream from the server.当接收二进制流数据时调用
        /// </summary>
        void OnOnBinaryReceived(WebSocket ws, byte[] data)
        {
            D.Log($"Binary received:        data.Length = {data.Length}");
        }

        /// <summary>
        /// Called when the web socket closed.当链接关闭时
        /// </summary>
        void OnClosed(WebSocket ws, ushort code, string message)
        {
            D.Log($"-WebSocket closed! Code: {code} Message: {message}");
        }

        /// <summary>
        /// Called when an error occured on client side. 当链接发生错误
        /// </summary>
        void OnError(WebSocket ws, Exception ex)
        {
            string errorMsg = string.Empty;
#if !UNITY_WEBGL || UNITY_EDITOR
            if (ws.InternalRequest.Response != null)
                errorMsg = $"Status Code from Server: {ws.InternalRequest.Response.StatusCode} and Message: {ws.InternalRequest.Response.Message}";
#endif

            D.Error($"-An error occured: {(ex != null ? ex.Message : "Unknown ServerError " + errorMsg)}");
            if (m_webSocket == null)
                return;
            m_webSocket.Close();
            m_webSocket.OnOpen = null;
            m_webSocket.OnMessage = null;
            m_webSocket.OnBinary = null;
            m_webSocket.OnClosed = null;
            m_webSocket.OnError = null;
            m_webSocket = null;
        }
        #endregion
    }
}
