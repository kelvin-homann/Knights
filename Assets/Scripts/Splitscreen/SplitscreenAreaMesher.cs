using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitscreenAreaMesher {

    //Meshes for the target screen areas
    private Mesh[] meshes;

    //List for verticies and triangles
    private List<Vector3> verts;
    private List<int> tris;

    //Array that contains the list of points used as verticies for each mesh
    private List<Vector2>[] vertPoints;
    private List<Vector2> orderedPoints;

    //The center points of the screen areas
    private Vector2[] centers;

    //Public attribute
    public Mesh[] Meshes { get { return meshes; } }
    public Vector2[] Centers { get { return centers; } }

    public SplitscreenAreaMesher(int targetCount)
    {
        //Init vertpoints array
        vertPoints = new List<Vector2>[targetCount];

        //Init centers array
        centers = new Vector2[targetCount];

        //Create meshes
        meshes = new Mesh[targetCount];
        for (int i = 0; i < targetCount; i++)
        {
            meshes[i] = new Mesh();
            meshes[i].MarkDynamic();
            vertPoints[i] = new List<Vector2>();
        }

        //Create lists for mesh generation
        verts = new List<Vector3>();
        tris = new List<int>();
        orderedPoints = new List<Vector2>();
    }

    public void CreateAllMeshes(List<SplitscreenDevider.Point> points, int pointCount, int[][] targetPairs)
    {
        //Go through all points and add the ones suitable as verticies to the vertPoints list
        SplitscreenDevider.Point point;
        for (int i = 0; i < pointCount; i++)
        {
            point = points[i];
            for (int j = 0; j < meshes.Length; j++)
            {
                if (point.IsSuitableVertex(j, targetPairs[j])) vertPoints[j].Add(point.position);
            }
        }

        //Create meshes
        for (int i = 0; i < meshes.Length; i++)
        {
            //Find center point of verts
            //FindCenter(vertPoints[i], out centers[i]);

            //Generate the mesh
            GenerateMesh(vertPoints[i], meshes[i]);

            centers[i] = meshes[i].bounds.center;

            //Finally clear the vert points
            vertPoints[i].Clear();
        }
    }

    public void GenerateMesh(List<Vector2> points, Mesh mesh)
    {
        //Clear everything
        mesh.Clear();
        verts.Clear();
        tris.Clear();
        //orderedPoints.Clear();

        //Order points
        //ConvexHull(points, orderedPoints);
        OrderByAngle(points);

        //Create verticies and triangles
        for (int i = 0; i < points.Count; i++)
        {
            //Add vert
            verts.Add(points[i]);

            //Add triangle
            if (i == 0 || i >= points.Count - 1) continue;
            tris.Add(0);
            tris.Add(i + 1);
            tris.Add(i);
        }

        //Build mesh
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    //Find center of polygon
    private void FindCenter(List<Vector2> points, out Vector2 center)
    {
        int n = points.Count;

        //Get bounds of polygon
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        Vector2 point;

        //Add all positions
        for (int i = 0; i < n; i++)
        {
            point = points[i];
            minX = Mathf.Min(minX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxX = Mathf.Max(maxX, point.x);
            maxY = Mathf.Max(maxY, point.y);
        }

        //Calculate bounds width/height
        float w = maxX - minX;
        float h = maxY - minY;

        //Center is center of bounds
        center = new Vector2(minX + w / 2, minY + h / 2);
    }

    private static int Orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);

        //Points are on a line (coliniear)
        if (val == 0) return 0;

        //1: Clockwise 2: Counterclockwise
        return (val > 0) ? 1 : 2;
    }

    public static void ConvexHull(List<Vector2> points, List<Vector2> hull)
    {
        int n = points.Count;

        // There must be at least 3 points 
        if (n < 3) return;

        // Initialize Result (Even though this is a convex hull algorithm
        // the result will still always contain all points
        //hull.Clear();

        // Find the leftmost point 
        int l = 0;
        for (int i = 1; i < n; i++)
            if (points[i].x < points[l].x)
                l = i;

        // Start from leftmost point, keep moving  
        // counterclockwise until reach the start point 
        // again. This loop runs O(h) times where h is 
        // number of points in result or output. 
        int p = l, q;
        do
        {
            // Add current point to result 
            hull.Add(points[p]);

            // Search for a point 'q' such that  
            // orientation(p, x, q) is counterclockwise  
            // for all points 'x'. The idea is to keep  
            // track of last visited most counterclock- 
            // wise point in q. If any point 'i' is more  
            // counterclock-wise than q, then update q. 
            q = (p + 1) % n;

            for (int i = 0; i < n; i++)
            {
                // If i is more counterclockwise than  
                // current q, then update q 
                if (Orientation(points[p], points[i], points[q]) == 2)
                {
                    q = i;
                } 
            }

            // Now q is the most counterclockwise with 
            // respect to p. Set p as q for next iteration,  
            // so that q is added to result 'hull' 
            p = q;

        } while (p != l && hull.Count <= n);  // While we don't come to first point 
    }

    //Orders points based on their angle to the center point
    private void OrderByAngle(List<Vector2> points)
    {
        int n = points.Count;
        Vector2 point;

        //Find center point
        float centerX = 0, centerY = 0;

            //Add all positions
        for (int i = 0; i < n; i++)
        {
            point = points[i];
            centerX += point.x;
            centerY += point.y;
        }

            //Devide by count
        centerX /= n; centerY /= n;

        //Calculate angels
        float[] angles = new float[n];

        for (int i = 0; i < n; i++)
        {
            point = points[i];
            angles[i] = Mathf.Atan2(point.y - centerY, point.x - centerX);
        }

        //Order by angle using insertion sort
        InsertionSort(points, angles);
    }

    //Implementation of insertion sort, sorting points based an angles in seperate array
    //(Based on pseudo code from Wikipedia)
    private void InsertionSort(List<Vector2> points, float[] angles)
    {
        Vector2 temp;
        int i = 1, j;
        float x;

        while(i < angles.Length)
        {
            x = angles[i];
            temp = points[i];
            j = i - 1;
            while(j >= 0 && angles[j] > x)
            {
                angles[j + 1] = angles[j];
                points[j + 1] = points[j];
                j--;
            }
            angles[j + 1] = x;
            points[j + 1] = temp;
            i++;
        }
    }
}
