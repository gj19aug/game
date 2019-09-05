using System;
using UnityEngine;

public class MinMaxRangeAttribute : PropertyAttribute
{
    public float min;
    public float max;

    public MinMaxRangeAttribute(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
}

[Serializable]
public struct Range
{
    public float min;
    public float max;
}

[Serializable]
public struct IntRange
{
    public int min;
    public int max;
}
