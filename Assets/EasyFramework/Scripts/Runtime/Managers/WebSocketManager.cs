/* 
 * ================================================
 * Describe:      This script is used to control the websocket managers.     Thanks to the author: psygames, can join his QQ group (1126457634) get the latest version.
 * Describe:      Related website: https://websockets.spec.whatwg.org/
 * Author:        psygames
 * CreationTime:  2016-06-25 00:00:00
 * ModifyAuthor:  Xiaohei.Wang(Wenhao)
 * ModifyTime:    2024-02-07 17:21:26
 * Version:       0.1 
 * ===============================================
 */
using System;
using System.Collections.Generic;
using EasyFramework.UnityWebSocket;

namespace EasyFramework.Managers
{
#if UNITY_EDITOR || !UNITY_WEBGL
    public class WebSocketManager : Singleton<WebSocketManager>, IManager, IUpdate
    {
        int IManager.ManagerLevel => EF.Projects.AppConst.ManagerLevels.IndexOf("WebSocketManager");

        int m_SocketIndex = -1;
        Dictionary<int, WebSocket> m_SocketDic;


        void ISingleton.Init()
        {
            m_SocketDic = new Dictionary<int, WebSocket>();
        }

        public void Update(float elapse, float realElapse)
        {
            foreach (var ws in m_SocketDic.Values)
                ws.Update();
        }

        void ISingleton.Quit()
        {
            DisposeAll();
            m_SocketDic = null;
        }

        bool CheckIDExistsInDic(int wsId)
        {
            if (!m_SocketDic.ContainsKey(wsId))
            {
                D.Error("Current id is not  find in distionary!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 创建一个连接
        /// </summary>
        /// <param name="address">远端地址</param>
        /// <param name="subProtocol">子协议地址</param>
        /// <param name="onOpen">Called when the connection to the WebSocket server is established. <para>当与WebSocket服务器的连接建立时调用</para></param>
        /// <param name="onMessage">Called when a new textual message is received from the server. <para>当从服务器接收到新的文本消息时调用</para></param>
        /// <param name="onClosed">Called when the WebSocket connection is closed. <para>当WebSocket连接关闭时调用</para></param>
        /// <param name="onError">Called when an error is encountered. The Exception parameter may be null. <para>遇到错误时调用。Exception参数可能为空</para></param>
        /// <returns>返回当前连接ID，后续函数调用时需传入</returns>
        public int CreateOne(string address, string[] subProtocol = null,
            Action<ConnectEventArgs> onOpen = null,
            Action<DisconnectEventArgs> onClosed = null,
            Action<ErrorEventArgs> onError = null,
            Action<MessageEventArgs> onMessage = null
            )
        {
            WebSocket _ws;
            if (null == subProtocol)
                _ws = new WebSocket(address);
            else
                _ws = new WebSocket(address, subProtocol);
            _ws.OnOpen += onOpen;
            _ws.OnClose += onClosed;
            _ws.OnError += onError;
            _ws.OnMessage += onMessage;
            m_SocketDic[++m_SocketIndex] = _ws;
            return m_SocketIndex;
        }

        /// <summary>
        /// 连接
        /// </summary>
        public void Connect(int wsId = 0)
        {
            if(CheckIDExistsInDic(wsId))
                m_SocketDic[wsId].ConnectAsync();
        }

        /// <summary>
        /// 启动全部连接
        /// </summary>
        public void ConnectAll()
        {
            foreach (var ws in m_SocketDic.Values)
                ws.ConnectAsync();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect(int wsId = 0)
        {
            if (CheckIDExistsInDic(wsId))
                m_SocketDic[wsId].CloseAsync();
        }

        /// <summary>
        /// 断开全部连接
        /// </summary>
        public void DisconnectAll()
        {
            foreach (var ws in m_SocketDic.Values)
                ws.CloseAsync();
        }

        /// <summary>
        /// 释放连接
        /// </summary>
        public void Dispose(int wsId = 0)
        {
            if (CheckIDExistsInDic(wsId))
            {
                m_SocketDic[wsId].CloseAsync();
                m_SocketDic.Remove(wsId);
            }
        }

        /// <summary>
        /// Dispose the all websocket. <para>销毁全部连接</para>
        /// </summary>
        public void DisposeAll()
        {
            int _index = -1;
            int[] _indexArray = new int[m_SocketDic.Count];

            foreach (var id in m_SocketDic.Keys)
            {
                _indexArray[++_index] = id;
            }

            for (int i = 0; i <= _index; i++)
            {
                m_SocketDic[_indexArray[i]].CloseAsync();
                m_SocketDic.Remove(_indexArray[i]);
            }
            m_SocketDic.Clear();
        }

        /// <summary>
        /// 获取连接地址
        /// </summary>
        public string GetAddress(int wsId = 0) 
        {
            if (CheckIDExistsInDic(wsId))
                return m_SocketDic[wsId].Address; 
            else return null;
        }

        /// <summary>
        /// 获取子协议地址
        /// </summary>
        public string[] GetSubProtocols(int wsId = 0)
        {
            if (CheckIDExistsInDic(wsId))
                return m_SocketDic[wsId].SubProtocols;
            else return null;
        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        public WebSocketState GetReadyState(int wsId = 0)
        {
            if (CheckIDExistsInDic(wsId))
                return m_SocketDic[wsId].ReadyState;
            else
                return WebSocketState.Inexistence;
        }

        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="msg">信息内容</param>
        /// <param name="wsId">连接ID</param>
        public void Send(string msg, int wsId = 0)
        {
            if (CheckIDExistsInDic(wsId))
                m_SocketDic[wsId].SendAsync(msg);
        }

        /// <summary>
        /// 发送信息
        /// </summary>
        /// <param name="msg">信息内容</param>
        /// <param name="wsId">连接ID</param>
        public void Send(byte[] msg, int wsId = 0)
        {
            if (CheckIDExistsInDic(wsId))
                m_SocketDic[wsId].SendAsync(msg);
        }
    }

 #elif !UNITY_EDITOR && UNITY_WEBGL
    
    using System.Runtime.InteropServices;
    using AOT;

    /// <summary>
    /// Class providing static access methods to work with JSLIB WebSocket
    /// <para>提供与JSLIB WebSocket一起工作的静态访问方法</para>
    /// </summary>
    internal static class WebSocketManager
    {
        /* Map of websocket instances */
        private static Dictionary<int, WebSocket> sockets = new Dictionary<int, WebSocket>();

        /* Delegates */
        public delegate void OnOpenCallback(int instanceId);
        public delegate void OnMessageCallback(int instanceId, IntPtr msgPtr, int msgSize);
        public delegate void OnMessageStrCallback(int instanceId, IntPtr msgStrPtr);
        public delegate void OnErrorCallback(int instanceId, IntPtr errorPtr);
        public delegate void OnCloseCallback(int instanceId, int closeCode, IntPtr reasonPtr);

        /* WebSocket JSLIB functions */
        [DllImport("__Internal")]
        public static extern int WebSocketConnect(int instanceId);

        [DllImport("__Internal")]
        public static extern int WebSocketClose(int instanceId, int code, string reason);

        [DllImport("__Internal")]
        public static extern int WebSocketSend(int instanceId, byte[] dataPtr, int dataLength);

        [DllImport("__Internal")]
        public static extern int WebSocketSendStr(int instanceId, string data);

        [DllImport("__Internal")]
        public static extern int WebSocketGetState(int instanceId);

        /* WebSocket JSLIB callback setters and other functions */
        [DllImport("__Internal")]
        public static extern int WebSocketAllocate(string url, string binaryType);

        [DllImport("__Internal")]
        public static extern int WebSocketAddSubProtocol(int instanceId, string protocol);

        [DllImport("__Internal")]
        public static extern void WebSocketFree(int instanceId);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnOpen(OnOpenCallback callback);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnMessage(OnMessageCallback callback);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnMessageStr(OnMessageStrCallback callback);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnError(OnErrorCallback callback);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnClose(OnCloseCallback callback);

        /* If callbacks was initialized and set */
        private static bool isInitialized = false;

        /* Initialize WebSocket callbacks to JSLIB */
        private static void Initialize()
        {
            WebSocketSetOnOpen(DelegateOnOpenEvent);
            WebSocketSetOnMessage(DelegateOnMessageEvent);
            WebSocketSetOnMessageStr(DelegateOnMessageStrEvent);
            WebSocketSetOnError(DelegateOnErrorEvent);
            WebSocketSetOnClose(DelegateOnCloseEvent);

            isInitialized = true;
        }

        [MonoPInvokeCallback(typeof(OnOpenCallback))]
        public static void DelegateOnOpenEvent(int instanceId)
        {
            if (sockets.TryGetValue(instanceId, out var socket))
            {
                socket.HandleOnOpen();
            }
        }

        [MonoPInvokeCallback(typeof(OnMessageCallback))]
        public static void DelegateOnMessageEvent(int instanceId, IntPtr msgPtr, int msgSize)
        {
            if (sockets.TryGetValue(instanceId, out var socket))
            {
                var bytes = new byte[msgSize];
                Marshal.Copy(msgPtr, bytes, 0, msgSize);
                socket.HandleOnMessage(bytes);
            }
        }

        [MonoPInvokeCallback(typeof(OnMessageCallback))]
        public static void DelegateOnMessageStrEvent(int instanceId, IntPtr msgStrPtr)
        {
            if (sockets.TryGetValue(instanceId, out var socket))
            {
                string msgStr = Marshal.PtrToStringAuto(msgStrPtr);
                socket.HandleOnMessageStr(msgStr);
            }
        }

        [MonoPInvokeCallback(typeof(OnErrorCallback))]
        public static void DelegateOnErrorEvent(int instanceId, IntPtr errorPtr)
        {
            if (sockets.TryGetValue(instanceId, out var socket))
            {
                string errorMsg = Marshal.PtrToStringAuto(errorPtr);
                socket.HandleOnError(errorMsg);
            }
        }

        [MonoPInvokeCallback(typeof(OnCloseCallback))]
        public static void DelegateOnCloseEvent(int instanceId, int closeCode, IntPtr reasonPtr)
        {
            if (sockets.TryGetValue(instanceId, out var socket))
            {
                string reason = Marshal.PtrToStringAuto(reasonPtr);
                socket.HandleOnClose((ushort)closeCode, reason);
            }
        }

        internal static int AllocateInstance(string address, string binaryType)
        {
            if (!isInitialized) Initialize();
            return WebSocketAllocate(address, binaryType);
        }

        internal static void Add(WebSocket socket)
        {
            if (!sockets.ContainsKey(socket.instanceId))
            {
                sockets.Add(socket.instanceId, socket);
            }
        }

        internal static void Remove(int instanceId)
        {
            if (sockets.ContainsKey(instanceId))
            {
                sockets.Remove(instanceId);
            }
        }
    }
#endif
}
