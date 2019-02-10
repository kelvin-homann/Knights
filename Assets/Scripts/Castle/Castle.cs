using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Castle : MonoBehaviour {
    public int health = 100;

    public GameObject intact;
    public GameObject damaged;
    public GameObject ruin;
    public GameObject pieces;

    public ParticleSystem particles;

    private bool destroyed_once = false;
    private bool destroyed_twice = false;

    void Start () {
        particles.Stop();
        // Disabling the Damaged Models
        intact.SetActive(true);
        damaged.SetActive(false);
        ruin.SetActive(false);
        pieces.SetActive(false);
    }
	
	void Update () {
        if (health < 0)
            health = 0;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            health = health - 20;
        } 
        
        if (health <= 40) // First Destruction
        {
            if (!destroyed_once)
            {
                particles.Play();
                damaged.SetActive(true);
                ruin.SetActive(false);
                intact.SetActive(false);
                destroyed_once = true;
            }            
        }

        if (health <= 0) // Second Destruction
        {
            if (!destroyed_twice)
            {
                particles.Play();
                ruin.SetActive(true);
                damaged.SetActive(false);
                pieces.SetActive(true);
                destroyed_twice = true;
            }
        }
    }
}
