using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterUIOnPlayer : MonoBehaviour {

    public SplitscreenCamera playerCamera;
    public Transform playerTransform;
    public Vector3 worldOffset;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        transform.position = playerCamera.WorldToScreenPoint(playerTransform.position + worldOffset);
	}
}
