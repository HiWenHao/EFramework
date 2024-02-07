using System;
using System.Collections.Generic;
using UnityEngine;
using UnityWebSocket;

public class HttpTest : MonoBehaviour
{
    int m_SocketIndex = -1;
    Dictionary<int, WebSocket> m_SocketDic;

    void Start()
    {
        m_SocketDic = new Dictionary<int, WebSocket>();
    }

    void OnDestroy()
    {
        m_SocketDic.Clear();
        m_SocketDic = null;
    }

    /// <summary>
    /// 创建一个连接
    /// </summary>
    /// <param name="address">远端地址</param>
    /// <param name="subProtocol"></param>
    /// <param name="onOpen"></param>
    /// <param name="onClose"></param>
    /// <param name="onError"></param>
    /// <param name="onMessage"></param>
    /// <returns>返回当前连接ID，后续函数调用时需传入</returns>
    public int CreateOne(string address, string subProtocol = null,
        EventHandler<OpenEventArgs> onOpen = null,
        EventHandler<CloseEventArgs> onClose = null,
        EventHandler<ErrorEventArgs> onError = null,
        EventHandler<MessageEventArgs> onMessage = null
        )
    {
        WebSocket _ws;
        if (string.IsNullOrEmpty(subProtocol))
            _ws = new WebSocket(address);
        else
            _ws = new WebSocket(address, subProtocol);
        _ws.OnOpen += onOpen;
        _ws.OnClose += onClose;
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
        m_SocketDic[wsId].ConnectAsync();
    }

    /// <summary>
    /// 断开链接
    /// </summary>
    public void Disconnect(int wsId = 0)
    {
        m_SocketDic[wsId].CloseAsync();
    }

    /// <summary>
    /// 释放链接
    /// </summary>
    public void Dispose(int wsId = 0)
    {
        m_SocketDic.Remove(wsId);
    }

    /// <summary>
    /// 获取链接地址
    /// </summary>
    public string GetAddress(int wsId = 0) => m_SocketDic[wsId].Address;

    /// <summary>
    /// 获取子协议地址
    /// </summary>
    public string[] GetSubProtocols(int wsId = 0) => m_SocketDic[wsId].SubProtocols;

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public WebSocketState GetReadyState(int wsId = 0) => m_SocketDic[wsId].ReadyState;

    /// <summary>
    /// 发送信息
    /// </summary>
    /// <param name="msg">信息内容</param>
    /// <param name="wsId">连接ID</param>
    public void Send(string msg, int wsId = 0)
    {
        m_SocketDic[wsId].SendAsync(msg);
    }
    /// <summary>
    /// 发送信息
    /// </summary>
    /// <param name="msg">信息内容</param>
    /// <param name="wsId">连接ID</param>
    public void Send(byte[] msg, int wsId = 0)
    {
        m_SocketDic[wsId].SendAsync(msg);
    }
}
