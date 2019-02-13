using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MapMaker : EditorWindow {

    private Texture2D mapImage;
    private MapTileset tileset;

    private int[,] map;
    private int width, height;
    private Transform parent;

    [MenuItem("Tools/Map Maker")]
    private static void Open()
    {
        GetWindow<MapMaker>("Map Maker");
    }

    private void OnGUI()
    {
        tileset = EditorGUILayout.ObjectField("Tileset", tileset, typeof(MapTileset), false) as MapTileset;

        Rect imageRect = GUILayoutUtility.GetAspectRect(1, GUILayout.Width(Mathf.Min(position.width, position.height - 20) * 0.95f));
        imageRect.position = new Vector2(imageRect.x + 15, imageRect.y);
        mapImage = EditorGUI.ObjectField(imageRect, mapImage, typeof(Texture2D), false) as Texture2D;

        if (GUILayout.Button("Build Map")) BuildMap();
    }

    private void BuildMap()
    {
        //Init map
        width = mapImage.width;
        height = mapImage.height;
        map = new int[width, height];

        //Load image
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color pixel = mapImage.GetPixel(x, y);
                map[x, y] = (int)(pixel.r * 255);
                if (pixel.a == 0 || pixel == Color.white) map[x, y] = -1;
            }
        }

        //Make Parent
        GameObject p = new GameObject("Tiles");
        parent = p.transform;

        //Make tiles
        for (int l = 0; l < 255; l++) //Layer
        {
            for (int x = 0; x < width; x++) //X
            {
                for (int y = 0; y < height; y++) //Y (Z)
                {
                    //Check if tile should be filled
                    if (!Match(GetMap(x, y), 1, l)) continue;

                    //Find matching tile
                    foreach (var tile in tileset.tiles)
                    {
                        if (CheckTile(tile, x, y, l)) break;
                    }
                }
            }
        }
    }

    private bool CheckTile(MapTile tile, int x, int y, int l)
    {
        bool match = true;
        int mapValue = 0;
        int maskValue = 0;
        
        for(int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                maskValue = tile.mask[j * 3 + i];
                mapValue = GetMap(x - 1 + i, y - 1 + j);
                if (!Match(mapValue, maskValue, l)) match = false;
            }
        }

        if (match) SpawnTile(tile, x, y, l);

        return match;
    }

    private void SpawnTile(MapTile tile, int x, int y, int v)
    {
        Vector3 pos = new Vector3(x, v, y);
        GameObject.Instantiate(tile.tilePrefab, pos, tile.tilePrefab.transform.rotation, parent);
    }

    private int GetMap(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return -1;
        return map[x, y];
    }

    private bool Match(int map, int mask, int layer)
    {
        if (mask == 2) return true;
        return (map >= layer ? mask == 1 : mask == 0);
    }
}
