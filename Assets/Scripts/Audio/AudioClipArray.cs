using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioClipArray.asset", menuName = "Knights/Audio Clip Array")]
public class AudioClipArray : ScriptableObject
{
    public AudioClip[] audioClips;
    public float pitchVariation = 0f;
    //public int dontRepeatTheLast = 1;
    private int lastRandomIndex = 0;

    public AudioClip GetRandomAudioClip()
    {
        if(audioClips.Length == 0)
            return null;
        else if(audioClips.Length == 1)
            return audioClips[0];

        int index = lastRandomIndex;

        // get a new random index that is not the last index
        while(index == lastRandomIndex && audioClips.Length > 1)
        {
            index = UnityEngine.Random.Range(0, audioClips.Length);
        }

        return audioClips[index];
    }

    public float GetRandomPitch()
    {
        float halfVariation = pitchVariation / 2f;
        return Random.Range(1f - halfVariation, 1f + halfVariation);
    }
}
