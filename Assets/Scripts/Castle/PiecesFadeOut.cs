using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiecesFadeOut : MonoBehaviour {

    MeshRenderer meshRenderer;

    private bool fadingout = false;

    void Start () {
        meshRenderer = this.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial.SetFloat("_Amount", 0f);
    }

    IEnumerator FadeOut()
    {
//        yield return new WaitForSecondsRealtime(Random.Range(2f, 5f));
        yield return new WaitForSecondsRealtime(4);

        for (float i = 0; i < Random.Range(0.7f, 0.9f); i += 0.005f)
        {
            meshRenderer.sharedMaterial.SetFloat("_Amount", i);
            yield return new WaitForSeconds(0.05f);
        }
        Destroy(gameObject);
    }

    public void startFading()
    {
        StartCoroutine("FadeOut");
    }

    public void Update()
    {
        if (!fadingout && this.isActiveAndEnabled)
        {
            startFading();
            fadingout = true;
        }
    }
}
