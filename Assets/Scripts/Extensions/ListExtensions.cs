using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ListExtensions
{
    public static List<T> Clone<T>(this List<T> oldList)
    {
        var newList = new List<T>(oldList.Capacity);
        newList.AddRange(oldList);
        return newList;
    }
}
