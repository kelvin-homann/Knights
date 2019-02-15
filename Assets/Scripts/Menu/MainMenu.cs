using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {
    [Header("UI")]
    public GameObject StartText;
    public CanvasGroup startScreen;
    public CanvasGroup lobbyScreen;
    public float uiFadeSpeed;

    [Header("Camera Movement")]
    public float smoothTime = 2F;
    public Transform startScreenPos;
    public Transform lobbyScreenPos;
    bool lobby = false;
    private Vector3 vel;

    public Material skyMaterial;
    public float scrollSpeed;
    private float rotation;

    private void Start()
    {
        transform.position = startScreenPos.position;
        transform.rotation = startScreenPos.rotation;

        startScreen.alpha = 1;
        lobbyScreen.alpha = 0;
    }

    void Update () {
        if (!lobby)
        {
            if (InputManager.Controller.Any.AnyKey.WasPressed || Input.GetKeyDown(KeyCode.Space))
            {
                lobby = true;
            }
        }
        else
        {
            //Camera pan
            transform.position = Vector3.SmoothDamp(transform.position, lobbyScreenPos.position, ref vel, smoothTime * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, lobbyScreenPos.rotation, Time.deltaTime * smoothTime * 3);
        }

        //Screen alpha
        startScreen.alpha = Mathf.Lerp(startScreen.alpha, lobby ? 0 : 1, Time.deltaTime * uiFadeSpeed);
        lobbyScreen.alpha = Mathf.Lerp(lobbyScreen.alpha, lobby ? 1 : 0, Time.deltaTime * uiFadeSpeed);

        //Sky
        rotation += scrollSpeed * Time.deltaTime;
        if (rotation > 360) rotation -= 360;
        if (rotation < 0) rotation += 360;

        skyMaterial.SetFloat("_Rotation", rotation);

    }
}
