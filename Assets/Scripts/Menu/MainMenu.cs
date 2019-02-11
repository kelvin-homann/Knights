using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {
    public float transitionSpeed;
    public Transform[] targets;
    Transform currentTarget;
    public Camera camera;

    public void Start()
    {
        currentTarget = targets[0];
    }

    public void SelectPlayers()
    {
        // Move Camera to Flags      
        currentTarget = targets[1];
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private void LateUpdate()
    {
        camera.transform.position = Vector3.Lerp(transform.position, currentTarget.position, Time.deltaTime * transitionSpeed);
        Vector3 currentAngle = new Vector3(Mathf.LerpAngle(camera.transform.rotation.eulerAngles.x, 
            currentTarget.transform.rotation.eulerAngles.x, Time.deltaTime * transitionSpeed),
            Mathf.LerpAngle(camera.transform.rotation.eulerAngles.y, currentTarget.transform.rotation.eulerAngles.y,
            Time.deltaTime * transitionSpeed), Mathf.LerpAngle(camera.transform.rotation.eulerAngles.z, currentTarget.transform.rotation.eulerAngles.z,
            Time.deltaTime * transitionSpeed));

        camera.transform.eulerAngles = currentAngle;

    }
}
