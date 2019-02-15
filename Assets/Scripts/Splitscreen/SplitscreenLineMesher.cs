using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitscreenLineMesher
{
    //Meshes for the target screen areas
    private Mesh mesh;

    //List for verticies and triangles
    private List<Vector3> verts;
    private List<int> tris;

    //Array of lines (one line for each target pair)
    private Line[] lines;

    //Public attribute
    public Mesh Mesh { get { return mesh; } }

    public SplitscreenLineMesher(int pairCount)
    {
        //Init vertpoints array
        lines = new Line[pairCount];

        //Create lines
        for (int i = 0; i < pairCount; i++)
        {
            lines[i] = new Line();
        }

        //Create mesh
        mesh = new Mesh();
        mesh.MarkDynamic();

        //Create lists for mesh generation
        verts = new List<Vector3>();
        tris = new List<int>();
    }

    public void CreateMesh(float thickness, List<SplitscreenDevider.Point> points, List<SplitscreenDevider.TargetPair> pairs, int pointCount, int[][] targetPairs)
    {
        SetLineThickness(thickness);
        CreateMesh(points, pairs, pointCount, targetPairs);
    }

    public void CreateMesh(float[] thicknesses, List<SplitscreenDevider.Point> points, List<SplitscreenDevider.TargetPair> pairs, int pointCount, int[][] targetPairs)
    {
        SetLineThickness(thicknesses);
        CreateMesh(points, pairs, pointCount, targetPairs);
    }

    public void CreateMesh(List<SplitscreenDevider.Point> points, List<SplitscreenDevider.TargetPair> pairs, int pointCount, int[][] targetPairs)
    {
        //Go through all points and find the vertecies of the lines
        SplitscreenDevider.Point point;
        SplitscreenDevider.TargetPair pair;

        for (int i = 0; i < pointCount; i++)
        {
            point = points[i];
            for (int j = 0; j < pairs.Count; j++)
            {
                pair = pairs[j];

                //Make sure the point is on the edge of the pair
                if (!point.IsSuitableForBooth(pair.target1, pair.target2, j)) continue;

                if (!point.IsSuitableVertex(pair.target1, targetPairs[pair.target1]) &&
                    !point.IsSuitableVertex(pair.target2, targetPairs[pair.target2])) continue;

                lines[j].SetPoint(point.position);
            }
        }

        //Clear everything
        mesh.Clear();
        verts.Clear();
        tris.Clear();

        //Add all lines to the mesh
        for (int i = 0; i < lines.Length; i++)
        {
            //Generate the mesh
            AddLineToMesh(lines[i]);

            //Reset line
            lines[i].Reset();
        }

        //Build mesh
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    //Set uniform line thickness
    private void SetLineThickness(float thickness)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].thickness = thickness;
        }
    }

    //Set individual line thicknesses
    private void SetLineThickness(float[] thicknesses)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].thickness = thicknesses[i];
        }
    }

    private void AddLineToMesh(Line line)
    {
        if (!line.IsSet) return;

        int v = verts.Count;

        float x1 = line.point1.x, x2 = line.point2.x, y1 = line.point1.y, y2 = line.point2.y;
        float dx = line.direction.x, dy = line.direction.y;
        float nx = line.normal.x, ny = line.normal.y;
        float t = line.thickness;

        //Create verticies

        //verts.Add(line.point1 - line.direction * line.thickness * 0.5f + line.normal * line.thickness * 0.5f);
        verts.Add(new Vector3(x1 - dx * t * 0.5f + nx * t * 0.5f, y1 - dy * t * 0.5f + ny * t * 0.5f));
        //verts.Add(line.point1 - line.direction * line.thickness * 0.5f - line.normal * line.thickness * 0.5f);
        verts.Add(new Vector3(x1 - dx * t * 0.5f - nx * t * 0.5f, y1 - dy * t * 0.5f - ny * t * 0.5f));


        //verts.Add(line.point2 + line.direction * line.thickness * 0.5f - line.normal * line.thickness * 0.5f);
        verts.Add(new Vector3(x2 + dx * t * 0.5f - nx * t * 0.5f, y2 + dy * t * 0.5f - ny * t * 0.5f));
        //verts.Add(line.point2 + line.direction * line.thickness * 0.5f + line.normal * line.thickness * 0.5f);
        verts.Add(new Vector3(x2 + dx * t * 0.5f + nx * t * 0.5f, y2 + dy * t * 0.5f + ny * t * 0.5f));

        //verts.Add(line.point1 + line.normal * line.thickness * 0.5f);
        //verts.Add(line.point1 - line.normal * line.thickness * 0.5f);

        //verts.Add(line.point2 - line.normal * line.thickness * 0.5f);
        //verts.Add(line.point2 + line.normal * line.thickness * 0.5f);

        //Create tris
        tris.Add(v + 2);
        tris.Add(v + 1);
        tris.Add(v + 0);

        tris.Add(v + 0);
        tris.Add(v + 3);
        tris.Add(v + 2);
    }

    private class Line
    {
        public Vector2 point1, point2;
        public Vector2 normal;
        public Vector2 direction;
        public float thickness = 1.0f;
        private bool p1Set = false, p2Set = false;

        public bool IsSet { get { return p1Set && p2Set; } }

        public Line()
        {
            point1 = new Vector2();
            point2 = new Vector2();
            normal = new Vector2();
            direction = new Vector2();
        }

        public void SetPoint(Vector2 point)
        {
            if (!p1Set)
            {
                point1 = point;
                p1Set = true;

                return;
            }
            point2 = point;
            p2Set = true;

            //set normal and direction
            direction.x = point2.x - point1.x;
            direction.y = point2.y - point1.y;
            direction.Normalize();

            normal.x = -direction.y;
            normal.y = direction.x;
        }

        public void Reset()
        {
            p1Set = false;
            p1Set = false;
        }

    }
}