using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputManager;

public class PlayerController : MonoBehaviour {

    public PlayerID playerID;
    public CastleInteractionController castleMenu;
    public BattleInteractionController battleMenu;
    public float infoFlashTime;
    public KingdomMaterialContainer uiMaterials;

    private Player player;
    private Controller Controller { get { return player.Controller; } }

    private IButton castleMenuButton, battleMenuButton;
    
    public float MoveX
    {
        get { return CanMove ? Controller.LeftStick.X : 0; }
    }

    public float MoveY
    {
        get { return CanMove ? Controller.LeftStick.Y : 0; }
    }

    public bool CanMove { get { return !castleMenu.IsVisible && !battleMenu.IsVisible; } }


    private void Start()
    {
        //Get the default player for the player id
        player = Player.GetDefaultPlayer(playerID);
        player.WorldCursor = transform;

        //Assign player to menus and init
        castleMenu.AssignPlayer(player);
        battleMenu.AssignPlayer(player);
        castleMenu.Init();
        battleMenu.Init();

        //Set buttons
        castleMenuButton = new Axis.Button(Controller.LeftTrigger);
        battleMenuButton = new Axis.Button(Controller.RightTrigger);

        //Set cursor material
        GetComponent<MeshRenderer>().sharedMaterial = uiMaterials.GetMaterial(player.Kingdom);

        //Move to the spawnpoint of the kingdom
        Vector3 spawnPoint = KingdomPlayerSpawnpoint.GetPoint(player.Kingdom);
        transform.position = new Vector3(spawnPoint.x, transform.position.y, spawnPoint.z);
    }

    private void OnEnable()
    {
        //Register events
        castleMenu.onActionPerformed += OnMenuActionPerformed;
        battleMenu.onActionPerformed += OnMenuActionPerformed;
    }

    private void OnDisable()
    {
        //Unregister events
        castleMenu.onActionPerformed -= OnMenuActionPerformed;
        battleMenu.onActionPerformed -= OnMenuActionPerformed;
    }

    // Update is called once per frame
    void Update () {
        //Open menus
        if (castleMenuButton.IsHeld && !castleMenu.IsVisible && !battleMenu.IsVisible)
        {
            castleMenu.Show();
            battleMenu.CancelFlashInfo();
        }
        else if (!castleMenuButton.IsHeld) castleMenu.Hide();

        if (battleMenuButton.IsHeld && !castleMenu.IsVisible && !battleMenu.IsVisible)
        {
            battleMenu.Show();
            castleMenu.CancelFlashInfo();
        }
        else if (!battleMenuButton.IsHeld) battleMenu.Hide();

        //Shortcuts
        castleMenu.ShortcutsEnabled = !battleMenu.IsVisible;
        battleMenu.ShortcutsEnabled = !castleMenu.IsVisible;

        //HACK
        if (player.ID == PlayerID.One)
            DisplaySettings.renderHealthBars = Controller.Any.Back.IsHeld;
    }

    public void Disable()
    {
        //Disable the ui for this player
        castleMenu.transform.parent.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnMenuActionPerformed(PlayerInteractionController sender, int index, bool shortcut)
    {
        //Flash info on menu if a non target-setting action was performed
        if(sender == castleMenu)
        {
            var action = (CastleInteractionController.Action)index;
            if(action != CastleInteractionController.Action.SetHarvest && shortcut)
            {
                castleMenu.FlashInfo(infoFlashTime);
                battleMenu.CancelFlashInfo();
            }
        }
        else if (sender == battleMenu)
        {
            var action = (BattleInteractionController.Action)index;
            if (action != BattleInteractionController.Action.AttackTarget && 
                action != BattleInteractionController.Action.DefendTarget && 
                shortcut)
            {
                battleMenu.FlashInfo(infoFlashTime);
                castleMenu.CancelFlashInfo();
            }
        }
    }
}
