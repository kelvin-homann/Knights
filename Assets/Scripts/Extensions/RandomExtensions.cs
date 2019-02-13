using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomExtensions
{
    public static T Choose<T>(this Random random, List<T> list)
    {
        if(list == null || list.Count == 0)
            return default(T);
        else if(list.Count == 1)
            return list[0];
        else
        {
            int randomIndex = Random.Range(0, list.Count);
            return list[randomIndex];
        }
    }
}
