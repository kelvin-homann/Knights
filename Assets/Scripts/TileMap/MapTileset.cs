using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New_Tileset", menuName = "Map Tileset")]
public class MapTileset : ScriptableObject
{
    public List<MapTile> tiles;
}
