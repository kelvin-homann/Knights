using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Castle : MonoBehaviour
{
    public float healthPointsRelative = 1f;
    public GameObject intact;
    public GameObject damaged;
    public GameObject ruin;
    public GameObject pieces;
    public ParticleSystem particles;

    private int destructionState = 0;

    private void Start()
    {
        particles.Stop();

        // show intact and hide damaged models
        intact.SetActive(true);
        damaged.SetActive(false);
        ruin.SetActive(false);
        pieces.SetActive(false);
    }

    private void FixedUpdate()
    {
        if(healthPointsRelative < 0f)
            healthPointsRelative = 0f;

        if(healthPointsRelative <= 0.4f && destructionState == 0)
        {
            particles.Play();
            damaged.SetActive(true);
            ruin.SetActive(false);
            intact.SetActive(false);
            destructionState++;
        }
        else if(healthPointsRelative <= 0f && destructionState == 1)
        {
            particles.Play();
            ruin.SetActive(true);
            damaged.SetActive(false);
            pieces.SetActive(true);
            destructionState++;
        }
    }
}
