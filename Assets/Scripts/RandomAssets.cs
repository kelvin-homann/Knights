using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAssets : MonoBehaviour {
    float randomSize;

	void Start () {
        // Randomizing the Grass on the Map
        randomSize = Random.Range(0f, 1f);
        this.transform.localScale += new Vector3(randomSize, randomSize, randomSize);
        this.transform.eulerAngles = new Vector3(0, Random.Range(0, 360), 0);
	}

}
