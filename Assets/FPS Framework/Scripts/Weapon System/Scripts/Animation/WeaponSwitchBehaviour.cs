using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponSwitchBehaviour : WeaponAnimationBehaviourBase
{
    public AnimationClip unequipTargetState;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        /* We want to change the animation we unequip with.
         * We'll then continue as normal.
        */
        controller.animator.ChangeEquipAnimation();
    }
    

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if(stateInfo.normalizedTime >= 1)
        {
            controller.animator.UpdateAnimations();
        }
    }
}
