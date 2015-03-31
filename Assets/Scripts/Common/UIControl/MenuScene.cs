﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class MenuScene : MonoBehaviour
{
    //全局
    private CardClient cardClient;

    //聊天
    private UITextList chatList;
    private UIScrollBar chatScroll;

    //角色信息
    private PlayerInfo playerInfo;
    private UILabel coinLabel;
    private UILabel gemLabel;
    private UILabel playerNameLabel;
    private UILabel levelLabel;

    //信息面板
    private GameObject infoPanel;

    private void Awake()
    {
        Global.Instance.scene = SceneType.MenuScene;//切换场景变量

        //尝试获取网络端口
        try
        {
            cardClient = GameObject.FindGameObjectWithTag(Tags.Networks).GetComponent<CardClient>();
            SendPlayerInfoRequest();//获取角色信息
        }
        catch (Exception ex) { LogsSystem.Instance.Print(ex.ToString()); }

        //聊天窗初始化
        chatList = GameObject.Find("Chatting/ChattingList").GetComponent<UITextList>();
        if (chatList != null)
        {
            chatScroll = chatList.scrollBar as UIScrollBar;
            chatScroll.alpha = 0.01f;
        }

        //角色信息获取
        coinLabel = GameObject.Find("Economy/Coin/Sprite/Num").GetComponent<UILabel>();
        gemLabel = GameObject.Find("Economy/Gem/Sprite/Num").GetComponent<UILabel>();
        playerNameLabel = GameObject.Find("Head/Name").GetComponent<UILabel>();
        levelLabel = GameObject.Find("Head/Head-bg/Level").GetComponent<UILabel>();

        //信息控件获取
        infoPanel = GameObject.Find("Frame/Background/InfoPanel");
    }

    /// <summary>
    /// 本地输入公共聊天消息提交
    /// </summary>
    public void OnPublicChattingSubmit(UIInput input)
    {
        string text = input.value;
        if (text != null && text != "" && text.Trim() != "")
        {
            LogsSystem.Instance.Print("[用户]" + text);
            AddChatList("我", text);
            if (cardClient != null)
            {
                SocketModel model = new SocketModel();
                model.protocol = SocketProtocol.CHAT;
                ChatDTO data = new ChatDTO(text, Global.Instance.playerName, Global.Instance.UUID);
                model.message = JsonCoding<ChatDTO>.encode(data);

                cardClient.SendMsg(JsonCoding<SocketModel>.encode(model));
            }
        }
        input.value = "";
    }
    /// <summary>
    /// 添加聊天信息到窗口
    /// </summary>
    /// <param name="username">发送者ID</param>
    /// <param name="message">发送的信息</param>
    public void AddChatList(string username, string message)
    {
        if (chatList != null)
        {
            chatList.Add(string.Format("[{0}] {1} : {2}", System.DateTime.Now.ToString("HH:mm:ss"), username, message));
            CheckScrollSize();
        }
    }

    /// <summary>
    /// 检查滚动条大小
    /// 当文字内容超过一个屏幕时显示滚动条
    /// </summary>
    private void CheckScrollSize()
    {
        if (chatScroll.barSize != 1)
        {
            chatScroll.alpha = 1.0f;
            Color temp = chatScroll.foregroundWidget.gameObject.GetComponent<UIButton>().defaultColor;
            temp.a = 1.0f;
            chatScroll.foregroundWidget.gameObject.GetComponent<UIButton>().defaultColor = temp;
        }
    }

    /// <summary>
    /// 获取角色信息
    /// </summary>
    private void SendPlayerInfoRequest()
    {
        SocketModel model = new SocketModel();
        model.protocol = SocketProtocol.PLAYERINFO;
        PlayerInfoDTO data = new PlayerInfoDTO(Global.Instance.UUID);
        model.message = JsonCoding<PlayerInfoDTO>.encode(data);

        cardClient.SendMsg(JsonCoding<SocketModel>.encode(model));
    }

    /// <summary>
    /// 刷新玩家信息
    /// </summary>
    public void UpdatePlayerInfo()
    {
        playerInfo = Global.Instance.playerInfo;//此时该对象有值

        if (playerInfo != null)
        {
            LogsSystem.Instance.Print("玩家数据:" + playerInfo.ToString());
            coinLabel.text = playerInfo.coin.ToString();
            gemLabel.text = playerInfo.gem.ToString();
            playerNameLabel.text = playerInfo.playerName;
            levelLabel.text = playerInfo.level.ToString();
        }
    }


    /// <summary>
    /// 获取玩家拥有的卡片列表
    /// </summary>
    private void GetPlayerCardList()
    {
        SocketModel model = new SocketModel();
        model.protocol = SocketProtocol.CARDINFOLIST;

        cardClient.SendMsg(JsonCoding<SocketModel>.encode(model));
    }

    /// <summary>
    /// 更新玩家拥有的卡片列表
    /// </summary>
    public void UpdatePlayerCardList()
    {
        List<CardInfo> cardList = Global.Instance.playerOwnCard;
        if (cardList == null)
        {
            if (cardList.Count == 0)
            {
                ShortMessagesSystem.Instance.ShowShortMessage("暂时没有拥有任何卡片");
            }
            else
            {
                LogsSystem.Instance.Print("更新卡片列表功能尚未实现", LogLevel.WARN);
            }
        }
    }
}
