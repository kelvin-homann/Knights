using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeRandomRotation : MonoBehaviour {
    float randomSize;

	void Start () {
        // Randomizing the Grass on the Map
        this.transform.eulerAngles = new Vector3(-90, 0, Random.Range(0, 360));
	}

}
