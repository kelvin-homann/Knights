using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorUtil {

    [MenuItem("GameObject/Group %G")]
    public static void GroupGameObjects()
    {
        if (Selection.transforms == null || Selection.transforms.Length <= 0) return;

        GameObject group = new GameObject("New Group");
        foreach(Transform t in Selection.transforms)
        {
            t.SetParent(group.transform, true);
        }
        Selection.activeGameObject = group;
    }

    [MenuItem("GameObject/Select Children", priority = -10)]
    public static void SelectChildren()
    {
        GameObject active = Selection.activeGameObject;
        if (!active) return;

        List<Transform> children = new List<Transform>();
        foreach (Transform t in active.transform) children.Add(t);

        Selection.objects = children.ToArray();
        Selection.selectionChanged();
    }

    //private const string PLAYER_COUNT_PREFIX = "Tools/Player Count/";
    //private const string PLAYER_COUNT_1 = PLAYER_COUNT_PREFIX + "1 Player";
    //private const string PLAYER_COUNT_2 = PLAYER_COUNT_PREFIX + "2 Players";
    //private const string PLAYER_COUNT_3 = PLAYER_COUNT_PREFIX + "3 Players";
    //private const string PLAYER_COUNT_4 = PLAYER_COUNT_PREFIX + "4 Players";

    //[MenuItem(PLAYER_COUNT_1)]
    //public static void SetPlayers1() { SetPlayerCount(1); }
    //[MenuItem(PLAYER_COUNT_2)]
    //public static void SetPlayers2() { SetPlayerCount(2); }
    //[MenuItem(PLAYER_COUNT_3)]
    //public static void SetPlayers3() { SetPlayerCount(3); }
    //[MenuItem(PLAYER_COUNT_4)]
    //public static void SetPlayers4() { SetPlayerCount(4); }

    //public static void SetPlayerCount(int count)
    //{
    //    //Set menu check
    //    Menu.SetChecked(PLAYER_COUNT_1, count == 1);
    //    Menu.SetChecked(PLAYER_COUNT_2, count == 2);
    //    Menu.SetChecked(PLAYER_COUNT_3, count == 3);
    //    Menu.SetChecked(PLAYER_COUNT_4, count == 4);

    //    GameObject players = GameObject.Find("Players");
    //    GameObject cameras = GameObject.Find("Cameras");

    //    //Enable/Disable
    //    players.FindObject("Player 1").SetActive(1 <= count);
    //    players.FindObject("Player 2").SetActive(2 <= count);
    //    players.FindObject("Player 3").SetActive(3 <= count);
    //    players.FindObject("Player 4").SetActive(4 <= count);

    //    cameras.FindObject("Camera 2").SetActive(2 <= count);
    //    cameras.FindObject("Camera 3").SetActive(3 <= count);
    //    cameras.FindObject("Camera 4").SetActive(4 <= count);

    //    //Assign
    //    SplitscreenDevider devider = Camera.main.GetComponent<SplitscreenDevider>();
    //    devider.targets = new SplitscreenCamera[count];
    //    devider.targets[0] = devider.gameObject.GetComponent<SplitscreenCamera>();
    //    for (int i = 1; i < count; i++)
    //    {
    //        devider.targets[i] = cameras.FindObject("Camera " + (i+1)).GetComponent<SplitscreenCamera>();
    //    }
    //}

    
}

public static class Extensions
{
    public static GameObject FindObject(this GameObject parent, string name)
    {
        Transform[] trs = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in trs)
        {
            if (t.name == name)
            {
                return t.gameObject;
            }
        }
        return null;
    }
}

