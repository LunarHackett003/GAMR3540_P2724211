using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NetWeaponAnimator : LunarNetScript
{
    [SerializeField] internal Animator animator;
    [SerializeField] internal WeaponController controller;
    [SerializeField] internal BaseNetWeapon weapon;
    public bool isWeapon;

    internal AnimatorOverrideController aoc;
    internal AnimationClipOverrides clipOverrides;


    internal void Initialise()
    {
        if (!TryGetComponent(out controller))
        {
            controller = GetComponentInParent<WeaponController>();
        }
        if (isWeapon)
        {
            weapon = GetComponent<BaseNetWeapon>();
            UpdateAnimations();
            animator.tag = "Weapon";
        }
    }

    public void UpdateAnimations()
    {
        if (aoc == null)
        {
            aoc = new(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = aoc;

            clipOverrides = new(aoc.overridesCount);
            aoc.GetOverrides(clipOverrides);
        }

        AnimationClipPair[] clips = isWeapon ? weapon.animationSet.clips : controller.currentWeapon.animSet.clips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClipPair acp = clips[i];
            //if (!isWeapon && acp.targetClip.name == "ChangeWeapon")
            //{
            //    continue;
            //}
            //else
            clipOverrides[acp.targetClip.name] = isWeapon ? acp.weaponClip : acp.characterClip;
        }
        aoc.ApplyOverrides(clipOverrides);
    }
    public void ChangeEquipAnimation()
    {
        if (aoc != null)
        {
            clipOverrides["ChangeWeapon"] =
                controller.currentWeapon.animSet.clips.First(x => x.targetClip.name == "ChangeWeapon").characterClip;
        }
    }

    public virtual void SetAnimationBool(string parameter, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(parameter, value);
        }
    }
    public virtual void SetAnimationFloat(string parameter, float value)
    {
        if (animator != null)
        {
            animator.SetFloat(parameter, value);
        }
    }

    public virtual void TriggerAnimation(string trigger, float time, bool reset = false)
    {
        if (animator != null)
        {
            StartCoroutine(AnimationTrigger(trigger, time, reset));
        }
    }
    protected virtual IEnumerator AnimationTrigger(string trigger, float time, bool reset = false)
    {
        animator.SetTrigger(trigger);
        if (reset)
        {
            yield return new WaitForSeconds(time);
            animator.ResetTrigger(trigger);
        }
        yield break;
    }
}
