using UnityEngine;
using UnityEngine.EventSystems;

using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using InputManager;

[AddComponentMenu("Radial Menu Framework/RMF Core Script")]
public class RMF_RadialMenu : MonoBehaviour {

    [HideInInspector]
    public RectTransform rt;
    //public RectTransform baseCircleRT;
    //public Image selectionFollowerImage;

    public bool visible = true;
    public bool flashing;

    [Tooltip("Adjusts the radial menu for use with a gamepad or joystick. You might need to edit this script if you're not using the default horizontal and vertical input axes.")]
    public bool useGamepad = false;
    public Player player;

    [Tooltip("With lazy selection, you only have to point your mouse (or joystick) in the direction of an element to select it, rather than be moused over the element entirely.")]
    public bool useLazySelection = true;


    [Tooltip("If set to true, a pointer with a graphic of your choosing will aim in the direction of your mouse. You will need to specify the container for the selection follower.")]
    public bool useSelectionFollower = true;

    [Tooltip("If using the selection follower, this must point to the rect transform of the selection follower's container.")]
    public RectTransform selectionFollowerContainer;

    [Tooltip("This is the text object that will display the labels of the radial elements when they are being hovered over. If you don't want a label, leave this blank.")]
    public Text textLabel;

    public float activeElementOffset = 0;
    public float invisibleElementOffset;
    public float fadeSpeed;

    [Tooltip("This is the list of radial menu elements. This is order-dependent. The first element in the list will be the first element created, and so on.")]
    public List<RMF_RadialMenuElement> elements = new List<RMF_RadialMenuElement>();


    [Tooltip("Controls the total angle offset for all elements. For example, if set to 45, all elements will be shifted +45 degrees. Good values are generally 45, 90, or 180")]
    public float globalOffset = 0f;


    [HideInInspector]
    public float currentAngle = 0f; //Our current angle from the center of the radial menu.


    [HideInInspector]
    public int index = 0; //The current index of the element we're pointing at.

    private int elementCount;

    private float angleOffset; //The base offset. For example, if there are 4 elements, then our offset is 360/4 = 90

    private int selectedElement = 0;
    private int previousActiveIndex = 0; //Used to determine which buttons to unhighlight in lazy selection.

    private PointerEventData pointer;
    CanvasGroup cg;

    private Controller Controller { get { return player.Controller; } }
    private bool oldJoystickMoved = false;
    private int shortcut = -1;

    public bool ShowingInfo { get { return selectedElement != -1 && shortcut == -1; } }

    void Awake() {

        pointer = new PointerEventData(EventSystem.current);

        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();

        if (rt == null)
            Debug.LogError("Radial Menu: Rect Transform for radial menu " + gameObject.name + " could not be found. Please ensure this is an object parented to a canvas.");

        if (useSelectionFollower && selectionFollowerContainer == null)
            Debug.LogError("Radial Menu: Selection follower container is unassigned on " + gameObject.name + ", which has the selection follower enabled.");

        elementCount = elements.Count;

        angleOffset = (360f / (float)elementCount);

        //Loop through and set up the elements.
        for (int i = 0; i < elementCount; i++) {
            if (elements[i] == null) {
                Debug.LogError("Radial Menu: element " + i.ToString() + " in the radial menu " + gameObject.name + " is null!");
                continue;
            }
            elements[i].parentRM = this;

            elements[i].setAllAngles((angleOffset * i) + globalOffset, angleOffset);

            elements[i].assignedIndex = i;
            elements[i].activeOffset = activeElementOffset;
            elements[i].invisibleOffset = invisibleElementOffset;           
        }

    }


    void Start() {


        if (useGamepad) {
            EventSystem.current.SetSelectedGameObject(gameObject, null); //We'll make this the active object when we start it. Comment this line to set it manually from another script.
            if (useSelectionFollower && selectionFollowerContainer != null)
                selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, -globalOffset); //Point the selection follower at the first element.
        }

    }

    // Update is called once per frame
    void Update() {

        foreach (var e in elements) e.visible = visible;
        cg.alpha = Mathf.Lerp(cg.alpha, visible || flashing ? 1.0f : 0f, Time.deltaTime * fadeSpeed);

        //If your gamepad uses different horizontal and vertical joystick inputs, change them here!
        //==============================================================================================
        //bool joystickMoved = Input.GetAxis("Horizontal") != 0.0 || Input.GetAxis("Vertical") != 0.0;
        bool joystickMoved = Mathf.Abs(Controller.LeftStick.X) > 0.5f || Mathf.Abs(Controller.LeftStick.Y) > 0.5f;
        //==============================================================================================

        if(useGamepad && !joystickMoved)
        {
            //Shortcuts
            if (visible)
            {
                Shortcut(Controller.Y, 0);
                Shortcut(Controller.B, 1);
                Shortcut(Controller.A, 2);
                Shortcut(Controller.X, 3);
            }

            if (selectedElement != -1 && shortcut == -1) UnSelectAll();
            oldJoystickMoved = joystickMoved;
            return;
        }


        float rawAngle;
        
        if (!useGamepad)
            rawAngle = Mathf.Atan2(Input.mousePosition.y - rt.position.y, Input.mousePosition.x - rt.position.x) * Mathf.Rad2Deg;
        else
            //rawAngle = Mathf.Atan2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal")) * Mathf.Rad2Deg;
            rawAngle = Mathf.Atan2(Controller.LeftStick.Y, Controller.LeftStick.X) * Mathf.Rad2Deg;

        //If no gamepad, update the angle always. Otherwise, only update it if we've moved the joystick.
        if (!useGamepad)
            currentAngle = normalizeAngle(-rawAngle + 90 - globalOffset + (angleOffset / 2f));
        else if (joystickMoved)
            currentAngle = normalizeAngle(-rawAngle + 90 - globalOffset + (angleOffset / 2f));

        //Handles lazy selection. Checks the current angle, matches it to the index of an element, and then highlights that element.
        if (angleOffset != 0 && useLazySelection) {

            //Current element index we're pointing at.
            index = (int)(currentAngle / angleOffset);

            if (elements[index] != null) {

                //Select it.
                selectButton(index);

                //If we click or press a "submit" button (Button on joystick, enter, or spacebar), then we'll execut the OnClick() function for the button.
                if (!useGamepad ? Input.GetMouseButtonDown(0) : Controller.A.WasPressed) {
                    ActivateElement();                    
                }
            }

        }

        //Updates the selection follower if we're using one.
        if (useSelectionFollower && selectionFollowerContainer != null) {
            if (!useGamepad || joystickMoved)
            {
                Quaternion rot = Quaternion.Euler(0, 0, rawAngle + 270);
                selectionFollowerContainer.rotation = !oldJoystickMoved ? rot : Quaternion.Lerp(selectionFollowerContainer.rotation, rot, Time.deltaTime * 12);
            }
           

        }

        oldJoystickMoved = joystickMoved;
    }


    //Selects the button with the specified index.
    private void selectButton(int i, bool showInfo = true) {

        selectedElement = i;

        selectionFollowerContainer.gameObject.SetActive(true);
        if (elements[i].active == false) {

            elements[i].highlightThisElement(pointer, showInfo); //Select this one

            if (previousActiveIndex != i) 
                elements[previousActiveIndex].unHighlightThisElement(pointer); //Deselect the last one.
            

        }

        previousActiveIndex = i;

    }

    private void UnSelectAll()
    {
        selectedElement = -1;
        selectionFollowerContainer.gameObject.SetActive(false);
        foreach(var e in elements)
        {
            e.unHighlightThisElement(pointer);
        }
    }

    private void ActivateElement(bool release=false)
    {
        if (!visible) return;

        ExecuteEvents.Execute(elements[selectedElement].button.gameObject, pointer, ExecuteEvents.submitHandler);
        if (release) UnSelectAll();
    }

    private void Shortcut(InputManager.IButton button, int element)
    {
        if(button.WasPressed && shortcut == -1)
        {
            shortcut = element;
            selectButton(element, false);
        }
        if(shortcut == element && button.WasReleased)
        {
            ActivateElement(true);
            shortcut = -1;
        }
    }

    //Keeps angles between 0 and 360.
    private float normalizeAngle(float angle) {

        angle = angle % 360f;

        if (angle < 0)
            angle += 360;

        return angle;

    }


}
