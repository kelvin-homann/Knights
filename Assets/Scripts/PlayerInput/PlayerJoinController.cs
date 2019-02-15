using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputManager;
using UnityEngine.UI;

public class PlayerJoinController : MonoBehaviour {

    public SkinnedMeshRenderer[] playerIndicators;
    public Material[] availableMaterials;
    public Material emptySlotMaterial;

    public GameObject readyParent;
    public Text[] ready;
    public Color notReadyColor;
    public GameObject pressStart;
    public GameObject loadingScreen;

    private class JoinedPlayer
    {
        public Player player;
        public int team = 0;
        public bool isReady = false;
        public JoinedPlayer(Player p, int t, bool r) { player = p; team = t; isReady = r; }
    }
    private List<JoinedPlayer> players;

	// Use this for initialization
	void Start () {

        pressStart.SetActive(false);
        readyParent.SetActive(true);
        loadingScreen.SetActive(false);

        foreach (Text t in ready)
        {
            t.color = notReadyColor;
        }

        //Init player list
        players = new List<JoinedPlayer>();
	}
	
	// Update is called once per frame
	void Update ()
    {
        bool allReady = players.Count > 0;

        //Allow joined players to change their team and confirm the selection
        for (int i = 0; i < players.Count; i++)
        {
            ChangeTeams(players[i]);
            ToggleReady(players[i], i);
            if (!players[i].isReady) allReady = false;
        }

        bool canStart = allReady && players.Count > 1;
 
        if (canStart && (Controller.Any.Start.WasPressed || Input.GetKeyDown(KeyCode.Space))) StarGame();

        readyParent.SetActive(!canStart);
        pressStart.SetActive(canStart);

        //Wait for new players to joint if there still are slots
        if (players.Count < playerIndicators.Length)
        {
            WaitForPlayerJoin();
        }
    }

    private void WaitForPlayerJoin()
    {
        //Get the index of the next empty player slot and the corresponding player id
        int currentSlot = players.Count;
        PlayerID currentPlayer = (PlayerID)currentSlot;

        //Check if any controller presses 'A' or 'Start'
        if (Controller.Any.A.WasPressed || Controller.Any.Start.WasPressed)
        {
            //Make sure the player who pressed has not joint already
            foreach(var p in players)
            {
                if (Controller.GetPressed().Player == p.player.Controller.Player) return;
            }

            //Get the player that should fill the slot and assign the controller to him
            Player playerToJoin = Player.GetDefaultPlayer(currentPlayer);
            Player.ChangePlayerController(currentPlayer, Controller.GetPressed());

            //Add the player to the slots and update the indicator
            players.Add(new JoinedPlayer(playerToJoin, currentSlot, false));
            UpdatePlayerIndicators();
        }

    }

    private void ChangeTeams(JoinedPlayer player)
    {
        if (player.isReady) return;

        bool changed = false;

        //If the player has pressed 'Left' or 'Right' change his team and flag as changed
        if (player.player.Controller.Left.WasPressed)
        {
            player.team--;
            changed = true;
        }
        if (player.player.Controller.Right.WasPressed)
        {
            player.team++;
            changed = true;
        }

        //Fix the choosen team index
        if (player.team < 0) player.team += availableMaterials.Length;
        if (player.team >= availableMaterials.Length) player.team -= availableMaterials.Length;

        //Update player indicators
        if (changed) UpdatePlayerIndicators();
    }

    private void ToggleReady(JoinedPlayer player, int index)
    {
        //Dont allow the same team twice
        for (int i = 0; i < players.Count; i++)
        {
            if (player.player == players[i].player) continue;
            if (player.team == players[i].team && players[i].isReady) return;
        }

        //Toggle ready based on player input
        if (player.player.Controller.A.WasPressed) player.isReady = true;
        else if (player.player.Controller.B.WasPressed) player.isReady = false;

        if(player.isReady)
        {
            ready[index].color = availableMaterials[player.team].GetColor("_Color1");
        }
        else
        {
            ready[index].color = notReadyColor;
        }
    }

    private void UpdatePlayerIndicators()
    {
        //Go through all indicators
        for(int i = 0; i < playerIndicators.Length; i++)
        {
            //If the slot is empty, the indicator should use the empty slot material
            if(i >= players.Count)
            {
                playerIndicators[i].sharedMaterial = emptySlotMaterial;
                continue;
            }
            
            //Otherwise assign the material of the choosen team
            playerIndicators[i].sharedMaterial = availableMaterials[players[i].team];
        }
    }

    //Sart the game with the joined players
    private void StarGame()
    {
        Player[] gamePlayers = new Player[players.Count];
        for (int i = 0; i < players.Count; i++)
        {
            gamePlayers[i] = players[i].player;
        }

        loadingScreen.SetActive(true);
        GameManager.Instance.StartGame(gamePlayers);
    }
}
