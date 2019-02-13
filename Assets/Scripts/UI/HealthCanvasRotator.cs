using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthCanvasRotator : MonoBehaviour
{
    public Canvas healthCanvas;

    void Awake()
    {
        healthCanvas = GetComponent<Canvas>();
    }

    void FixedUpdate()
    {
        if(healthCanvas == null)
            return;

        Vector3 lookDirection = Camera.main.transform.position - healthCanvas.transform.position;
        lookDirection.x = 1f;
        lookDirection.z = 0f;

        Quaternion rotation = Quaternion.LookRotation(lookDirection);
        healthCanvas.transform.rotation = rotation;
    }
}
