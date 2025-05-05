using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : LunarScript
{
    public BaseWeapon currentWeapon;
    public List<BaseWeapon> weapons = new(0);
    public int weaponIndex;
    public bool primaryInput, secondaryInput;
    protected bool primaryOld, secondaryOld;

    public Transform fireOrigin;

    internal bool FireBlocked => fireBlockedByAnimation;
    [SerializeField] internal bool fireBlockedByAnimation;
    internal float aimLerp = 0;
    internal float aimAmount;

    public WeaponAnimator animator;

    public virtual float Spread(float value) => value * (1 - aimAmount);

    protected virtual void Start()
    {
        if(weapons.Count == 0)
        {
            weapons.AddRange(GetComponentsInChildren<BaseWeapon>());
            ChangeCurrentWeapon(weapons[0], out _, out _);
        }
    }

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

    public virtual void ChangeCurrentWeapon(BaseWeapon newWeapon, out BaseWeapon oldWeapon, out bool success)
    {
        oldWeapon = currentWeapon;
        currentWeapon = newWeapon;
        success = newWeapon != null && newWeapon != currentWeapon;
        if(animator != null)
            animator.UpdateAnimations();
    }
    public virtual void SwitchToWeaponIndex(int index)
    {
        ChangeCurrentWeapon(weapons[index], out _, out _);
    }

}
