﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleInteractionController : PlayerInteractionController {

    public enum Action { DefendTarget, AddOffense, AttackTarget, AddDefense }

    public Transform attackTargetMarker;
    public Transform defendTargetMarker;

    public Text healthText;
    public Text defenseText;
    public Text offenseText;
    public Image defenseBar;
    public Image offenseBar;
	
	// Update is called once per frame
	protected override void Update () {
        base.Update();

        //shortcuts.Add((int)Action.SpawnKnight, player.Controller.Y);
        //shortcuts.Add((int)Action.SpawnArcher, player.Controller.X);
        //shortcuts.Add((int)Action.SpawnHeavy, player.Controller.B);
        //shortcuts.Add((int)Action.SetHarvest, player.Controller.A);

        //Update bars
        defenseBar.enabled = player.TotalUnits > 0;
        offenseBar.enabled = player.TotalUnits > 0;
        if (player.TotalUnits > 0)
        {
            defenseBar.fillAmount = (float)player.DefenseSize / player.TotalUnits;
            offenseBar.fillAmount = (float)player.OffenseSize / player.TotalUnits;
        }

        //Update text
        healthText.text = player.Health.ToString();
        defenseText.text = player.DefenseSize.ToString();
        offenseText.text = player.OffenseSize.ToString();
    }

    protected override void ActionA()
    {
        //attackTargetMarker.position = player.WorldCursor.position;
        SetMarkerPosition(attackTargetMarker, player.WorldCursor.position);
    }

    protected override void ActionB()
    {
        Debug.Log("Add Offense");
        player.DefenseSize--;
        player.OffenseSize++;
    }

    protected override void ActionX()
    {
        Debug.Log("Add Defense");
        player.DefenseSize++;
        player.OffenseSize--;
    }

    protected override void ActionY()
    {
        //defendTargetMarker.position = player.WorldCursor.position;
        SetMarkerPosition(defendTargetMarker, player.WorldCursor.position);
    }

}
