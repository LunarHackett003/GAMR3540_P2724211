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
}
