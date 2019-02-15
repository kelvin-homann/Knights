using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    public string mapName = "Map01";

    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if(instance == null)
            {
                GameObject go = new GameObject("Game Manager", typeof(GameManager));
                instance = go.GetComponent<GameManager>();
            }
            return instance;
        }
    }

    public Game Game { get; private set; }
    public bool IsGameOver { get { return Game != null && Game.CheckWinningCondition(); } }
    public bool IsGameRunning { get { return Game != null && Game.IsRunning; } }

    public delegate void GameOverFunc(Player winner);
    public event GameOverFunc onGameOver;

	void Awake () {
        //Handle singelton / Dont destroy on load
        if (instance == null) instance = this;
        if(instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
	}

    void Update () {
        //Check if game has been initialized
        if (Game == null) return;

		//Check if the game is over
        if(IsGameRunning && IsGameOver)
        {
            Game.End();
            if (onGameOver != null) onGameOver(Game.Winner);
        }
	}

    //Start a new game with new gamemode for given players
    public void StartGame(params Player[] players)
    {
        Game = new Game(new GameMode(), players);
        Game.Start();
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(mapName);
    }
}
