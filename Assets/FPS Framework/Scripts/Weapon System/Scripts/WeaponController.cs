using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : LunarScript
{
    public BaseWeapon currentWeapon;
    public bool primaryInput, secondaryInput;
    protected bool primaryOld, secondaryOld;

    public Transform fireOrigin;

    internal bool FireBlocked => fireBlockedByAnimation;
    [SerializeField] internal bool fireBlockedByAnimation;
    [SerializeField] internal Animator animator;
    internal float aimLerp = 0;
    internal float aimAmount;

    public virtual float Spread(float value) => value * (1 - aimAmount);

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
        success = newWeapon != null && newWeapon != currentWeapon;
    }

    public virtual void SetAnimationBool(string parameter, bool value)
    {
        if(animator != null)
        {
            animator.SetBool(parameter, value);
        }
    }

    public virtual void TriggerAnimation(string trigger, float time)
    {
        if(animator != null)
        {
            StartCoroutine(AnimationTrigger(trigger, time));
        }
    }
    protected virtual IEnumerator AnimationTrigger(string trigger, float time)
    {
        animator.SetTrigger(trigger);
        yield return new WaitForSeconds(time);
        animator.ResetTrigger(trigger);
    }
}
