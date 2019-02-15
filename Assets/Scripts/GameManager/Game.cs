using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game {

    public Player[] Players { get; private set; }
    public Player Winner { get; private set; }
    public GameMode GameMode { get; private set; }
    public bool IsRunning { get; private set; }

    public Game(GameMode mode, params Player[] players)
    {
        Players = players;
        GameMode = mode;
    }

    public bool CheckWinningCondition()
    {
        if (!IsRunning) return false;

        foreach(var p in Players)
        {
            if (GameMode.CheckWinningConditionForPlayer(p))
            {
                Winner = p;
                return true;
            }
        }
        return false;
    }

    public void Start()
    {
        GameMode.StartGame(this);
        IsRunning = true;
    }

    public void End()
    {
        IsRunning = false;
    }
	
}
