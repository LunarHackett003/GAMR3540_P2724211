using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponFireSwitchBehaviour : WeaponAnimationBehaviourBase
{
    RangedWeapon weapon;
    bool switched;
    public float timeToSwitch;
    float normTime;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);

        weapon = (RangedWeapon)controller.currentWeapon;
        if(weapon != null)
        {
            timeToSwitch = weapon.fireModeSwitchTime;
            normTime = Mathf.InverseLerp(0, stateInfo.length, timeToSwitch);
            switched = false;
        }
        else
        {
            Debug.Log("Should not have entered this state with current weapon, re-evaluate. Stupid dev >:|");
        }
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);

        if(!switched && stateInfo.normalizedTime >= normTime)
        {
            weapon.IncrementFireMode();
            switched = true;
        }
    }
}
