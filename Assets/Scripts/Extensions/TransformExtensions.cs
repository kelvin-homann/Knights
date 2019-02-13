using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    public static Transform FindChildWithTag(this Transform parent, string tag)
    {
        for(int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if(child.tag.Equals(tag, System.StringComparison.CurrentCultureIgnoreCase))
                return child;
            if(child.childCount > 0)
                FindChildWithTag(child, tag);
        }
        return null;
    }

    public static Transform[] FindChildrenWithTag(this Transform parent, string tag)
    {
        List<Transform> childrenWithTag = new List<Transform>();

        for(int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if(child.tag.Equals(tag, System.StringComparison.CurrentCultureIgnoreCase))
                childrenWithTag.Add(child);
            if(child.childCount > 0)
                childrenWithTag.AddRange(FindChildrenWithTag(child, tag));
        }
        return childrenWithTag.ToArray();
    }

    //public static List<GameObject> FindObjectsWithTag(this Transform parent, string tag)
    //{
    //    List<GameObject> taggedGameObjects = new List<GameObject>();

    //    for(int i = 0; i < parent.childCount; i++)
    //    {
    //        Transform child = parent.GetChild(i);
    //        if(child.tag == tag)
    //        {
    //            taggedGameObjects.Add(child.gameObject);
    //        }
    //        if(child.childCount > 0)
    //        {
    //            taggedGameObjects.AddRange(FindObjectsWithTag(child, tag));
    //        }
    //    }
    //    return taggedGameObjects;
    //}
}
