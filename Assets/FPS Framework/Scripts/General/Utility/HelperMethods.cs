using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HelperMethods
{
    public static Vector3 Multiply(this Vector3 value, Vector3 scale)
    {
        value.Scale(scale);
        return value;
    }
    public static void Flip(ref this bool value)
    {
        value = !value;
    }

    
    public static Vector3 RandomPerAxis(Vector3 min, Vector3 max)
    {
        return new Vector3
        {
            x = Random.Range(min.x, max.x),
            y = Random.Range(min.y, max.y),
            z = Random.Range(min.z, max.z)
        };
    }
}
