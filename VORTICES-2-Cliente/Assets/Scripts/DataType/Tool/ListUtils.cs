using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ListUtils
{
    public static void Swap<T>(this List<T> list, int i, int j)
    {
        T temp = list[i];
        list[i] = list[j];
        list[j] = temp;
    }

    public static void Move<T>(this List<T> list, int oldIndex, int newIndex)
    {
        T item = list[oldIndex];
        list.RemoveAt(oldIndex);
        list.Insert(newIndex, item);
    }

    public static float nfmod(float a, float b)
    {
        return a - b * Mathf.Floor(a / b);
    }

    public static void InsertAtOrFill<T>(List<T> list, int index, T value)
    {
        while (list.Count <= index)
        {
            list.Add(default(T)); // Fill with default values
        }

        list[index] = value;
    }


}
