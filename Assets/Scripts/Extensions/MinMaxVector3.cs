using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinMaxVector3
{
    public Vector3 min, max;
    public static Vector3 operator +(Vector3 lhs, MinMaxVector3 rhs)
    {
        return RandomVector3(rhs.min, rhs.max) + lhs;
    }
    public static Vector3 RandomVector3(Vector3 min, Vector3 max)
    {
        return new Vector3()
        {
            x = Random.Range(min.x, max.x),
            y = Random.Range(min.y, max.y),
            z = Random.Range(min.z, max.z)
        };
    }
}


