using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputManager;

public class InputTester : MonoBehaviour {

    public enum ControllerIndex { One, Two, Three, Four, Any }

    public ControllerIndex player;
    public Controller controller;

    public bool A, B, X, Y, RB, LB, STA, BAC, RS, LS, Any;
    public float LS_X, LS_Y, RS_X, RS_Y, DP_X, DP_Y, LT, RT;

	// Use this for initialization
	void Start () {
        controller = GetController();
	}
	
	// Update is called once per frame
	void Update () {
        A = controller.A.IsHeld;
        B = controller.B.IsHeld;
        Y = controller.Y.IsHeld;
        X = controller.X.IsHeld;
        RB = controller.RightBumper.IsHeld;
        LB = controller.LeftBumper.IsHeld;
        STA = controller.Start.IsHeld;
        BAC = controller.Back.IsHeld;
        LS = controller.LeftStick.IsHeld;
        RS = controller.RightStick.IsHeld;

        LS_X = controller.LeftStick.X;
        LS_Y = controller.LeftStick.Y;
        RS_X = controller.RightStick.X;
        RS_Y = controller.RightStick.Y;
        DP_X = controller.DPad.X;
        DP_Y = controller.DPad.Y;
        LT = controller.LeftTrigger.Value;
        RT = controller.RightTrigger.Value;
        Any = controller.AnyKey.IsHeld;

        if (Controller.Any.A.WasPressed || Controller.Any.Start.WasPressed)
        {
            Debug.Log(Controller.GetPressed().Player);
        }
    }

    private Controller GetController()
    {
        switch (player)
        {
            case ControllerIndex.One:
                return Controller.One;
            case ControllerIndex.Two:
                return Controller.Two;
            case ControllerIndex.Three:
                return Controller.Three;
            case ControllerIndex.Four:
                return Controller.Four;
            case ControllerIndex.Any:
                return Controller.Any;
        }
        return null;
    }
}
