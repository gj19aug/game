using UnityEngine;

public static class RandomEx
{
    public static T Element<T>(T[] array)
    {
        int index = Random.Range(0, array.Length);
        return array[index];
    }
}

public static class TransformEx
{
    public static void MulScaleX(this Transform transform, float mul)
    {
        Vector3 scale = transform.localScale;
        scale.x = mul * scale.x;
        transform.localScale = scale;
    }

    public static void MulScaleY(this Transform transform, float mul)
    {
        Vector3 scale = transform.localScale;
        scale.y = mul * scale.y;
        transform.localScale = scale;
    }

    public static void MulScaleZ(this Transform transform, float mul)
    {
        Vector3 scale = transform.localScale;
        scale.z = mul * scale.z;
        transform.localScale = scale;
    }

    public static void SetScaleX(this Transform transform, float x)
    {
        Vector3 scale = transform.localScale;
        scale.x = x;
        transform.localScale = scale;
    }

    public static void SetScaleY(this Transform transform, float y)
    {
        Vector3 scale = transform.localScale;
        scale.y = y;
        transform.localScale = scale;
    }

    public static void SetScaleZ(this Transform transform, float z)
    {
        Vector3 scale = transform.localScale;
        scale.z = z;
        transform.localScale = scale;
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

public static class PolygonCollider2DEx
{
    public static float Radius(this PolygonCollider2D collider)
    {
        float radiusSq = 0.0f;
        Vector2[] points = collider.points;
        for (int i = 0; i < points.Length; i++)
            radiusSq = Mathf.Max(radiusSq, points[i].sqrMagnitude);
        return Mathf.Sqrt(radiusSq);
    }
}
