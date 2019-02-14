using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {
    [Header("UI")]
    public GameObject StartText;

    [Header("Camera Movement")]
    public float smoothTime = 2F;
    private Vector3 velocity = Vector3.zero;
    Vector3 targetPosition = new Vector3(9f, 5.7f, 0f);
    bool keypressed = false;

    void Update () {
        if (!keypressed)
        {
            // Replace with Controller Input System
            if (Input.GetKeyDown(KeyCode.Space))
            {
                keypressed = true;
                StartText.SetActive(false);
            }
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        } 
        
        // Add player Selection Logic
    }
}
