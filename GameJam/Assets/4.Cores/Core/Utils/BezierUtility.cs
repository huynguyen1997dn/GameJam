using UnityEngine;

public static class BezierUtility
{
    public static Vector2 Quadratic(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        return Mathf.Pow(1 - t, 2) * a +
               2 * (1 - t) * t * b +
               Mathf.Pow(t, 2) * c;
    }

    public static Vector2 GetControlPoint(Vector2 start, Vector2 end, float height)
    {
        Vector2 mid = (start + end) * 0.5f;
        return mid + Vector2.up * height;
    }
}