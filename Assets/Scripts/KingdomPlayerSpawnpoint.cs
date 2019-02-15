using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KingdomPlayerSpawnpoint : MonoBehaviour {

    public KingdomMaterialContainer.Kingdom kingdom;

    private static Vector3[] positions;

	// Use this for initialization
	void Awake () {
        if (positions == null) positions = new Vector3[4];
        positions[(int)kingdom] = transform.position;
	}
	
    public static Vector3 GetPoint(int kingdom)
    {
        return positions[kingdom];
    }
}
