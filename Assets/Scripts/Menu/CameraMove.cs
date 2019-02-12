using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour {
    Vector3 transformCam = new Vector3(8.75f, 6.3f, 0f);
    Vector3 rotationCam = new Vector3(5, -90, 0);
    Vector3 curPos;
    Vector3 curRot;

    private void Start()
    {
        curPos = this.transform.position;
        curRot = this.transform.eulerAngles;
    }
    void Update () {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            this.transform.position = Vector3.Lerp(transformCam, curPos, Time.deltaTime);
            this.transform.eulerAngles = Vector3.Lerp(rotationCam, curRot, Time.deltaTime);
        }
    }
}
