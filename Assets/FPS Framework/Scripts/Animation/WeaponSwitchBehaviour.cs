using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSwitchBehaviour : WeaponAnimationBehaviourBase
{
    public AnimationClip unequipTargetState;
    bool triggered = false;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        triggered = false;
        base.OnStateEnter(animator, stateInfo, layerIndex);
        /* We want to change the animation we unequip with.
         * We'll then continue as normal.
        */
        if (!canExecute)
            return;
        //controller.animator.ChangeEquipAnimation();
    }
    

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if(canExecute && !triggered && stateInfo.normalizedTime >= 1)
        {
            controller.animator.UpdateAnimations();
            controller.SwitchToWeaponIndex(controller.nextWeaponIndex.Value);
            triggered = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);
        controller.switchingWeapons = false;
    }
}
