using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance = null;

    private static List<AudioClip> gruntSounds = null;
    private static List<AudioClip> yelpSounds = null;
    private static List<AudioClip> wooshSounds = null;
    private static List<AudioClip> woodHitSounds = null;

    private static int lastGruntSoundIndex = 0;
    private static int lastYelpSoundIndex = 0;
    private static int lastWooshSoundIndex = 0;
    private static int lastWoodHitSoundIndex = 0;

    private static bool destroyed = false;

    [ReadOnly] public bool audioClipsLoaded = false;

    public static AudioManager Instance { get { return instance; } protected set {} }

    void Awake()
    {
        if(instance == null)
            instance = this;
        else if(instance != this) {
            Destroy(gameObject);
            return;
        }

        if(!audioClipsLoaded)
            LoadAudioClips();
    }

    private void OnDestroy()
    {
        destroyed = true;
    }

    /// <summary>
    /// Gets the singleton instance of the only AudioManager
    /// </summary>
    /// <returns></returns>
    public static AudioManager GetInstance()
    {
        if(instance == null && !destroyed)
            throw new Exception("AudioManager.GetInstance(): Fatal Error: AudioManager has not yet been initialized. " +
                "It needs to be a component of a GameObject. Maybe you need to make sure that the AudioManager script is above " +
                "Default Time in Project Settings/Script Execution Order");
        return instance;
    }

    /// <summary>
    /// Returns a random grunt interjection sound as an AudioClip
    /// </summary>
    /// <returns></returns>
    public static AudioClip GetRandomGruntSound()
    {
        if(gruntSounds == null || gruntSounds.Count == 0)
            return null;

        int index = GetRandomIndex(lastGruntSoundIndex, gruntSounds);
        lastGruntSoundIndex = index;
        return gruntSounds[index];
    }

    /// <summary>
    /// Returns a random yelp interjection sound as an AudioClip
    /// </summary>
    /// <returns></returns>
    public static AudioClip GetRandomYelpSound()
    {
        if(yelpSounds == null || yelpSounds.Count == 0)
            return null;

        int index = GetRandomIndex(lastYelpSoundIndex, yelpSounds);
        lastYelpSoundIndex = index;
        return yelpSounds[index];
    }

    /// <summary>
    /// Returns a random woosh (air resistance) sound as an AudioClip
    /// </summary>
    /// <returns></returns>
    public static AudioClip GetRandomWooshSound()
    {
        if(wooshSounds == null || wooshSounds.Count == 0)
            return null;

        int index = GetRandomIndex(lastWooshSoundIndex, wooshSounds);
        lastWooshSoundIndex = index;
        return wooshSounds[index];
    }

    /// <summary>
    /// Returns a random wood hit sound as an AudioClip
    /// </summary>
    /// <returns></returns>
    public static AudioClip GetRandomWoodHitSound()
    {
        if(woodHitSounds == null || woodHitSounds.Count == 0)
            return null;

        int index = GetRandomIndex(lastWoodHitSoundIndex, woodHitSounds);
        lastWoodHitSoundIndex = index;
        return woodHitSounds[index];
    }

    /// <summary>
    /// Returns a random index within the specified list with respect to the last used index.
    /// This ensures that not the same sound is played several times in a row unless the 
    /// provided list only contains one sound.
    /// </summary>
    /// <returns></returns>
    private static int GetRandomIndex<T>(int lastIndex, List<T> list)
    {
        if(list == null || list.Count == 0)
            return -1;

        int index = lastIndex;

        // get a new random index that is not the last index
        while(index == lastIndex && list.Count > 1)
        {
            index = UnityEngine.Random.Range(0, list.Count);
        }

        return index;
    }

    /// <summary>
    /// Loads necessary sound files from the Resources folder, creates AudioClips from them
    /// and stores them into managed lists of AudioClips.
    /// </summary>
    /// <returns></returns>
    private static void LoadAudioClips()
    {
        gruntSounds = new List<AudioClip>();
        yelpSounds = new List<AudioClip>();
        wooshSounds = new List<AudioClip>();
        woodHitSounds = new List<AudioClip>();

        for(int i = 1; i <= 8; i++)
        {
            string audioClipFileName = string.Format("Sounds/grunt_{0:00}", i);
            AudioClip audioClip = Resources.Load<AudioClip>(audioClipFileName);
            if(audioClip != null)
                gruntSounds.Add(audioClip);
            else
                LogSystem.Log(ELogMessageType.AudioManagerAudioClipLoading,
                    "could not load resource with file name <color=white>{0}</color>", audioClipFileName);
        }

        for(int i = 1; i <= 3; i++)
        {
            string audioClipFileName = string.Format("Sounds/yelp_{0:00}", i);
            AudioClip audioClip = Resources.Load<AudioClip>(audioClipFileName);
            if(audioClip != null)
                yelpSounds.Add(audioClip);
            else
                LogSystem.Log(ELogMessageType.AudioManagerAudioClipLoading,
                    "could not load resource with file name <color=white>{0}</color>", audioClipFileName);
        }

        for(int i = 1; i <= 4; i++)
        {
            string audioClipFileName = string.Format("Sounds/woosh_{0:00}", i);
            AudioClip audioClip = Resources.Load<AudioClip>(audioClipFileName);
            if(audioClip != null)
                wooshSounds.Add(audioClip);
            else
                LogSystem.Log(ELogMessageType.AudioManagerAudioClipLoading,
                    "could not load resource with file name <color=white>{0}</color>", audioClipFileName);
        }

        for(int i = 1; i <= 2; i++)
        {
            string audioClipFileName = string.Format("Sounds/wood_hit_{0:00}", i);
            AudioClip audioClip = Resources.Load<AudioClip>(audioClipFileName);
            if(audioClip != null)
                woodHitSounds.Add(audioClip);
            else
                LogSystem.Log(ELogMessageType.AudioManagerAudioClipLoading,
                    "could not load resource with file name <color=white>{0}</color>", audioClipFileName);
        }

        instance.audioClipsLoaded = true;
    }
}
