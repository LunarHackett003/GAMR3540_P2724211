using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponReloadBehaviour : WeaponAnimationBehaviourBase
{
    public float reloadAtTime;
    public bool emptyReload;

    protected bool reloaded;
    protected float normalisedTimeForReload;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        Debug.Log($"reload time : {reloadAtTime}, state duration: {stateInfo.length}");
        reloadAtTime = emptyReload ? controller.currentWeapon.emptyReloadTime : controller.currentWeapon.partialReloadTime;
        normalisedTimeForReload = Mathf.InverseLerp(0, stateInfo.length, reloadAtTime);
        reloaded = false;
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);

        if(!reloaded && stateInfo.normalizedTime >= normalisedTimeForReload)
        {
            reloaded = true;
            controller.currentWeapon.ReloadWeapon(true);
        }
    }
}
