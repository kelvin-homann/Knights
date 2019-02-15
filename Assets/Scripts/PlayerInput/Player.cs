using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player
{
    private static Player playerOne;
    public static Player PlayerOne { get { return GetDefaultPlayer(ref playerOne, PlayerID.One); } }
    private static Player playerTwo;
    public static Player PlayerTwo { get { return GetDefaultPlayer(ref playerTwo, PlayerID.Two); } }
    private static Player playerThree;
    public static Player PlayerThree { get { return GetDefaultPlayer(ref playerThree, PlayerID.Three); } }
    private static Player playerFour;
    public static Player PlayerFour { get { return GetDefaultPlayer(ref playerFour, PlayerID.Four); } }

    public PlayerID ID { get; private set; }
    public int Index { get { return (int)ID; } }
    
    public InputManager.Controller Controller { get; private set; }

    public bool IsPlaying { get; set; }

    private int crystals = 500;
    public int Crystals { get { return crystals; }
        set {
            crystals = Mathf.Max(0, value);
        }
    }

    private float pollInterval = 0.5f;
    private float lastPollTime;
    private int offensePoll, defensePoll;
    public int OffenseSize { get { CheckPoll(); return offensePoll; } }
    public int DefenseSize { get { CheckPoll(); return defensePoll; } }
    public int TotalUnits { get { return OffenseSize + DefenseSize; } }

    private int healthPoll;
    public int Health { get { CheckPoll(); return healthPoll; } }
    public bool IsDead { get { return Health <= 0; } }
    public bool IsAlive { get { return !IsDead; } }

    public int Kingdom { get; set; }

    public Transform WorldCursor { get; set; }

    private void CheckPoll()
    {
        if (Time.time - lastPollTime > pollInterval) PollValues();
    }

    public void PollValues()
    {
        if (BattleManager.Instance == null) return;
        defensePoll = BattleManager.Instance.GetCharactersCount(Kingdom, EDeploymentType.Defense);
        offensePoll = BattleManager.Instance.GetCharactersCount(Kingdom, EDeploymentType.Attack);
        healthPoll = Mathf.RoundToInt(BattleManager.Instance.GetTotalStructureHealthPointsRelative(Kingdom) * 100);
        lastPollTime = Time.time;
    }

    public Player(PlayerID id) 
    {
        ID = id;
        crystals = 500;
        IsPlaying = false;
    }

    public override bool Equals(object obj)
    {
        return ((Player)obj).ID == this.ID;
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }

    private static Player GetDefaultPlayer(ref Player player, PlayerID id)
    {
        if (player == null)
        {
            player = new Player(id);
            switch(id)
            {
                case PlayerID.One: player.Controller = InputManager.Controller.One; break;
                case PlayerID.Two: player.Controller = InputManager.Controller.Two; break;
                case PlayerID.Three: player.Controller = InputManager.Controller.Three; break;
                case PlayerID.Four: player.Controller = InputManager.Controller.Four; break;
            }
            player.Kingdom = (int)id;
        }
        return player;
    }

    public static Player GetDefaultPlayer(PlayerID id)
    {
        switch (id)
        {
            case PlayerID.One: return PlayerOne;
            case PlayerID.Two: return PlayerTwo;
            case PlayerID.Three: return PlayerThree;
            case PlayerID.Four: return PlayerFour;
        }
        return null;
    }

    public static void ChangePlayerController(PlayerID player, InputManager.Controller controller)
    {
        GetDefaultPlayer(player).Controller = controller;
    }

    public static int ActivePlayerCount { get
        {
            int count = 0;
            if (PlayerOne.IsPlaying) count++;
            if (PlayerTwo.IsPlaying) count++;
            if (PlayerThree.IsPlaying) count++;
            if (PlayerFour.IsPlaying) count++;
            return count;
        } }
}

public enum PlayerID
{
    One = 0, Two = 1, Three = 2, Four = 3
}
