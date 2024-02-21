using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathExtensions
{
    public static Vector2 ClampThis(this Vector2 value, Vector2 min, Vector2 max)
    {
        return new Vector2(value.x.ClampThis(min.x, max.x), value.y.ClampThis(min.y, max.y));
    }
    public static float ClampThis(this float value, float min, float max)
    {
        return Mathf.Clamp(value, min, max);
    }
    public static Vector3 ClampThis(this Vector3 value, Vector3 min, Vector3 max)
    {
        return new Vector3(
            value.x.ClampThis(min.x, max.x),
            value.y.ClampThis(min.y, max.y),
            value.z.ClampThis(min.z, max.z));
    }
    public static Vector2 ScaleReturn(this Vector2 value, Vector2 scale)
    {
        return new Vector2(value.x * scale.x, value.y * scale.y);
    }
    public static Vector3 ScaleReturn(this Vector3 value, Vector3 scale)
    {
        return new Vector3(value.x * scale.x, value.y * scale.y, value.z * scale.z);
    }
    public static Vector2 Swizzle(this Vector2 value)
    {
        return new Vector2(value.y, value.x);
    }
}
