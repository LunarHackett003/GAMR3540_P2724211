using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseAI : Character
{

    [SerializeField] protected AIWeaponController controller;

    public override void LTimestep()
    {
        base.LTimestep();
    }
}
