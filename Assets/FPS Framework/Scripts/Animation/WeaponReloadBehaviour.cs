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
        if (!canExecute)
            return;

        Debug.Log($"reload time : {reloadAtTime}, state duration: {stateInfo.length}", animator.gameObject);
        reloadAtTime = emptyReload ? controller.CurrentWeapon.emptyReloadTime : controller.CurrentWeapon.partialReloadTime;
        normalisedTimeForReload = Mathf.InverseLerp(0, stateInfo.length, reloadAtTime);
        reloaded = false;
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (!canExecute)
            return;
        if (!reloaded && stateInfo.normalizedTime >= normalisedTimeForReload)
        {
            Debug.Log("attempted to reload weapon", weapon);
            reloaded = true;
            weapon.ReloadWeapon(true);
        }
    }
}
