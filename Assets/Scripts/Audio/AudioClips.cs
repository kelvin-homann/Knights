using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioClips : MonoBehaviour
{
    private static AudioClips instance = null;
    private static bool destroyed = false;

    public AudioClipArray gruntSounds;
    public AudioClipArray yelpSounds;
    public AudioClipArray wooshSounds;
    public AudioClipArray woodHitSounds;
    public AudioClipArray arrowHitSounds;

    public static AudioClips Instance { get { return instance; } protected set {} }

    private void Awake()
    {
        if(instance == null)
            instance = this;
        else if(instance != this) {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        destroyed = true;
    }

    public static AudioClips GetInstance()
    {
        if(instance == null && !destroyed)
            throw new Exception("AudioClips.GetInstance(): Fatal Error: AudioClips has not yet been initialized. " +
                "It needs to be a component of a GameObject. Maybe you need to make sure that the AudioClips script is above " +
                "Default Time in Project Settings/Script Execution Order");
        return instance;
    }
}
