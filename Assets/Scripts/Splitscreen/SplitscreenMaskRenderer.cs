using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplitscreenMaskRenderer {

    private static RenderTexture maskTexture;
    private Material unlitMaterial;
    private Color[] maskColors = { new Color(1, 0, 0, 0), new Color(0, 1, 0, 0), new Color(0, 0, 1, 0), new Color(0, 0, 0, 1) };

    private Vector3[] verts;
    private int[] tris;

    public static RenderTexture MaskTexture { get { return maskTexture; } }

	//Initialize material and render texture
	public SplitscreenMaskRenderer () {

        //Unlit material to render the screen area mesh
        unlitMaterial = new Material(Shader.Find("Hidden/SplitscreenMask"));

        //Render texture to store the mask
        maskTexture = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 16, RenderTextureFormat.ARGB32);
        maskTexture.Create();
	}

    public void RenderMask(SplitscreenAreaMesher areaMesher, SplitscreenLineMesher lineMesher=null)
    {
        //Set the render target to the mask texture
        Graphics.SetRenderTarget(maskTexture.colorBuffer, maskTexture.depthBuffer);

        //Store matrix and load orthographic projection
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, maskTexture.width, 0, maskTexture.height);

        GL.Clear(true, true, Color.clear);

        for (int i = 0; i < areaMesher.Meshes.Length; i++)
        {
            RenderMesh(areaMesher.Meshes[i], maskColors[i]);
        }

        //Lines
        if (lineMesher != null) RenderMesh(lineMesher.Mesh, Color.clear);

        //Reset matrix
        GL.PopMatrix();
    }

    private void RenderMesh(Mesh mesh, Color color)
    {
        //Get verts/tris
        verts = mesh.vertices;
        tris = mesh.triangles;

        unlitMaterial.SetColor("_Color", color);
        unlitMaterial.SetPass(0);

        //Begin GL in triangle mode
        GL.Begin(GL.TRIANGLES);

        //Draw mesh
        for (int i = 0; i < tris.Length; i++)
        {
            GL.Vertex(verts[tris[i]]);
        }

        //End GL
        GL.End();
    }
}
