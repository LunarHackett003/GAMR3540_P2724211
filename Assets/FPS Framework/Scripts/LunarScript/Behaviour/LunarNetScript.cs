using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LunarNetScript : NetworkBehaviour
{
    protected virtual void OnEnable()
    {
        Debug.Log("Subscribed this net script");
        LunarManager.Instance.lPostUpdate += LPostUpdate;
        LunarManager.Instance.lUpdate += LUpdate;
        LunarManager.Instance.lTimeStep += LTimestep;
    }
    protected virtual void OnDisable()
    {
        Debug.Log("Unsubscribed this net script");
        LunarManager.Instance.lPostUpdate -= LPostUpdate;
        LunarManager.Instance.lUpdate -= LUpdate;
        LunarManager.Instance.lTimeStep -= LTimestep;
    }
    


    public virtual void LUpdate()
    {

    }
    public virtual void LTimestep()
    {

    }
    public virtual void LPostUpdate()
    {

    }
}
