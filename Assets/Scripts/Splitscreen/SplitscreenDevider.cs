using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitscreenDevider : MonoBehaviour
{
    //Singelton instance
    private static SplitscreenDevider instance;

    //Array with target transforms and Screen (Camera) Object
    public SplitscreenCamera[] targets;

    //The dynamic virtual screen used to calculate target postions
    private static VirtualScreen screen;
    public static VirtualScreen Screen { get { return screen; } }

    //The width/height of the target screen in pixels (i.e. initial pixel w/h of main camera)
    private static Vector2 targetResolution;
    public static Vector2 TargetResolution { get { return targetResolution; } }

    //Public properties related to line rendering
    [System.Serializable]
    public struct LineProperties
    {
        public float thickness;
        public AnimationCurve thicknessCurve;
    }
    public LineProperties lines;

    //Public properties used for debugging
    [System.Serializable]
    public struct DebugProperties
    {
        //Debug
        [Range(1, 4)]
        public int targetToDebug;
    }
    public DebugProperties debug;

    //List of target pairs. Each pair stores information about connection vector and perpendicular edge
    private List<TargetPair> pairs;

    //List containing possible edge points/vertecies for the screen area mesh
    private List<Point> points;
    private int currentPointCount;

    //Array specifing with pairs include with target (i.e. targetPairs[0] contains all pairs that include target 0)
    private int[][] targetPairs;

    //Mesher to create the screen area mesh and line mesh
    private SplitscreenAreaMesher areaMesher;
    private SplitscreenLineMesher lineMesher;

    //Renderer to render the meshes into a mask texture
    private SplitscreenMaskRenderer maskRenderer;

    //The virtual screens of the targeted splitscreen cameras
    private VirtualScreen[] targetScreens;
    private VirtualScreen[] TargetScreens
    {
        get
        {
            if(targetScreens == null)
            {
                targetScreens = new VirtualScreen[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                {
                    targetScreens[i] = targets[i].VirtualScreen;
                }
            }
            return targetScreens;
        }
    }

    //Assign singelton reference
    private void Awake()
    {
        instance = this;

        //TODO: Dont hard code this
        //Set player count
        SetPlayerCount(Mathf.Max(1, Player.ActivePlayerCount));        
    }

    //Initialization
    private void Start()
	{
        //<Target count> choose 2 -> number of possible pairs
        int pairCount = NChooseK(targets.Length, 2);
        pairs = new List<TargetPair>();

        //Generate int array with pairs foreach target
        targetPairs = new int[targets.Length][];
        List<int>[] targetPairsLists = new List<int>[targets.Length];
        for (int i = 0; i < targetPairsLists.Length; i++)
        {
            targetPairsLists[i] = new List<int>();
        }

        //Create target pairs
        for (int i = 0; i < targets.Length-1; i++)
        {
            for (int j = i+1; j < targets.Length; j++)
            {
                //Add pair
                TargetPair pair = new TargetPair();
                pair.target1 = i; pair.target2 = j;
                pairs.Add(pair);

                //Add pair to target pair lists
                targetPairsLists[i].Add(pairs.Count - 1);
                targetPairsLists[j].Add(pairs.Count - 1);
            }
        }

        //Assign target pairs to list
        for(int i = 0; i < targets.Length; i++)
        {
            targetPairs[i] = new int[targetPairsLists[i].Count];
            for(int j = 0; j < targetPairsLists[i].Count; j++)
            {
                targetPairs[i][j] = targetPairsLists[i][j];
            }
        }

        //Initialize points
        points = new List<Point>();
        currentPointCount = 0;

        //Initialize mesher and mask renderer
        areaMesher = new SplitscreenAreaMesher(targets.Length);
        lineMesher = new SplitscreenLineMesher(pairs.Count);
        maskRenderer = new SplitscreenMaskRenderer();

        //Initialize screen and set target resolution
        screen = new VirtualScreen(Camera.main.aspect);
        targetResolution = new Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height);
	}
	
	//Update component
	private void Update()
	{
        //Recalculate the screen
        screen.GetBestFit(TargetScreens);

        //Update pair values
        UpdatePairs();

        //Add all points
        UpdatePoints();

        //Map points back from virtual screen space to target resolution
        NormalizePoints();

        //Create meshes
        lineMesher.CreateMesh(lines.thickness, points, pairs, currentPointCount, targetPairs);
        areaMesher.CreateAllMeshes(points, currentPointCount, targetPairs);

        //Update camera offsets
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i].UpdateCameraOffset(areaMesher.Meshes[i]);
        }
    }

    //Adjust the target count
    public static void SetPlayerCount(int count)
    {
        SplitscreenCamera[] newTargets = new SplitscreenCamera[count];
        for (int i = 0; i < instance.targets.Length; i++)
        {
            if (i < count)
                newTargets[i] = instance.targets[i];
            else
            {
                instance.targets[i].Disable();
            }
        }
        instance.targets = newTargets;
    }

    private void OnRenderObject()
    {
        //Render the mask using meshes
        maskRenderer.RenderMask(areaMesher, lineMesher);
    }

    private void UpdatePairs()
    {
        Vector2 pos1, pos2;
        for (int i = 0; i < pairs.Count; i++)
        {
            pos1 = GetScreenSpacePos(pairs[i].target1);
            pos2 = GetScreenSpacePos(pairs[i].target2);
            pairs[i].Calculate(pos1, pos2);
        }
    }

    private void UpdatePoints()
    {
        currentPointCount = 0;

        //Intersection points
        Vector2 intersection1;
        Vector2 intersection2;

        //Bounds
        float w = screen.Width;
        float h = screen.Height;

        //Bound corner points
        AddPoint(new Vector2(0, 0));
        AddPoint(new Vector2(w, 0));
        AddPoint(new Vector2(w, h));
        AddPoint(new Vector2(0, h));

        //Bound intersections
        for (int i = 0; i < pairs.Count; i++)
        {
            GetBoundIntersection(pairs[i].midPoint, pairs[i].edge, w, h, out intersection1, out intersection2);
            AddPoint(intersection1).SetBothSides(i);
            AddPoint(intersection2).SetBothSides(i);
        }

        //Pair edge intersections
        Point p;
        int commonTarget;
        for (int i = 0; i < pairs.Count - 1; i++)
        {
            for (int j = i + 1; j < pairs.Count; j++)
            {
                commonTarget = pairs[i].GetCommonTarget(pairs[j]);
                if (commonTarget == -1) continue;

                if (GetIntersectionPoint(pairs[i].midPoint, pairs[i].edge, pairs[j].midPoint, pairs[j].edge, out intersection1))
                {
                    if (intersection1.x < 0 || intersection1.y < 0 || intersection1.x > w || intersection1.y > h) continue;
                    p = AddPoint(intersection1);
                    p.SetBothSides(i);
                    p.SetBothSides(j);
                    p.SetParent(commonTarget);
                }
            }
        }

        //Update point sides
        for(int i = 0; i < currentPointCount; i++)
        {
            p = points[i];
            for(int j = 0; j < pairs.Count; j++)
            {
                if (p.targetPairSides[j] > 0) continue;
                p.SetSide(pairs[j].IsOnTargetSide(p.position), j);
            }
        }

    }

    //Map points from virtual screen space to target resolution
    private void NormalizePoints()
    {
        Point point;
        for (int i = 0; i < currentPointCount; i++)
        {
            point = points[i];
            point.position.x = (point.position.x / screen.Width) * targetResolution.x;
            point.position.y = (point.position.y / screen.Height) * targetResolution.y;
        }
    }

    private float[] GetLineThickness()
    {
        float[] thicknesses = new float[pairs.Count];

        TargetPair pair;
        float distance;

        for (int i = 0; i < thicknesses.Length; i++)
        {
            pair = pairs[i];
            distance = Vector3.Distance(targets[pair.target1].target.position,
                targets[pair.target2].target.position);

            thicknesses[i] = lines.thicknessCurve.Evaluate(distance);
        }

        return thicknesses;
    }

    private Point AddPoint(Vector2 position)
    {
        //Add new point if there are not enough points already
        if (currentPointCount >= points.Count) points.Add(new Point(pairs.Count));

        Point p = points[currentPointCount];

        //Set point position and reset side information
        p.position = position;
        for(int i = 0; i < pairs.Count; i++)
        {
            p.targetPairSides[i] = 0;
        }

        //Reset parent
        p.parent = 0;

        //Increment current point count
        currentPointCount++;

        return p;
    }


    private Vector2 GetScreenSpacePos(int target)
    {
        Vector2 position = targets[target].VirtualScreen.Position;
        position = screen.TransformScreenPoint(position);

        return position;
    }

    private bool GetIntersectionPoint(Vector2 p1, Vector2 v1, Vector2 p2, Vector2 v2, out Vector2 intersection)
    {
        float c1 = v1.y * p1.x - v1.x * p1.y;
        float c2 = v2.y * p2.x - v2.x * p2.y;
        float det = v1.y * (-v2.x) - v2.y * (-v1.x);

        if(det == 0)
        {
            //Parallel
            intersection = Vector2.zero;
            return false;
        }

        float x = (-v2.x * c1 + v1.x * c2) / det;
        float y = (v1.y * c2 - v2.y * c1) / det;
        intersection = new Vector2(x, y);

        return true;
    }

    private void GetBoundIntersection(Vector2 p, Vector2 v, float w, float h, out Vector2 intersection1, out Vector2 intersection2)
    {
        //Line is parallel to x/y axis
        if(v.x == 0 || v.y == 0)
        {
            //Set intersections by simply setting x/y coord to bound
            intersection1 = new Vector2(v.y == 0 ? 0 : p.x, v.x == 0 ? 0 : p.y);
            intersection2 = new Vector2(v.y == 0 ? w : p.x, v.x == 0 ? h : p.y);
            return;
        }

        //Parameter used to determine intersection point P(X, Y) from line -> X = P.x + a*v.x, Y = P.y + b*v.y
        float a1 = (0 - p.x) / v.x;
        float a2 = (w - p.x) / v.x;

        float b1 = (0 - p.y) / v.y;
        float b2 = (h - p.y) / v.y;

        //Calculate potential x/y coords
        float y1 = p.y + a1 * v.y;
        float y2 = p.y + a2 * v.y;

        float x1 = p.x + b1 * v.x;
        float x2 = p.x + b2 * v.x;

        //Set intersection point if coord is valid
        intersection1 = Vector2.zero;
        intersection2 = Vector2.zero;
        bool first = true;
        if (y1 >= 0 && y1 <= h) SetIntersection(new Vector2(0, y1), ref intersection1, ref intersection2, ref first);
        if (y2 >= 0 && y2 <= h) SetIntersection(new Vector2(w, y2), ref intersection1, ref intersection2, ref first);
        if (x1 >= 0 && x1 <= w) SetIntersection(new Vector2(x1, 0), ref intersection1, ref intersection2, ref first);
        if (x2 >= 0 && x2 <= w) SetIntersection(new Vector2(x2, h), ref intersection1, ref intersection2, ref first);
    }

    //Helper method to set the two bound intersection points
    private void SetIntersection(Vector2 point, ref Vector2 i1, ref Vector2 i2, ref bool first)
    {
        if (first) i1 = point;
        else i2 = point;

        first = false;
    }

    public class TargetPair
    {
        public int target1, target2;
        public Vector2 midPoint;
        public Vector2 edge;
        public Vector2 connectionVector;

        public void Calculate(Vector2 pos1, Vector2 pos2)
        {
            connectionVector = pos2 - pos1;
            midPoint = pos1 + connectionVector * 0.5f;
            edge = Vector2.Perpendicular(connectionVector).normalized;
        }

        //Returns the index of the target the given point belongs to
        public int IsOnTargetSide(Vector2 pos)
        {
            //float dot = Vector2.Dot(pos - midPoint, connectionVector);
            float dot = (pos.x - midPoint.x) * connectionVector.x + (pos.y - midPoint.y) * connectionVector.y;
            if (dot == 0) return -1; //point is on the edge = point is suitable for target1 and target2

            return dot < 1 ? target1 : target2;
        }

        public int GetCommonTarget(TargetPair pair)
        {
            if (target1 == pair.target1 || target1 == pair.target2) return target1;
            if (target2 == pair.target1 || target2 == pair.target2) return target2;
            return -1;
        }
    }

    public class Point
    {
        public Vector2 position;

        //This array stores the side this point belongs to relative to each target pair (edge)
        //It is either 0 (=undefined), 1 (=both targets) or <targetIndex>+2
        public byte[] targetPairSides;

        //The target this points originates from
        //Is either 0 = corner point (no parent) or <targetIndex>+1
        public byte parent;
        public bool IsCorner { get { return parent == 0; } }

        public Point(int targetPairsCount, int parentTarget=-1)
        {
            targetPairSides = new byte[targetPairsCount];
            parent = (byte)(parentTarget + 1);
        }

        //Returns true if the point is on the targets side relative to the target pair
        public bool IsOnTargetSide(int target, int targetPair)
        {
            if (targetPairSides[targetPair] == 1) return true;
            return targetPairSides[targetPair] == (byte)(target+2);
        }

        public bool IsSuitableVertex(int target, int[] pairs)
        {
            if (!IsChild(target)) return false;

            int side;
            for (int i = 0; i < pairs.Length; i++)
            {
                side = targetPairSides[pairs[i]];
                if (side != 1 && side != (target + 2)) return false;
            }
            return true;
        }

        public bool IsSuitableForBooth(int target1, int target2, int pair)
        {
            return (IsChild(target1) || IsChild(target2)) && targetPairSides[pair] == 1;
        }

        public void SetSide(int target, int targetPair)
        {
            targetPairSides[targetPair] = (byte)(target + 2);
        }

        public void SetBothSides(int targetPair)
        {
            targetPairSides[targetPair] = 1;
        }

        public void SetParent(int parentTarget) 
        {
            parent = (byte)(parentTarget + 1);
        }

        public bool IsChild(int target)
        {
            return parent == (byte)0 || parent == ((byte)target + (byte)1);
        }
    }

    //Fast n choose k algorithm
    private int NChooseK(int n, int k)
    {
        int result = 1;
        for(int i = 1; i <= k; i++)
        {
            result *= n - (k - 1);
            result /= i;
        }
        return result;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        float w = screen.Width, h = screen.Height;
        int target = debug.targetToDebug - 1;
        Rect sr = screen.ScreenRect;

        //Bounds
        //Gizmos.color = Color.blue;
        //Gizmos.DrawLine(new Vector2(0, 0), new Vector2(0, h));
        //Gizmos.DrawLine(new Vector2(0, 0), new Vector2(w, 0));
        //Gizmos.DrawLine(new Vector2(0, h), new Vector2(w, h));
        //Gizmos.DrawLine(new Vector2(w, 0), new Vector2(w, h));

        /*
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector2(sr.xMin, sr.yMin), new Vector2(sr.xMin, sr.yMax));
        Gizmos.DrawLine(new Vector2(sr.xMin, sr.yMax), new Vector2(sr.xMax, sr.yMax));
        Gizmos.DrawLine(new Vector2(sr.xMax, sr.yMax), new Vector2(sr.xMax, sr.yMin));
        Gizmos.DrawLine(new Vector2(sr.xMax, sr.yMin), new Vector2(sr.xMin, sr.yMin));

        Gizmos.color = Color.red;
        //Virtual screens of targets
        for (int i = 0; i < targets.Length; i++)
        {
            sr = targets[i].VirtualScreen.ScreenRect;
            Gizmos.DrawLine(new Vector2(sr.xMin, sr.yMin), new Vector2(sr.xMin, sr.yMax));
            Gizmos.DrawLine(new Vector2(sr.xMin, sr.yMax), new Vector2(sr.xMax, sr.yMax));
            Gizmos.DrawLine(new Vector2(sr.xMax, sr.yMax), new Vector2(sr.xMax, sr.yMin));
            Gizmos.DrawLine(new Vector2(sr.xMax, sr.yMin), new Vector2(sr.xMin, sr.yMin));
        }
        */
        //DEBUG Only draw virtual screens
        //return;

        //Connection and edges
        Gizmos.color = Color.grey;
        Vector2 pos1;
        foreach (var pair in pairs)
        {
            Gizmos.color = Color.grey;
            pos1 = GetScreenSpacePos(pair.target1);
            Gizmos.DrawLine(pos1, pos1 + (pair.midPoint - pos1) * 2);

            if (pair.target1 == target || pair.target2 == target)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pair.midPoint + pair.edge * 1000, pair.midPoint - pair.edge * 1000);
            }
        }

        //Target
        Gizmos.color = Color.red;
        for (int i = 0; i < targets.Length; i++)
        {
            Gizmos.DrawSphere(GetScreenSpacePos(i), 4f);
        }

        //Points

        Point p;
        Gizmos.color = Color.green;
        for (int i = 0; i < currentPointCount; i++)
        {
            p = points[i];
            if (!p.IsChild(target)) continue;
            bool vertex = p.IsSuitableVertex(target, targetPairs[target]);
            Gizmos.color = vertex ? Color.yellow : Color.green;
            Gizmos.DrawSphere(p.position, vertex ? 4f : 3f);
        }

        //Mesh
        Color c;
        for (int i = 0; i < targets.Length; i++)
        {
            c = debugColors[i];
            c.a = 0.5f;
            Gizmos.color = c;
            Gizmos.DrawMesh(areaMesher.Meshes[i]);
        }

        //Lines
        Gizmos.color = Color.white;
        Gizmos.DrawMesh(lineMesher.Mesh);

    }
    private Color[] debugColors = { Color.red, Color.blue, Color.green, Color.yellow };
}

