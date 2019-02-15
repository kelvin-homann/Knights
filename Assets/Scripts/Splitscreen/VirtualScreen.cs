using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualScreen
{
    //Position of virtual screen (in virtual screen space)
    private Vector2 position;
    public Vector2 Position { get { return position; } }

    //Width, Height and Aspect Ratio of the virtual screen (matches camera if the virtual screen represents a camera)
    public float Width { get; private set; }
    public float Height { get; private set; }
    public float Aspect { get; private set; }

    public Rect ScreenRect
    {
        get
        {
            return new Rect(position.x - Width / 2, position.y - Height / 2, Width, Height);
        }
    }

    //Constructor for virtual screen as camera
    public VirtualScreen(float width, float height)
    {
        position = Vector2.zero;
        Width = width;
        Height = height;
        Aspect = width / height;
    }

    //Constructor for dynamic master virtual screen 
    public VirtualScreen(float aspect)
    {
        position = Vector2.zero;
        Width = 0;
        Height = 0;
        Aspect = aspect;
    }

    //Get the points position relative to the minimum corner of the virtual screen
    public Vector2 TransformScreenPoint(Vector2 point)
    {
        return new Vector2(point.x - position.x + Width / 2, point.y - position.y + Height / 2);
    }

    //Calculate the virtual screen position based on the current camera position
    public void UpdatePosition(Camera camera, Vector3 offset)
    {
        //Calculate virtual screen position based on world space origin (virtual screen is at 0,0 if camera target is at 0,0,0)
        Vector2 zeroInScreenSpace = camera.WorldToScreenPoint(Vector3.zero + offset);
        position.x = Width / 2 - zeroInScreenSpace.x;
        position.y = Height / 2 - zeroInScreenSpace.y;
    }

    public void UpdatePosition(Camera camera)
    {
        UpdatePosition(camera, Vector3.zero);
    }
    
    public void UpdatePosition(Vector3 target)
    {
        position.x = target.x * (Width / 16);
        position.y = target.z * (Height / 9);
    }

    //Get the smallest virtual screen that contains the given virtual screens
    public void GetBestFit(params VirtualScreen[] screens)
    {
        //Get min/max screen bounds
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        Rect screenRect;
        for(int i = 0; i < screens.Length; i++)
        {
            screenRect = screens[i].ScreenRect;
            minX = Mathf.Min(minX, screenRect.xMin);
            minY = Mathf.Min(minY, screenRect.yMin);
            maxX = Mathf.Max(maxX, screenRect.xMax);
            maxY = Mathf.Max(maxY, screenRect.yMax);
        }

        //Calculate new screen
        float w = maxX - minX;
        float h = maxY - minY;

        position.x = minX + w / 2;
        position.y = minY + h / 2;

        //Adjust w/h (make sure the new screen preserves the aspect ratio of this virtual screen)
        //w/h < aspect: Calculated screen is thinner -> adjust w
        if(w/h < Aspect)
        {
            w = Aspect * h;
        }
        //w/h > aspect: Calculated screen is wider -> adjust h
        else
        {
            h = w / Aspect;
        }

        //Set width/height
        Width = w;
        Height = h;
    }

}
