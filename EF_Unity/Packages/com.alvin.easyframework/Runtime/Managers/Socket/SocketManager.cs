/* 
 * ================================================
 * Describe:      This script is used to control the all managers.
 * Author:        Xiaohei.Wang(Wenhao)
 * CreationTime:  2022-06-17 16:31:29
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2023-05-26 16:31:29
 * Version:       0.1 
 * ===============================================
 */

#if !BESTHTTP_DISABLE_PROXY && !UNITY_WEBGL
using BestHTTP;
#endif
using BestHTTP.WebSocket;
using System;
using System.Collections.Generic;

namespace EasyFramework.Managers
{
    public class SocketManager : Singleton<SocketManager>, IManager
    {
        private int Count;
        private List<WebSocket> _webSocketList;

        void ISingleton.Init()
        {
            _webSocketList = new List<WebSocket>();
        }

        void ISingleton.Quit()
        {
            DisposeAll();
            _webSocketList.Clear();
            _webSocketList = null;
        }

        /// <summary>
        /// Creates a WebSocket instance from the given uri.
        /// <para>从给定的uri创建WebSocket实例</para>
        /// </summary>
        /// <param name="uri">The uri of the WebSocket server. <para>WebSocket服务器的uri</para></param>
        /// <param name="onOpen">Called when the connection to the WebSocket server is established. <para>当与WebSocket服务器的连接建立时调用</para></param>
        /// <param name="onMessage">Called when a new textual message is received from the server. <para>当从服务器接收到新的文本消息时调用</para></param>
        /// <param name="onBinary">Called when a new binary message is received from the server. <para>当从服务器接收到新的二进制消息时调用</para></param>
        /// <param name="onClosed">Called when the WebSocket connection is closed. <para>当WebSocket连接关闭时调用</para></param>
        /// <param name="onError">Called when an error is encountered. The Exception parameter may be null. <para>遇到错误时调用。Exception参数可能为空</para></param>
        /// <param name="onErrorDescription">Called when an error is encountered. The parameter will be the description of the error. <para>遇到错误时调用。该参数将是错误的描述</para></param>
        /// <param name="onIncompleteFrame">Called when an incomplete frame received. No attempt will be made to reassemble these fragments internally, and no reference are stored after this event to this frame.
        /// <para>当接收到不完整帧时调用。不会尝试在内部重新组装这些片段，并且在此事件之后不会存储对该帧的引用。</para></param>
        public WebSocket CreateAndOpenWebSocket(Uri uri, OnWebSocketOpenDelegate onOpen = null, OnWebSocketMessageDelegate onMessage = null,
            OnWebSocketBinaryDelegate onBinary = null, OnWebSocketClosedDelegate onClosed = null, OnWebSocketErrorDelegate onError = null,
            OnWebSocketErrorDescriptionDelegate onErrorDescription = null
#if (!UNITY_WEBGL || UNITY_EDITOR)
            , OnWebSocketIncompleteFrameDelegate onIncompleteFrame = null
#endif
            )
        {
            WebSocket ws = new WebSocket(uri);
            Register(ws, onOpen, onMessage, onBinary, onClosed, onError, onErrorDescription
#if (!UNITY_WEBGL || UNITY_EDITOR)
                , onIncompleteFrame
#endif
                );
            return ws;
        }

        /// <summary>
        /// Creates a WebSocket instance from the given uri, protocol and origin.
        /// <para>根据给定的uri、协议和来源创建WebSocket实例</para>
        /// </summary>
        /// <param name="uri">The uri of the WebSocket server. <para>WebSocket服务器的uri</para></param>
        /// <param name="origin">Servers that are not intended to process input from any web page but only for certain sites SHOULD verify the |Origin| field is an origin they expect.
        /// If the origin indicated is unacceptable to the server, then it SHOULD respond to the WebSocket handshake with a reply containing HTTP 403 Forbidden status code.
        /// <para>如果服务器不打算处理来自任何网页的输入，而只针对某些站点，则应该验证|Origin|字段是否是他们期望的来源。如果指定的来源对服务器来说是不可接受的，那么它应该用一个包含HTTP 403 Forbidden状态码的应答来响应WebSocket握手。</para></param>
        /// <param name="protocol">The application-level protocol that the client want to use.Can be null or empty string if not used. 
        /// <para>客户端想要使用的应用程序级协议。如果不使用，可以是空字符串或空字符串</para></param>
        /// <param name="onOpen">Called when the connection to the WebSocket server is established. <para>当与WebSocket服务器的连接建立时调用</para></param>
        /// <param name="onMessage">Called when a new textual message is received from the server. <para>当从服务器接收到新的文本消息时调用</para></param>
        /// <param name="onBinary">Called when a new binary message is received from the server. <para>当从服务器接收到新的二进制消息时调用</para></param>
        /// <param name="onClosed">Called when the WebSocket connection is closed. <para>当WebSocket连接关闭时调用</para></param>
        /// <param name="onError">Called when an error is encountered. The Exception parameter may be null. <para>遇到错误时调用。Exception参数可能为空</para></param>
        /// <param name="onErrorDescription">Called when an error is encountered. The parameter will be the description of the error. <para>遇到错误时调用。该参数将是错误的描述</para></param>
        /// <param name="onIncompleteFrame">Called when an incomplete frame received. No attempt will be made to reassemble these fragments internally, and no reference are stored after this event to this frame.
        /// <para>当接收到不完整帧时调用。不会尝试在内部重新组装这些片段，并且在此事件之后不会存储对该帧的引用。</para></param>
        /// <param name="extensions">Optional IExtensions implementations. <para>可选IExtensions实现</para></param>
        public WebSocket CreateAndOpenWebSocket(Uri uri, string origin, string protocol, OnWebSocketOpenDelegate onOpen = null,
            OnWebSocketMessageDelegate onMessage = null, OnWebSocketBinaryDelegate onBinary = null, OnWebSocketClosedDelegate onClosed = null,
            OnWebSocketErrorDelegate onError = null, OnWebSocketErrorDescriptionDelegate onErrorDescription = null
#if (!UNITY_WEBGL || UNITY_EDITOR)
            , OnWebSocketIncompleteFrameDelegate onIncompleteFrame = null,
            params BestHTTP.WebSocket.Extensions.IExtension[] extensions
#endif
            )
        {
            WebSocket ws = new WebSocket(uri, origin, protocol
#if (!UNITY_WEBGL || UNITY_EDITOR)
                , extensions
#endif
                );
            Register(ws, onOpen, onMessage, onBinary, onClosed, onError, onErrorDescription
#if (!UNITY_WEBGL || UNITY_EDITOR)
                , onIncompleteFrame
#endif
                );
            return ws;
        }

        /// <summary>
        /// Dispose designation webSocket.
        /// <para>释放指定的套接字</para>
        /// </summary>
        public void DisposeDesignation(WebSocket ws)
        {
            if (-1 != _webSocketList.IndexOf(ws))
            {
                Dispose(ws);
                _webSocketList.Remove(ws);
                Count--;
            }
        }

        /// <summary>
        /// Dispose the all webSocket.
        /// <para>释放全部套接字</para>
        /// </summary>
        public void DisposeAll()
        {
            while (Count > 0)
            {
                Dispose(_webSocketList[--Count]);
                _webSocketList.RemoveAt(Count);
            }
        }

        void Register(WebSocket ws, OnWebSocketOpenDelegate onOpen, OnWebSocketMessageDelegate onMessage,
            OnWebSocketBinaryDelegate onBinary, OnWebSocketClosedDelegate onClosed, OnWebSocketErrorDelegate onError,
            OnWebSocketErrorDescriptionDelegate onErrorDescription
#if (!UNITY_WEBGL || UNITY_EDITOR)
            , OnWebSocketIncompleteFrameDelegate onIncompleteFrame
#endif
            )
        {
#if !BESTHTTP_DISABLE_PROXY && !UNITY_WEBGL
            if (HTTPManager.Proxy != null)
                ws.InternalRequest.Proxy = new HTTPProxy(HTTPManager.Proxy.Address, HTTPManager.Proxy.Credentials, false);
#endif

            ws.OnOpen += onOpen;
            ws.OnMessage += onMessage;
            ws.OnBinary += onBinary;
            ws.OnClosed += onClosed;
            ws.OnError += onError;
            ws.OnErrorDesc += onErrorDescription;
#if (!UNITY_WEBGL || UNITY_EDITOR)
            ws.OnIncompleteFrame += onIncompleteFrame;
#endif
            ws.Open();
            Count++;
            _webSocketList.Add(ws);
        }

        void Dispose(WebSocket ws)
        {
            ws.Close();
            ws.OnOpen = null;
            ws.OnMessage = null;
            ws.OnBinary = null;
            ws.OnClosed = null;
            ws.OnError = null;
            ws.OnErrorDesc = null;
#if (!UNITY_WEBGL || UNITY_EDITOR)
            ws.OnIncompleteFrame = null;
#endif
        }
    }
}