﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Global
{
    #region 单例模式
    private static Global _instance;
    public static Global Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Global();
            }

            return _instance;
        }
    }
    #endregion
    public SceneType scene;
    public string serverName;
    public string UUID;
    public string playerName;
    public PlayerInfo playerInfo;
    public List<CardInfo> playerOwnCard;

    public Global()
    {

    }
}

public enum SceneType
{
    LoginScene, SkipScene, MenuScene, GameScene
}