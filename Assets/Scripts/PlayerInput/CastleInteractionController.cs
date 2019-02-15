using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//TODO: Don't keep UI and queue logic in the same class
public class CastleInteractionController : PlayerInteractionController {

    private static readonly float[] QUEUE_TIMES = {10f, 5f, 15f};
    private static readonly int[] CHARACTER_COSTS = { 12, 10, 20 };

    public enum Action { SpawnKnight, SpawnHeavy, SetHarvest, SpawnArcher }
    private enum SlotContent { None = -1, Archer = 0, Knight = 1, Heavy = 2}

    public Transform harvestTargetMarker;

    public Image progressBar;
    public Text crystalCounter;
    public Image[] queueSlotImages;
    public Sprite[] queueIconsActive;
    public Sprite[] queueIconsQueued;

    private Image[] inactiveQueueSlotImages;
    private SlotContent[] queueSlots;
    private SlotContent ActiveSlot { get { return queueSlots[0]; } }

    private float currentQueueTime;
    private float queueTimer;

	// Use this for initialization
	public override void Init () {
        shortcuts.Add((int)Action.SpawnKnight, player.Controller.Y);
        shortcuts.Add((int)Action.SpawnArcher, player.Controller.X);
        shortcuts.Add((int)Action.SpawnHeavy, player.Controller.B);
        shortcuts.Add((int)Action.SetHarvest, player.Controller.A);

        //Slots
        queueSlots = new SlotContent[queueSlotImages.Length];

        //Get inactive queue slot indicators
        inactiveQueueSlotImages = new Image[queueSlotImages.Length];
        for (int i = 0; i < queueSlotImages.Length; i++)
        {
            inactiveQueueSlotImages[i] = queueSlotImages[i].transform.GetChild(0).GetComponent<Image>();
            queueSlots[i] = SlotContent.None;
        }

        //UpdateQueueSlots();
        SetQueueSlot(0, SlotContent.None);

        //Progress bar
        progressBar.fillAmount = 0;
    }

    protected override void Update()
    {
        //Default player interaction behaviour
        base.Update();

        if (Input.GetKeyDown(KeyCode.Space)) UpdateQueueSlots();

        if(ActiveSlot != SlotContent.None)
        {
            queueTimer -= Time.deltaTime;
            progressBar.fillAmount = 1 - (queueTimer / currentQueueTime);

            //If timer reached zero
            if(queueTimer <= 0)
            {
                progressBar.fillAmount = 0;
                SpawnCharacter(ActiveSlot);
                PopSlot();
            }
        }

        //Crystals
        crystalCounter.text = player.Crystals.ToString();
    }

    //Update the visuals of all slots
    private void UpdateQueueSlots()
    {
        for (int i = 0; i < queueSlots.Length; i++)
        {
            SetQueueSlot(i, queueSlots[i]);
        }
    }

    //Update to timer to match the active slot
    private void UpdateTimer()
    {
        if (ActiveSlot == SlotContent.None)
        {
            currentQueueTime = 0;
            queueTimer = 0;
        }
        else
        {
            currentQueueTime = QUEUE_TIMES[(int)ActiveSlot];
            queueTimer = currentQueueTime;
        }
    }

    //Display nothing/character in a slot
    private void SetQueueSlot(int index, SlotContent content)
    {
        int characterClass = (int)content;
        queueSlotImages[index].enabled = (content != SlotContent.None);
        inactiveQueueSlotImages[index].enabled = content == SlotContent.None;

        if (content != SlotContent.None)
        {
            queueSlotImages[index].sprite = index == 0 ? queueIconsActive[characterClass] : queueIconsQueued[characterClass];
        }
    }

    private void QueueCharacter(SlotContent content)
    {
        if (content == SlotContent.None) return;

        int characterClass = (int)content;

        //Check if player has enough crystals
        int requiredCrystals = CHARACTER_COSTS[characterClass];
        if (player.Crystals < requiredCrystals) return;

        //Remove crystals
        player.Crystals -= requiredCrystals;

        //Find an empty slot and queue the character
        for (int i = 0; i < queueSlots.Length; i++)
        {
            if (queueSlots[i] != SlotContent.None) continue;
            queueSlots[i] = content;

            //If active slot set timer
            if (i == 0) UpdateTimer();

            break;
        }

        //Update slot visuals
        UpdateQueueSlots();
    }

    private void PopSlot()
    {
        //Clear active slot
        queueSlots[0] = SlotContent.None;

        //Shift all slots forward
        for (int i = 0; i < queueSlots.Length; i++)
        {
            queueSlots[i] = i + 1 < queueSlots.Length ? queueSlots[i + 1] : SlotContent.None;
        }

        //Update the timer
        UpdateTimer();

        //Update visuals
        UpdateQueueSlots();
    }

    private void SpawnCharacter(SlotContent content)
    {
        Debug.Log("Spawning Character " + content);
    }

    protected override void ActionA()
    {
        //harvestTargetMarker.transform.position = player.WorldCursor.position;
        SetMarkerPosition(harvestTargetMarker, player.WorldCursor.position);
    }

    protected override void ActionB()
    {
        QueueCharacter(SlotContent.Heavy);
    }

    protected override void ActionX()
    {
        QueueCharacter(SlotContent.Archer);
    }

    protected override void ActionY()
    {
        QueueCharacter(SlotContent.Knight);
    }

}
