using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraInputManager : MonoBehaviour
{
    public bool autoInitializeCameras = true;
    public Camera[] cameras;
    private int activeCameraIndex = 0;

    private void Awake()
    {
        if(autoInitializeCameras)
        {
            cameras = GetComponentsInChildren<Camera>();
            for(int i = 0; i < cameras.Length; i++)
            {
                if(cameras[i].enabled)
                {
                    activeCameraIndex = i;
                    break;
                }
            }
        }
    }

    void Update()
    {
        /* CACHE KEY STATES */

        // getkeys
        bool keySpace = Input.GetKey(KeyCode.Space);
        bool keyLeftControl = Input.GetKey(KeyCode.LeftControl);

        // getkeydowns
        bool keySpaceDown = Input.GetKeyDown(KeyCode.Space);


        /* INPUT LOGIC */

        // render health bar state
        if(keyLeftControl && keySpaceDown && DisplaySettings.renderHealthBarsState == ESwitchState.Off)
            DisplaySettings.renderHealthBarsState = ESwitchState.On;

        else if(keyLeftControl && keySpaceDown && DisplaySettings.renderHealthBarsState != ESwitchState.Off)
            DisplaySettings.renderHealthBarsState = ESwitchState.Off;

        // render health bar
        if((keySpace && DisplaySettings.renderHealthBarsState == ESwitchState.Off) || (!keySpace && DisplaySettings.renderHealthBarsState == ESwitchState.On))
            DisplaySettings.renderHealthBars = true;
        else
            DisplaySettings.renderHealthBars = false;

        if(Input.GetKeyDown(KeyCode.Alpha0))
            SetCamera(0);
        else if(Input.GetKeyDown(KeyCode.Alpha1))
            SetCamera(1);
        else if(Input.GetKeyDown(KeyCode.Alpha2))
            SetCamera(2);
        else if(Input.GetKeyDown(KeyCode.Alpha3))
            SetCamera(3);
        else if(Input.GetKeyDown(KeyCode.Alpha4))
            SetCamera(4);
        else if(Input.GetKeyDown(KeyCode.Alpha5))
            SetCamera(5);

        if(Input.GetKeyDown(KeyCode.Comma))
            SetPreviousCamera();
        else if(Input.GetKeyDown(KeyCode.Period))
            SetNextCamera();
    }

    private void SetCamera(int index)
    {
        if(index < 0 || index >= cameras.Length)
            return;

        for(int i = 0; i < cameras.Length; i++)
            cameras[i].enabled = i == index;

        activeCameraIndex = index;
    }

    private void SetNextCamera()
    {
        SetCamera((activeCameraIndex + 1) % cameras.Length);
    }

    private void SetPreviousCamera()
    {
        SetCamera((activeCameraIndex + cameras.Length - 1) % cameras.Length);
    }
}
