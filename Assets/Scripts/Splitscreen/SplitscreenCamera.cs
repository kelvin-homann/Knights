using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SplitscreenCamera : MonoBehaviour {

    //Static list containing all active splitscreen cameras
    private static SplitscreenCamera[] cameras;
    public static SplitscreenCamera[] Cameras { get { return cameras; } }
    private static SplitscreenCamera main;

    //Render texture this camera will render to
    public RenderTexture renderTexture;
    public Vector2 offset;

    //Target to track (player)
    public Transform target;
    public Vector3 targetOffset;
    public bool autoOffset;

    [Range(0.01f, 5f)]
    public float lerpTime = 1;
    public float zoom = 1.0f;
    public AnimationCurve areaBasedZoomCurve;

    private new Camera camera;
    private bool isMain = false;
    public int cameraIndex = -1;

    //Virtual screen that represents this camera
    private VirtualScreen virtualScreen;
    private float maxArea;
    private float currentArea;

    private Vector2 extraOffset = Vector2.zero;

    //Public readonly reference to render texture
    public RenderTexture Texture { get { return renderTexture; } }
    public static RenderTexture MainTexture { get { return main.Texture; } }
    public bool IsMain { get { return IsMain; } }
    public int CameraIndex { get { return cameraIndex; } }
    public VirtualScreen VirtualScreen { get { return virtualScreen; } }

    //Initialize main camera before all other cameras
    private void Awake()
    {
        //Calculate offset
        if (autoOffset) targetOffset = transform.position - target.position;

        if (gameObject.CompareTag("MainCamera")) Start();
    }

    // Use this for initialization
    void Start ()
    {
        //Stop main camera from calling start twice
        if (isMain) return;
        
        //Init camera list
        if (cameras == null) cameras = new SplitscreenCamera[4];
        cameras[cameraIndex] = this;

        //Grab camera reference
        camera = GetComponent<Camera>();

        //Create virtual screen; set area
        virtualScreen = new VirtualScreen(camera.pixelWidth, camera.pixelHeight);
        maxArea = virtualScreen.Width * virtualScreen.Height;
        currentArea = maxArea;

        //Set the main camera
        if (Camera.main == camera)
        {
            isMain = true;
            main = this;
            return;
        }
        
        //Only create render texture if the camera is not the main camera
        
        //Create render texture
        renderTexture = new RenderTexture(camera.pixelWidth, camera.pixelHeight, 16, RenderTextureFormat.DefaultHDR);
        renderTexture.Create();

        //Tell the camera to render into the texture instead of to the screen
        camera.targetTexture = renderTexture;

        //Add the texture to the composite material
        SplitscreenCompositor.AddCameraTexture(renderTexture, cameraIndex);
	}    

    private void LateUpdate()
    {
        //Set camera zoom
        UpdateZoom();

        //DEBUG Camera offset
        SplitscreenCompositor.SetTextureOffset(offset, CameraIndex);

        Vector3 targetPos = target.position - new Vector3(extraOffset.x, 0, extraOffset.y);

        //Camera position
        Vector3 position = targetPos + targetOffset * zoom;

        //Camera rotation (look at target)
        Quaternion rotation = Quaternion.LookRotation(targetPos - position);

        //Smooth interpolation for position and rotation
        float lerpValue = 1 / lerpTime * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, position, lerpValue);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, lerpValue);

        //Update virtual screen
        virtualScreen.UpdatePosition(target.position);
    }

    //Calculate the offset for this virtual camera to center the target inside the screen area of this camera
    //public void UpdateCameraOffset(Vector2 screenAreaCenter)
    //{
    //    //Position of this camera on the master virtual screen
    //    Vector2 pos = SplitscreenDevider.Screen.TransformScreenPoint(VirtualScreen.Position);

    //    //Absolute offset in master screen space
    //    //Vector2 offset = new Vector2(SplitscreenDevider.Screen.Width / 2 - screenAreaCenter.x, SplitscreenDevider.Screen.Height / 2 - screenAreaCenter.y);
    //    Vector2 centerOffset = new Vector2(VirtualScreen.Width / 2 - screenAreaCenter.x, VirtualScreen.Height / 2 - screenAreaCenter.y);
    //    centerOffset.x /= VirtualScreen.Width;
    //    centerOffset.y /= VirtualScreen.Height;

    //    Vector2 posOffset = new Vector2(SplitscreenDevider.Screen.Width / 2 - pos.x, SplitscreenDevider.Screen.Height / 2 - pos.y);
    //    posOffset.x /= SplitscreenDevider.Screen.Width;
    //    posOffset.y /= SplitscreenDevider.Screen.Height;

    //    //this.offset = new Vector2(offset.x / SplitscreenDevider.Screen.Width, offset.y / SplitscreenDevider.Screen.Height);
    //    this.offset = new Vector2(posOffset.x, centerOffset.y);
    //}

    public void UpdateCameraOffset(Mesh screenAreaMesh)
    {
        Vector3[] areaVerts = screenAreaMesh.vertices;
        Vector2 master = SplitscreenDevider.TargetResolution;
        Vector2 masterCenter = master / 2;

        Vector2 areaCenter = GetAreaCentroid(areaVerts, out currentArea);

        float availableX = master.x - screenAreaMesh.bounds.size.x;
        float availableY = master.y - screenAreaMesh.bounds.size.y;

        Vector2 offset = areaCenter - masterCenter;
        
        float clampedX = Mathf.Clamp(offset.x, -availableX, availableX);
        float clampedY = Mathf.Clamp(offset.y, -availableY, availableY);

        extraOffset = new Vector2((offset.x - clampedX) / master.x * 16, (offset.y - clampedY) / master.y * 9);

        offset.x = clampedX;
        offset.y = clampedY;

        //Debug.Log("Master (w/h): " + master.x + "|" + master.y + "  Area Center (x/y): " + areaCenter.x + "|" + areaCenter.y);
        this.offset = Vector2.Lerp(this.offset, new Vector2(-offset.x / master.x, -offset.y / master.y), 1 / lerpTime * Time.deltaTime);
    }

    public void UpdateZoom()
    {
        //Calculate camera zoom based on current screen area
        float normArea = currentArea / maxArea;
        zoom = areaBasedZoomCurve.Evaluate(normArea);
    }

    //Convert a wolrd position to screen position acounting for screen offset
    public Vector2 WorldToScreenPoint(Vector3 pos)
    {
        Vector3 worldPos = camera.WorldToScreenPoint(pos);
        return new Vector2(worldPos.x - offset.x * VirtualScreen.Width, worldPos.y - offset.y * VirtualScreen.Height);
    }

    //Disable the splitcreen camera
    public void Disable()
    {
        target.GetComponent<PlayerController>().Disable();
        gameObject.SetActive(false);
    }

    private float GetCenterAtCoord(Mesh areaMesh, float Y, bool flip)
    {
        //Output
        float X = 0;

        //Number of valid intersections
        float intersectionCount = 0;

        //Copy of vert array
        Vector3[] verts = areaMesh.vertices;

        //Working vars for points of line segment
        float x1, x2, y1, y2;
        float xOut;
        int i2;

        for (int i = 0; i < verts.Length; i++)
        {
            //Assign points

            //First point is current vertex
            x1 = verts[i].x;
            y1 = verts[i].y;

            //Second point is next vertex (or first vertex if current vertex is the last vertex)
            i2 = i + 1 >= verts.Length ? 0 : i + 1;
            x2 = verts[i2].x;
            y2 = verts[i2].y;

            //Check intersection
            if(FindSecondCoordOnLine(x1, x2, y1, y2, Y, out xOut, flip))
            {
                X += xOut;
                intersectionCount++;
            }
        }
        return X / intersectionCount;
    }

    //Given a line (defined by two points) this function calculates the X coord of the point on the line at the given Y coord
    private bool FindSecondCoordOnLine(float x1, float x2, float y1, float y2, float Y, out float X)
    {
        //Default output
        X = 0;

        //Check if line is horizontal
        if (y1 == y2) return false;

        //Check if given Y is within bounds of line
        if (Mathf.Min(y1, y2) > Y || Mathf.Max(y1, y2) < Y) return false;

        //Calculate factor t based on Y
        float t = (Y - y1) / (y2 - y1);

        //Calculate X using factor t
        X = x1 + t * (x2 - x1);

        return true;
    }

    //Allow flipping (find Y for given X)
    private bool FindSecondCoordOnLine(float x1, float x2, float y1, float y2, float Y, out float X, bool flip)
    {
        if(flip)
        {
            return FindSecondCoordOnLine(y1, y2, x1, x2, Y, out X);
        }
        return FindSecondCoordOnLine(x1, x2, y1, y2, Y, out X);
    }

    //Get center of area mesh (avrg. vert pos)
    private Vector2 GetAreaCenter(Vector3[] verts)
    {
        float x = 0, y = 0;
        for (int i = 0; i < verts.Length; i++)
        {
            x += verts[i].x;
            y += verts[i].y;
        }

        return new Vector2(x, y) / verts.Length;
    }

    private Vector2 GetAreaCentroid(Vector3[] verts, out float A)
    {
        Vector3 v;
        Vector3 v1;

        A = 0;
        float x = 0, y = 0;

        for (int i = 0; i < verts.Length; i++)
        {
            v = verts[i];
            v1 = i < verts.Length - 1 ? verts[i+1] : verts[0];

            A += (v.x * v1.y - v1.x * v.y);
            x += (v.x + v1.x) * (v.x * v1.y - v1.x * v.y);
            y += (v.y + v1.y) * (v.x * v1.y - v1.x * v.y);
        }

        A /= 2;
        x /= A * 6;
        y /= A * 6;

        return new Vector2(x, y);
    }
}
