using UnityEngine;

public abstract class SingletonBehaviour<T> : LunarScript where T : LunarScript
{
    public static T Instance;
}
