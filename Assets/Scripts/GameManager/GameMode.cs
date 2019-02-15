using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMode  {

    private Game currentGame;

    public void StartGame(Game game)
    {
        currentGame = game;
    }

    public bool CheckWinningConditionForPlayer(Player player)
    {
        if (player.IsDead) return false;

        bool playersAlive = false;
        foreach(Player p in currentGame.Players)
        {
            if (p == player) continue;
            if (p.IsAlive) playersAlive = true;
        }

        return !playersAlive;
    }
	
}
