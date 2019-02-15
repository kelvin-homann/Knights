using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Kingdom Material Container")]
public class KingdomMaterialContainer : ScriptableObject {

    public enum Kingdom { Blue, Red, Green, Yellow }

    public Material blue;
    public Material red;
    public Material green;
    public Material yellow;

    public Material GetMaterial(int kingdom)
    {
        switch((Kingdom)kingdom)
        {
            case Kingdom.Blue: return blue;
            case Kingdom.Red: return red;
            case Kingdom.Green: return green;
            case Kingdom.Yellow: return yellow;
        }
        return null;
    }

    public Material GetMaterial(Kingdom kingdom)
    {
        return GetMaterial((int)kingdom);
    }

}
