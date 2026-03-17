using Google.Protobuf;
using Org.Sinkorty.MineSweepServer.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

public static class NetManager
{
    private static Socket socket; // 唯一真神
    private static ByteArray readBuff; // 接收缓冲区
    private static Queue<ByteArray> writeQueue; // 写入队列

    public delegate void EventListener(string err);
    public delegate void MsgListener(MessageWrapper wrapper); // TODO: 有点纠结是用int好还是原封不动MessageWrapper.MessageBodyOnceofCase好

    private static Dictionary<NetEvent, EventListener> eventListeners = new Dictionary<NetEvent, EventListener>();
    private static Dictionary<MessageWrapper.MessageBodyOneofCase, MsgListener> msgListeners = new Dictionary<MessageWrapper.MessageBodyOneofCase, MsgListener>();
    private static bool isConnecting = false;
    private static bool isClosing = false;

    // 消息接收缓冲区
    private static List<MessageWrapper> msgList = new List<MessageWrapper>();
    private static int msgCount = 0; // 消息列表长度

    private readonly static int MAX_MESSAGE_FIRE = 10;

    /// <summary>
    /// 需要担心异步线程，因为订阅的事件一旦触发直接进入回调
    /// </summary>
    public static void AddEventListener(NetEvent netEvent, EventListener listener)
    {
        if (eventListeners.ContainsKey(netEvent)) eventListeners[netEvent] += listener;
        else eventListeners[netEvent] = listener;
    }
    /// <summary>
    /// 无需担心异步线程，因为订阅的事件会在UnityEngine.Update中处理更新
    /// </summary>
    public static void AddMsgListener(MessageWrapper.MessageBodyOneofCase msgType, MsgListener listener)
    {
        if (msgListeners.ContainsKey(msgType)) msgListeners[msgType] += listener;
        else msgListeners[msgType] = listener;
    }
    public static void RemoveEventListener(NetEvent netEvent, EventListener listener)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent] -= listener;
            if (eventListeners[netEvent] == null)
            {
                eventListeners.Remove(netEvent);
            }
        }
    }
    public static void RemoveEventAllListener(NetEvent netEvent)
    {
        if (!eventListeners.ContainsKey(netEvent))
        {
            Debug.LogWarningFormat("[NetManager] Doesn't have any net event listener on {0}", netEvent);
            return;
        }
        eventListeners.Remove(netEvent);
    }
    public static void RemoveMsgListener(MessageWrapper.MessageBodyOneofCase msgType, MsgListener listener)
    {
        if (msgListeners.ContainsKey(msgType))
        {
            msgListeners[msgType] -= listener;
            if (msgListeners[msgType] == null)
            {
                msgListeners.Remove(msgType);
            }
        }
    }
    public static void RemoveMsgAllListener(MessageWrapper.MessageBodyOneofCase msgType)
    {
        if (!msgListeners.ContainsKey(msgType))
        {
            Debug.LogWarningFormat("[NetManager] Doesn't have any msg listener on {0}", msgType);
            return;
        }
        msgListeners.Remove(msgType);
    }
    public static void Connect(string host, int port)
    {
        if (socket != null && socket.Connected)
        {
            Debug.LogError("[NetManager] Connect fail, already connected.");
            return;
        }
        if (isConnecting)
        {
            Debug.LogError("[NetManager] Connect fail, is connecting.");
            return;
        }

        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        readBuff = new ByteArray();
        writeQueue = new Queue<ByteArray>();

        socket.NoDelay = true; // 调参数
        isConnecting = true;
        socket.BeginConnect(host, port, ConnectCallback, socket);
    }
    public static void Close() // TODO: Doesn't remove the msg or event listeners
    {
        FireEvent(NetEvent.BEFORE_CLOSE, "");
        if (socket != null && !socket.Connected)
        {
            Debug.LogError("[NetManager] Socket close fail.");
            return;
        }
        if (isConnecting)
        {
            Debug.LogError("[NetManager] Socket close fail. Still connecting");
            return;
        }
        if (writeQueue.Count > 0)
        {
            isClosing = true;
        }
        else
        {
            socket.Close();
            FireEvent(NetEvent.AFTER_CLOSE, "");
        }
    }
    public static void Send(MessageWrapper msg)
    {
        if (socket == null | !socket.Connected)
        {
            Debug.LogError("[NetManager] Socket send fail. Need to connect.");
            return;
        }
        if (isConnecting)
        {
            Debug.LogError("[NetManager] Socket send fail. Is connecting.");
            return;
        }
        if (isClosing)
        {
            Debug.LogError("[NetManager] Socket send fail. Is closing.");
            return;
        }
        // 拼装，可以优化成符合Protobuf的varint32类型，但是我懒
        byte[] bodyBytes = Encode(msg);

        byte[] lenBytes = new byte[4];

        // 小端序：低位在前，高位在后
        lenBytes[0] = (byte)(bodyBytes.Length & 0xFF);         // 最低位
        lenBytes[1] = (byte)((bodyBytes.Length >> 8) & 0xFF);  // 次低位
        lenBytes[2] = (byte)((bodyBytes.Length >> 16) & 0xFF); // 次高位
        lenBytes[3] = (byte)((bodyBytes.Length >> 24) & 0xFF); // 最高位

        byte[] sendBytes = new byte[bodyBytes.Length + 4];
        Array.Copy(lenBytes, 0, sendBytes, 0, lenBytes.Length);
        Array.Copy(bodyBytes, 0, sendBytes, 4, bodyBytes.Length);

        // Write Out bound!!!
        ByteArray byteArray = new ByteArray(sendBytes);
        int count = 0;
        lock (writeQueue)
        {
            writeQueue.Enqueue(byteArray);
            count = writeQueue.Count;
        }
        if (count == 1)
        {
            socket.BeginSend(sendBytes, 0, sendBytes.Length, 0, SendCallback, socket);
        }
    }
    public static void Update()
    {
        MsgUpdate();
    }

    private static void MsgUpdate()
    {
        if (msgCount == 0) return;
        for (int i = 0; i < MAX_MESSAGE_FIRE; i++)
        {
            MessageWrapper wrapper = null;
            lock (msgList)
            {
                if (msgList.Count > 0)
                {
                    wrapper = msgList[0];
                    msgList.RemoveAt(0);
                    msgCount--;
                }
            }
            if (wrapper != null)
            {
                //Debug.Log("Fire Msg");
                FireMsg(wrapper.MessageBodyCase, wrapper);
            }
            else
            {
                break;
            }
        }
    }

    private static void SendCallback(IAsyncResult ar)
    {
        Socket socket = (Socket)ar.AsyncState;
        if (socket == null || !socket.Connected) // 大清亡了
        {
            Debug.LogWarning("[NetManager] Socket Send fail. Already been closed.");
            return;
        }
        int count = socket.EndSend(ar);
        // 获取入队列第一条数据
        ByteArray byteArray;
        lock (writeQueue)
        {
            byteArray = writeQueue.First();
        }
        // 完整发送
        byteArray.readIdx += count;
        if (byteArray.length == 0)
        {
            lock (writeQueue)
            {
                writeQueue.Dequeue();
                byteArray = writeQueue.First();
            }
        }
        // 继续发
        if (byteArray != null)
        {
            socket.BeginSend(byteArray.bytes, byteArray.readIdx, byteArray.length, 0, SendCallback, socket);
        }
        else if (isClosing)
        {
            socket.Close();
        }
    }
    private static void ConnectCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            socket.EndConnect(ar);
            Debug.Log("[NetManager] Socket connect succ.");
            isConnecting = false;
            FireEvent(NetEvent.CONNECT_SUCC, "");

            // 连接建立，开始监听消息
            socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
        }
        catch (SocketException ex)
        {
            Debug.LogError("[NetManager] Socket connect fail " + ex.Message);
            FireEvent(NetEvent.CONNECT_FAIL, ex.ToString());
            isConnecting = false;
        }
    }
    private static void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = (Socket)ar.AsyncState;
            int count = socket.EndReceive(ar);
            if (count == 0) // 接受长度为零代表 连接终止
            {
                Close();
                return;
            }
            readBuff.writeIdx += count;

            HandleFrame();

            if (readBuff.remain < 8)
            {
                readBuff.MoveBytes();
                readBuff.ReSize(readBuff.length * 2);
            }
            socket.BeginReceive(readBuff.bytes, readBuff.writeIdx, readBuff.remain, 0, ReceiveCallback, socket);
        }
        catch (SocketException ex)
        {
            Debug.LogError("[NetManager] Socket receive fail. " + ex.Message);
        }
    }
    private static void HandleFrame()
    {
        if (readBuff.length <= 4) return;
        // 小端读取消息长度
        int readIdx = readBuff.readIdx;
        byte[] bytes = readBuff.bytes;
        int bodyLength = bytes[readIdx] | (bytes[readIdx + 1] << 8) | (bytes[readIdx + 2] << 16) | (bytes[readIdx + 3] << 24);

        if (readBuff.length < bodyLength + 4) return;
        readBuff.readIdx += 4;

        // 解析协议：
        MessageWrapper wrapper = Decode(bytes, readBuff.readIdx, bodyLength);

        readBuff.readIdx += bodyLength;
        readBuff.CheckAndMoveBytes();
        // 添加到消息队列
        lock (msgList)
        {
            msgList.Add(wrapper);
        }
        msgCount++;
        if (readBuff.length > 4)
        {
            HandleFrame();
        }
    }
    private static void FireEvent(NetEvent netEvent, string err)
    {
        if (eventListeners.ContainsKey(netEvent))
        {
            eventListeners[netEvent](err);
        }
    }
    private static void FireMsg(MessageWrapper.MessageBodyOneofCase msgType, MessageWrapper wrapper)
    {
        if (msgListeners.ContainsKey(msgType))
        {
            msgListeners[msgType](wrapper);
        }
    }

    private static byte[] Encode(MessageWrapper wrapper)
    {
        //Decode(wrapper.ToByteArray(),0, wrapper.ToByteArray().Length);
        return wrapper.ToByteArray();
    }
    private static MessageWrapper Decode(byte[] data, int offset, int count)
    {
        return MessageWrapper.Parser.ParseFrom(data, offset, count);
    }
}
