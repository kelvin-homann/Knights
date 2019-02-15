using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerInteractionController : MonoBehaviour {

    public GameObject mainInfoPanel;

    protected Player player;
    protected RMF_RadialMenu menu;
    protected System.Action[] actions;
    protected Dictionary<int, InputManager.IButton> shortcuts;
    protected bool visible;

    public delegate void ActionPerformedFunc(PlayerInteractionController sender, int index, bool shortcut);
    public event ActionPerformedFunc onActionPerformed;

    public bool IsVisible { get { return visible; } }
    public bool ShortcutsEnabled { get; set; }

    public float flashTimer = 0;

	// Use this for initialization
	void Awake() {
        menu = GetComponent<RMF_RadialMenu>();
        actions = new System.Action[4];
        actions[0] = ActionY;
        actions[1] = ActionB;
        actions[2] = ActionA;
        actions[3] = ActionX;
        shortcuts = new Dictionary<int, InputManager.IButton>();
    }
	
    public virtual void Init()
    {

    }

	// Update is called once per frame
	protected virtual void Update () {
        
        //Menu visiblity
        menu.visible = visible;
        
        //Shortcuts
        if (!visible && ShortcutsEnabled)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                if (!shortcuts.ContainsKey(i)) continue;
                if (shortcuts[i].WasPressed) PerformAction(i, true);
            }
        }

        //Info panel visibility
        mainInfoPanel.SetActive(!menu.ShowingInfo);

        //Flash Info
        if (flashTimer > 0)
        {
            menu.flashing = true;

            if (!visible)
            {
                mainInfoPanel.SetActive(true);
            }
            flashTimer -= Time.deltaTime;
        }
        else menu.flashing = false;
    }

    public void PerformAction(int index, bool shortcut)
    {
        if (index < actions.Length && index >= 0)
            actions[index].Invoke();

        if (onActionPerformed != null) onActionPerformed(this, index, shortcut);
    }

    public void PerformAction(int index) { PerformAction(index, false); }

    protected virtual void ActionY() { }
    protected virtual void ActionB() { }
    protected virtual void ActionA() { }
    protected virtual void ActionX() { }

    public void Show()
    {
        visible = true;
    }

    public void Hide()
    {
        visible = false;
    }

    public void AssignPlayer(Player player)
    {
        this.player = player;
        menu.player = player;
    }

    public void FlashInfo(float time)
    {
        flashTimer = time;
    }

    public void CancelFlashInfo()
    {
        flashTimer = 0;
    }

    public void SetMarkerPosition(Transform marker, Vector3 targetPos)
    {
        if (!marker.gameObject.activeSelf) marker.gameObject.SetActive(true);

        UnityEngine.AI.NavMeshHit hit;
        UnityEngine.AI.NavMesh.SamplePosition(targetPos, out hit, 100, UnityEngine.AI.NavMesh.AllAreas);

        marker.position = new Vector3(hit.position.x, marker.position.y, hit.position.z);
    }
}
