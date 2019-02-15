using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputManager;

public class SimpleInputController : MonoBehaviour {

    public float speed;
    public int index;

    private static int currentIndex = 0;

    private PlayerController playerCon;

	// Use this for initialization
	void Awake () {
        playerCon = GetComponent<PlayerController>();
    }
	
	// Update is called once per frame
	void LateUpdate () {
        //if (index != currentIndex) return;

        //string i = (index != 0 ? (index+1).ToString() : "");
        float hor = playerCon.MoveX;
        float vert = playerCon.MoveY;
        Vector3 control = new Vector3(hor, -vert, 0) * speed;

        transform.Translate(control * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Alpha1)) currentIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) currentIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) currentIndex = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) currentIndex = 3;

        //if (Controller.One.RightBumper.WasPressed) currentIndex++;
        //if (Controller.One.LeftBumper.WasPressed) currentIndex++;
        currentIndex = Mathf.Clamp(currentIndex, 0, 3);
    }
}
