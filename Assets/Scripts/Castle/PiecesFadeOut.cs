using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiecesFadeOut : MonoBehaviour
{
    private const string AMOUNT = "_Amount";

    public Material materialToFade;
    public float fadeTime;
    private float fadeTimer;

    private void Awake()
    {
        materialToFade.SetFloat(AMOUNT, 0f);
        enabled = false;
    }

    private void OnEnable()
    {
        materialToFade.SetFloat(AMOUNT, 0f);
        fadeTimer = fadeTime;
    }

    //IEnumerator FadeOut()
    //{
    //    //yield return new WaitForSecondsRealtime(Random.Range(2f, 5f));
    //    yield return new WaitForSecondsRealtime(4f);

    //    for(float i = 0; i < Random.Range(0.7f, 0.9f); i += 0.005f)
    //    {
    //        yield return new WaitForSeconds(0.05f);
    //    }

    //    Destroy(gameObject);
    //}

    private void FixedUpdate()
    {
        if(fadeTimer > 0f)
        {
            fadeTimer -= Time.deltaTime;
            float value = 1.0f - (fadeTimer / fadeTime);
            materialToFade.SetFloat(AMOUNT, value);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
