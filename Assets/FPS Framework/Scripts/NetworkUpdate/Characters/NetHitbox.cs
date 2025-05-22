using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetHitbox : NetDamageable
{
    [SerializeField] NetDamageable rootDamageable;

    [SerializeField] internal float damageMultiplier = 1;
    [SerializeField] internal bool isCritBox = false;


    public override void ModifyHealth(float delta, NetworkBehaviourReference source = default, DamageSourceType damageSourceType = 0, bool isCrit = false)
    {
        Debug.Log("Passed damage from hitbox to root damageable");
        rootDamageable.ModifyHealth(delta, source, damageSourceType, isCritBox || isCrit);
    }

}
