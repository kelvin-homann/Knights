using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorUtil {

    //Create project folder structure if it does not exist already
    [MenuItem("Tools/Create Folder Structure")]
	public static void CreateProjectFolder()
    {
        string[] folders = { "Models", "Textures", "Prefabs", "Scripts", "Shaders", "Scenes", "Materials", "User" };
        foreach(var folder in folders)
        {
            if(!AssetDatabase.IsValidFolder("Assets/" + folder))
            {
                AssetDatabase.CreateFolder("Assets", folder);
            }
        }
    }

}
