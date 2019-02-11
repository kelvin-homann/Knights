using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiecesFadeOut : MonoBehaviour {

    public Material materialToFade;
    public float fadeTime;

    private bool fadingout = false;
    private float fadeTimer;

    private const string AMOUNT = "_Amount";

    private void Awake()
    {
        materialToFade.SetFloat(AMOUNT, 0f);
        enabled = false;
    }

    private void OnEnable () {
        materialToFade.SetFloat(AMOUNT, 0f);
        fadeTimer = fadeTime;
    }

    IEnumerator FadeOut()
    {
//        yield return new WaitForSecondsRealtime(Random.Range(2f, 5f));
        yield return new WaitForSecondsRealtime(4);

        for (float i = 0; i < Random.Range(0.7f, 0.9f); i += 0.005f)
        {
            //meshRenderer.sharedMaterial.SetFloat("_Amount", i);
            yield return new WaitForSeconds(0.05f);
        }
        Destroy(gameObject);
    }

    public void startFading()
    {
        //StartCoroutine("FadeOut");
    }

    public void Update()
    {
        if(fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;
            float value = 1.0f - (fadeTimer / fadeTime);
            materialToFade.SetFloat(AMOUNT, value);
        }
        else
        {
            gameObject.SetActive(false);
        }

        //if (!fadingout && this.isActiveAndEnabled)
        //{
        //    startFading();
        //    fadingout = true;
        //}
    }
}
