using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class WeaponAnimator : LunarScript
{
    [SerializeField] internal Animator animator;
    [SerializeField] internal WeaponController controller;
    public bool isWeapon;

    internal AnimatorOverrideController aoc;
    internal AnimationClipOverrides clipOverrides;


    private void Start()
    {
        if (controller.TryGetComponent(out controller))
        {

        }
    }

    public void UpdateAnimations()
    {
        if(aoc == null)
        {
            aoc = new(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = aoc;

            clipOverrides = new(aoc.overridesCount);
            aoc.GetOverrides(clipOverrides);
        }

        for (int i = 0; i < controller.currentWeapon.animSet.clips.Length; i++)
        {
            AnimationClipPair acp = controller.currentWeapon.animSet.clips[i];
            if (acp.targetClip.name == "ChangeWeapon")
            {
                continue;
            }
            else
            {
                clipOverrides[acp.targetClip.name] = acp.newClip;
            }
        }
        aoc.ApplyOverrides(clipOverrides);
    }
    public void ChangeEquipAnimation()
    {
        if(aoc != null)
        {
            clipOverrides["ChangeWeapon"] = 
                controller.currentWeapon.animSet.clips.First(x => x.targetClip.name == "ChangeWeapon").newClip;
        }
    }

    public virtual void SetAnimationBool(string parameter, bool value)
    {
        if (animator != null)
        {
            animator.SetBool(parameter, value);
        }
    }

    public virtual void TriggerAnimation(string trigger, float time)
    {
        if (animator != null)
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

public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
{
    public AnimationClipOverrides(int capacity) : base(capacity) { }

    public AnimationClip this[string name]
    {
        get { return this.Find(x => x.Key.name.Equals(name)).Value; }
        set
        {
            int index = this.FindIndex(x => x.Key.name.Equals(name));
            if (index != -1)
                this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
        }
    }
}
