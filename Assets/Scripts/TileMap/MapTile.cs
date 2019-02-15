using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New_Tile", menuName = "Map Tile")]
public class MapTile : ScriptableObject {

    public const int MASK_WIDTH = 3;
    public const int MASK_HEIGHT = 3;

    public int[] mask;
    public GameObject tilePrefab;

    private Mesh tileMesh;
    public Mesh TileMesh {
        get {
            if (tileMesh == null) tileMesh = tilePrefab.GetComponent<MeshFilter>().sharedMesh;
            return tileMesh;
        }
    }

    public MapTile()
    {
        mask = new int[MASK_WIDTH * MASK_HEIGHT];
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MapTile))]
public class MapTileEditor : Editor
{
    private const int TILE_BUTTON_SIZE = 30;

    private MapTile t;

    private void OnEnable()
    {
        t = target as MapTile;
    }

    public override void OnInspectorGUI()
    {
        GUI.changed = false;
        int w = MapTile.MASK_WIDTH, h = MapTile.MASK_HEIGHT;

        for (int i = 0; i < h; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            for (int j = 0; j < w; j++)
            {
                int index = i * w + j;
                t.mask[index] = DrawTileButton(t.mask[index]);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        if (GUI.changed) EditorUtility.SetDirty(t);

        base.OnInspectorGUI();
    }

    private int DrawTileButton(int value)
    {
        bool result = GUILayout.Toggle(value > 0, "", "Button",
            GUILayout.Width(TILE_BUTTON_SIZE), GUILayout.Height(TILE_BUTTON_SIZE));
        return (result != value > 0) ? (result ? 1 : 0) : value;
    }
}
#endif