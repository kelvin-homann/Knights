using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SplitscreenDevider))]
public class SplitscreenCompositor : MonoBehaviour {

    public Color lineColor = Color.white;

    private new Camera camera;

    //Material that uses the shader that combines the different cameras based on a mask
    private static Material compositeMaterial;
    private static Material CompositeMaterial
    {
        get
        {
            if(compositeMaterial == null) compositeMaterial = new Material(Shader.Find("Hidden/SplitscreenComposite"));
            return compositeMaterial;
        }
    }

    private static Vector2 mainTexOffset;
    private static string[] cameraTextureNames = { "_MainTex", "_Camera2", "_Camera3", "_Camera4"};

	// Use this for initialization
	void Start ()
    {
        //Grab camera
        camera = GetComponent<Camera>();

        //Assign mask
        CompositeMaterial.SetTexture("_Mask", SplitscreenMaskRenderer.MaskTexture);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //Render compositetd cameras to screen
        //Source is the texture of the main camera (other cameras should be already assigned to the composite material)
        //Destination is the screen (actually still the main cameras texture)
        
        //Manual version of: Graphics.Blit(source, destination, CompositeMaterial);

        //Set _MainTex on material
        CompositeMaterial.SetTexture("_MainTex", source);

        //Set line color
        compositeMaterial.SetColor("_LineColor", lineColor);

        //Set render target and load projection
        Graphics.SetRenderTarget(destination);
        GL.PushMatrix();
        CompositeMaterial.SetPass(0);
        GL.LoadOrtho();

        //Set material pass
        
        
        //Render full screen quad
        GL.Begin(GL.QUADS);
        GL.TexCoord2(0, 0);
        GL.Vertex3(0, 0, 0);

        GL.TexCoord2(1, 0);
        GL.Vertex3(1, 0, 0);

        GL.TexCoord2(1, 1);
        GL.Vertex3(1, 1, 0);

        GL.TexCoord2(0, 1);
        GL.Vertex3(0, 1, 0);

        GL.End();

        //Reset matrix
        GL.PopMatrix();
    }

    //Add a camera texture to the composite material
    public static void AddCameraTexture(RenderTexture texture, int cameraIndex)
    {
        //Camera 0 (Main camera) does not need to add a texture and the shader only supports 4 cameras
        if (cameraIndex <= 0 || cameraIndex >= 4) return;

        //Set the texture on the material
        CompositeMaterial.SetTexture(cameraTextureNames[cameraIndex], texture);
    }

    public static void SetTextureOffset(Vector2 offset, int cameraIndex)
    {
        //if (cameraIndex == 0) mainTexOffset = offset;
        CompositeMaterial.SetTextureOffset(cameraTextureNames[cameraIndex], offset);
    }
}
