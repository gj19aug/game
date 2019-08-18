﻿using UnityEngine;

public static class RandomEx
{
    public static T Element<T>(T[] array)
    {
        int index = Random.Range(0, array.Length);
        return array[index];
    }
}

public static class Vector3Ex
{
    public static Vector3 SetX(this Vector3 v, float x)
    {
        v.x = x;
        return v;
    }

    public static Vector3 SetY(this Vector3 v, float y)
    {
        v.y = y;
        return v;
    }

    public static Vector3 SetZ(this Vector3 v, float z)
    {
        v.z = z;
        return v;
    }
}