using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetDamageable : LunarNetScript
{
    public AnticipatedNetworkVariable<float> currentHealth = new(100, StaleDataHandling.Reanticipate);

    [SerializeField] internal int maxHealth;
    public int IntHealth => Mathf.RoundToInt(currentHealth.Value);

    
    public virtual void ModifyHealth(float delta, NetworkBehaviourReference source = default, DamageSourceType damageSourceType = 0, bool isCrit = false)
    {
        if (IsServer)
        {
            HealthUpdated_RPC(delta, source, damageSourceType, isCrit);
        }
        switch (damageSourceType)
        {
            case DamageSourceType.world:
                WorldDamage(delta);
                break;
            case DamageSourceType.weapon:
                WeaponDamage(delta, isCrit, source);
                break;
            case DamageSourceType.hazard:
                MeleeDamage(delta, source);
                break;
            case DamageSourceType.melee:
                MeleeDamage(delta, source);
                break;
            default:
                break;
        }
        if(currentHealth.Value <= 0 || currentHealth.AuthoritativeValue <= 0)
        {
            DamageableDied(source, isCrit);
        }
    }
    [Rpc(SendTo.NotServer)]
    public void HealthUpdated_RPC(float delta, NetworkBehaviourReference source = default, DamageSourceType damageSourceType = 0, bool isCrit = false)
    {
        ModifyHealth(delta, source, damageSourceType, isCrit);
    }
    public virtual void WorldDamage(float deltaHealth)
    {
        if (IsServer)
        {
            currentHealth.AuthoritativeValue += deltaHealth;
        }
        else
        {
            currentHealth.Anticipate(currentHealth.Value + deltaHealth);
        }
    }
    public virtual void WeaponDamage(float deltaHealth, bool isCrit, NetworkBehaviourReference reference)
    {
        if(reference.TryGet(out BaseNetWeapon weapon))
        {
            if (isCrit && weapon.canCrit)
            {
                deltaHealth *= weapon.critMultiplier;
            }
            if (IsServer)
            {
                currentHealth.AuthoritativeValue += deltaHealth;
            }
            else
            {
                currentHealth.Anticipate(currentHealth.Value + deltaHealth);
            }
        }
    }
    public virtual void MeleeDamage(float deltaHealth, NetworkBehaviourReference reference)
    {
        if (IsServer)
        {
            currentHealth.AuthoritativeValue += deltaHealth;
        }
        else
        {
            currentHealth.Anticipate(currentHealth.Value + deltaHealth);
        }
    }
    public virtual void HazardDamage(float deltaHealth, NetworkBehaviourReference reference)
    {
        if (IsServer)
        {
            currentHealth.AuthoritativeValue += deltaHealth;
        }
        else
        {
            currentHealth.Anticipate(currentHealth.Value + deltaHealth);
        }
    }

    public virtual void DamageableDied(NetworkBehaviourReference sourceObj, bool isCrit)
    {

    }
}


public enum DamageSourceType : int
{
    world = 0,
    weapon = 1,
    hazard = 2,
    melee = 3
}
