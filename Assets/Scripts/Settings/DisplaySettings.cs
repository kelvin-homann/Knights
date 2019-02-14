using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A settings specific switch state that can either be on indefinitely, on for the current frame or off entirely.
/// </summary>
public enum ESwitchState
{
    On, 
    OnOnce, 
    Off
}

public static class DisplaySettings
{
    public static bool renderHealthBars = false;
    public static ESwitchState renderHealthBarsState = ESwitchState.On;

    public static bool renderHitIndicator = false;
}
