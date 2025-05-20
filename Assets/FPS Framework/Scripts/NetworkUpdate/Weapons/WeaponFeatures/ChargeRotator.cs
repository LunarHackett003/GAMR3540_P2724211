using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChargeRotator : LunarScript
{
    public BaseNetWeapon weapon;

    public float spinSpeed = 5;

    public Vector3 axis = Vector3.up;

    private void Start()
    {
        if(weapon == null)
        {
            weapon = GetComponentInParent<BaseNetWeapon>();
        }
    }

    public override void LPostUpdate()
    {
        base.LPostUpdate();

        if (weapon)
        {
            transform.Rotate(axis, spinSpeed * Time.deltaTime * weapon.chargeAmount, Space.Self);
        }
    }
}
