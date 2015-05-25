﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using UnityEngine;

public class GameClient
{
    #region 单例模式
    private static GameClient _instance;
    public static GameClient Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameClient();
            }
            return _instance;
        }
    }
    #endregion
    public const int gamePort = 28283;
    public TcpClient gameClient;
    public Encoding encoding;//编码格式

    private Dictionary<GameData, Socket> gameDataMessageList;//缓存游戏数据信息,等待主线程调用

    public GameClient()
    {
        gameClient = new TcpClient();
        encoding = Encoding.UTF8;
        gameDataMessageList = new Dictionary<GameData, Socket>();
    }

    /// <summary>
    /// 连接游戏服务器
    /// </summary>
    /// <param name="hostname">服务器ip地址</param>
    public void ConnectGameServer(string hostname)
    {
        try
        {
            LogsSystem.Instance.Print(string.Format("正在尝试连接到远程服务器{0}", hostname, gamePort));
            gameClient.Connect(hostname, gamePort);//连接服务器

            //开始接受数据
            LogsSystem.Instance.Print("连接成功！开始异步接受数据");
            Receive(gameClient.Client);
        }
        catch (Exception ex)
        {
            LogsSystem.Instance.Print("无法创建TCP连接" + ex, LogLevel.ERROR);
        }
    }

    /// <summary>
    /// 关闭连接
    /// </summary>
    public void CloseTcpConnect()
    {
        if (gameClient.Client != null && gameClient.Connected)
        {
            //如果tcp已连接
            gameClient.Close();//关闭连接
        }
    }
    /// <summary>
    /// 延时关闭TCP连接的方法
    /// </summary>
    public IEnumerator DelayCloseTcpConnectCoroutine(float time)
    {
        yield return new WaitForSeconds(time);

        CloseTcpConnect();
    }




    #region 发送数据
    /// <summary>
    /// 发送到已经连接的
    /// </summary>
    /// <param name="data"></param>
    public void SendToServer(GameData data)
    {
        if (this.gameClient != null)
        {
            Send(this.gameClient.Client, data);
        }
        else
        {
            LogsSystem.Instance.Print("未连接到游戏服务器", LogLevel.WARN);
        }
    }
    public void Send(Socket socket, GameData data)
    {
        string sendMessage = JsonCoding<GameData>.encode(data);
        byte[] sendBytes = encoding.GetBytes(sendMessage);
        Send(socket, sendBytes);
    }
    private void Send(Socket socket, byte[] data)
    {
        LogsSystem.Instance.Print(string.Format("发送数据({0}):{1}", data.Length, encoding.GetString(data)));
        socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), socket);
    }
    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket handler = (Socket)ar.AsyncState;

            int bytesSent = handler.EndSend(ar);
            LogsSystem.Instance.Print(string.Format("数据({0})发送完成", bytesSent));
        }
        catch (Exception e)
        {
            LogsSystem.Instance.Print(e.ToString(), LogLevel.ERROR);
        }
    }
    #endregion

    #region 接受数据
    private void Receive(Socket client)
    {
        try
        {
            StateObject state = new StateObject();
            state.socket = client;
            client.BeginReceive(state.buffer, 0, StateObject.buffSize, 0, new AsyncCallback(ReceiveCallback), state);
        }
        catch (Exception e)
        {
            LogsSystem.Instance.Print(e.ToString(), LogLevel.ERROR);
        }
    }
    private void ReceiveCallback(IAsyncResult ar)
    {
        try
        {
            StateObject receiveState = (StateObject)ar.AsyncState;
            Socket client = receiveState.socket;

            int bytesRead = client.EndReceive(ar);
            if (bytesRead < StateObject.buffSize && bytesRead > 0)
            {
                //如果读取到数据长度较小
                foreach (byte b in receiveState.buffer)
                {
                    if (b != 0x00)
                    {
                        //将缓存加入结果列
                        receiveState.dataByte.Add(b);
                    }
                }
                receiveState.buffer = new byte[StateObject.buffSize];//清空缓存

                //接受完成
                byte[] receiveData = receiveState.dataByte.ToArray();
                LogsSystem.Instance.Print(string.Format("读取到{1}长度数据,接受到{0}字节数据", receiveData.Length, bytesRead));
                //处理数据
                ProcessReceiveMessage(receiveData, receiveState.socket);

                Receive(client);//继续下一轮的接受
            }
            else if (bytesRead >= StateObject.buffSize)
            {
                //如果读取到数据长度大于缓冲区
                receiveState.dataByte.AddRange(receiveState.buffer);//将缓存加入结果列
                receiveState.buffer = new byte[StateObject.buffSize];//清空缓存
                client.BeginReceive(receiveState.buffer, 0, StateObject.buffSize, 0, new AsyncCallback(ReceiveCallback), receiveState);//继续接受下一份数据包
            }
        }
        catch (Exception e)
        {
            LogsSystem.Instance.Print(e.ToString(), LogLevel.WARN);
        }
    }

    /// <summary>
    /// 处理数据
    /// </summary>
    private void ProcessReceiveMessage(byte[] receiveBytes, Socket socket)
    {
        try
        {
            string receiveMessage = encoding.GetString(receiveBytes);
            LogsSystem.Instance.Print(string.Format("[TCP FROM{0}]:{1}", socket.RemoteEndPoint, receiveMessage));//日志记录
            GameData receiveData = JsonCoding<GameData>.decode(receiveMessage);
            if (receiveData != null)
            {
                this.gameDataMessageList.Add(receiveData, socket);//将得到的数据缓存到内存等待主线程读取
            }
        }
        catch (Exception ex)
        {
            LogsSystem.Instance.Print(ex.ToString(), LogLevel.ERROR);
        }
    }
    #endregion

    public Dictionary<GameData, Socket> GetGameDataList()
    { return this.gameDataMessageList; }
    public GameScene GetGameSceneManager()
    {
        if (Global.Instance.activedSceneManager is GameScene)
        {
            return (Global.Instance.activedSceneManager as GameScene);
        }
        else
        {
            return null;
        }
    }

    class StateObject
    {
        //socket 客户端
        public Socket socket = null;
        //缓冲区大小
        public const int buffSize = 256;
        //缓冲
        public byte[] buffer = new byte[buffSize];
        //数据流
        public List<byte> dataByte = new List<byte>();
    }
}
