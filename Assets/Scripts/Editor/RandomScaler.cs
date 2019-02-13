using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RandomScaler : EditorWindow {

    private Vector3 minScale = Vector3.one;
    private Vector3 maxScale = Vector3.one;
    private bool uniformXZ = false, uniformXY = false;
    private bool scaleX = true, scaleY = true, scaleZ = true;

    [MenuItem("Tools/Random Scaler")]
	public static void Open()
    {
        GetWindow<RandomScaler>(true, "Random Scaler");
    }

    private void OnGUI()
    {
        scaleX = EditorGUILayout.Toggle("Scale X", scaleX);
        scaleY = EditorGUILayout.Toggle("Scale Y", scaleY);
        scaleZ = EditorGUILayout.Toggle("Scale Z", scaleZ);
        uniformXZ = EditorGUILayout.Toggle("Uniform XZ", uniformXZ);
        uniformXY = EditorGUILayout.Toggle("Uniform XY", uniformXY);

        Vector2 X, Y, Z;
        X = EditorGUILayout.Vector2Field("X (Min, Max)", new Vector2(minScale.x, maxScale.x));
        Y = EditorGUILayout.Vector2Field("Y (Min, Max)", new Vector2(minScale.y, maxScale.y));
        Z = EditorGUILayout.Vector2Field("Z (Min, Max)", new Vector2(minScale.z, maxScale.z));

        minScale = new Vector3(X.x, Y.x, Z.x);
        maxScale = new Vector3(X.y, Y.y, Z.y);

        if (GUILayout.Button("Scale Selected")) ScaleSelected(minScale, maxScale, uniformXZ, uniformXY, scaleX, scaleY, scaleZ);
    }

    public static void ScaleSelected(Vector3 min, Vector3 max, bool uniformXZ, bool uniformXY, bool x=true, bool y=true, bool z=true)
    {
        Undo.RecordObjects(Selection.transforms, "Random Scale");
        foreach (Transform t in Selection.transforms)
        {
            Vector3 scale = new Vector3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), Random.Range(min.z, max.z));
            if (uniformXZ) scale.z = scale.x;
            if (uniformXY) scale.y = scale.x;
            t.localScale = new Vector3(x ? scale.x : t.localScale.x, y ? scale.y : t.localScale.y, z ? scale.z : t.localScale.z);
        }
        Undo.FlushUndoRecordObjects();
    }

}
