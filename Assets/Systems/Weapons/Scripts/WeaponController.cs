using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : LunarScript
{
    public BaseWeapon currentWeapon;
    public bool primaryInput, secondaryInput;
    protected bool primaryOld, secondaryOld; 
    public override void LUpdate()
    {
        base.LUpdate();

        if(currentWeapon != null)
        {
            if(primaryOld != primaryInput)
            {
                currentWeapon.SetPrimaryInput(primaryInput);
                primaryOld = primaryInput;
            }
            if(secondaryOld != secondaryInput)
            {
                currentWeapon.SetSecondaryInput(secondaryInput);
                secondaryOld = secondaryInput;
            }
        }
    }

    public bool ChangeCurrentWeapon(BaseWeapon newWeapon, out BaseWeapon oldWeapon)
    {
        oldWeapon = currentWeapon;
        return newWeapon != null && newWeapon != currentWeapon;
    }
}
