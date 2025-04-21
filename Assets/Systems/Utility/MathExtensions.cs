using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathExtensions
{
    public static Vector3 Multiply(this Vector3 value, Vector3 scale)
    {
        value.Scale(scale);
        return value;
    }

}
