using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TileMerger {

    private const int VERTEX_LIMIT = 65535;

    private GameObject parent;
    private List<Vector3> verts;
    private List<List<int>> tris;
    private List<Vector3> normals;
    //Mesh chunkMesh;
    //List<CombineInstance> instances;
    public TileMerger()
    {
        parent = new GameObject("Tiles");
        verts = new List<Vector3>();
        tris = new List<List<int>>();
        normals = new List<Vector3>();
        //chunkMesh = new Mesh();
        //instances = new List<CombineInstance>();
    }

    public void AddMesh(Mesh mesh, Vector3 offset)
    {
        if (mesh.vertexCount + verts.Count >= VERTEX_LIMIT) CreateNewSubMesh();
        //if (.vertexCount + mesh.vertexCount >= VERTEX_LIMIT) CreateNewSubMesh();

        //CombineInstance instance = new CombineInstance();
        //instance.mesh = mesh;
        //instance.transform = Matrix4x4.TRS(offset, Quaternion.Euler(-90, 0, 0), Vector3.one);
        //instances.Add(instance);
        Matrix4x4 m = Matrix4x4.TRS(offset, Quaternion.Euler(-90, 0, 0), Vector3.one);
        int count = verts.Count;

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            verts.Add(m.MultiplyPoint(mesh.vertices[i]));
        }

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            if (i >= tris.Count) tris.Add(new List<int>());

            int submesh = mesh.subMeshCount == 1 ? 1 : i;
            if (tris.Count <= submesh) tris.Add(new List<int>());

            int[] meshTris = mesh.GetTriangles(i);
            for (int j = 0; j < meshTris.Length; j++)
                tris[submesh].Add(count + meshTris[j]);
        }

        normals.AddRange(mesh.normals);
    }
    
    private void CreateNewSubMesh()
    {
        GameObject chunk = new GameObject("Chunk", typeof(MeshRenderer), typeof(MeshFilter));
        chunk.transform.SetParent(parent.transform);

        Mesh chunkMesh = new Mesh();
        chunkMesh.SetVertices(verts);
        //chunkMesh.SetNormals(normals);

        chunkMesh.subMeshCount = tris.Count;
        for (int i = 0; i < tris.Count; i++)
        {
            chunkMesh.SetTriangles(tris[i], i);
        }

        chunkMesh.RecalculateBounds();
        //chunkMesh.RecalculateTangents();
        chunkMesh.RecalculateNormals();

        //chunkMesh.CombineMeshes(instances.ToArray(), false, true, false);
        chunk.GetComponent<MeshFilter>().sharedMesh = chunkMesh;

        tris.Clear();
        verts.Clear();
        normals.Clear();
        //chunkMesh = new Mesh();
        //instances = new List<CombineInstance>();
    }

    public void Apply(Material[] materials)
    {
        string path = EditorUtility.OpenFolderPanel("Select mesh save location", Application.dataPath, "");
        if (string.IsNullOrEmpty(path)) path = "";
        path = "Assets" + path.Replace(Application.dataPath, "");
        Debug.Log(path);
        Debug.Log(Application.dataPath);

        if (verts.Count > 0) CreateNewSubMesh();
        //CreateNewSubMesh();

        foreach (Transform t in parent.transform)
        {
            t.GetComponent<MeshRenderer>().sharedMaterials = materials;
            Mesh m = t.GetComponent<MeshFilter>().sharedMesh;
            string filePath = path + "/ChunkMesh_" + Mathf.Abs(m.GetInstanceID()).ToString() + ".asset";
            Debug.Log(filePath);
            AssetDatabase.CreateAsset(m, filePath);
        }

        AssetDatabase.SaveAssets();
    }

}
