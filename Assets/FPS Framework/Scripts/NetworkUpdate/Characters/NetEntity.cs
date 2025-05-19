using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class NetEntity : NetDamageable
{

    public AnticipatedNetworkVariable<bool> isDead = new(false, StaleDataHandling.Reanticipate);

    [SerializeField] protected bool canRegenerateHealth = false;
    [SerializeField] protected float regenerationDelay = 5;
    protected float currentRegenTime = 0;
    [SerializeField] protected float regenerationRate = 5;
    [SerializeField] protected bool immuneToDamage = false;

    [SerializeField] protected bool receivesDebuffs;
    [SerializeField] protected List<Debuff> currentDebuffs;

    [SerializeField] protected float movementModifier = 0;
    [SerializeField] protected float rotationModifier = 0;

    [SerializeField] protected float damageMultiplier = 1;
    /// <summary>
    /// Invokes an event on all subscribers, passing the new health and the source entity's ID.
    /// </summary>
    public UnityEvent<float, long> HealthModified;

    float healthThisFrame;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentHealth.Anticipate(maxHealth);
    }

    public virtual void ClearDebuff(Debuff debuff)
    {
        if (currentDebuffs.Contains(debuff))
        {
            currentDebuffs.Remove(debuff);
        }
    }


    [Rpc(SendTo.ClientsAndHost, DeferLocal = true)]
    public void HealthChanged_RPC(float deltaHealth, float lastAuthHealthValue, long source = -1, RpcParams rpcParams = default)
    {
        HealthModified?.Invoke(lastAuthHealthValue - deltaHealth, source);
        if(deltaHealth < 0)
            currentRegenTime = 0;
    }


    public override void LTimestep()
    {
        base.LTimestep();
        healthThisFrame = currentHealth.Value;
        if (canRegenerateHealth && !isDead.Value)
        {
            if (currentRegenTime >= regenerationDelay)
            {
                if (IsServer)
                {
                    currentHealth.AuthoritativeValue = Mathf.Clamp(currentHealth.AuthoritativeValue + Time.fixedDeltaTime * regenerationRate, 0, maxHealth);
                }
                else
                {
                    currentHealth.Anticipate(Mathf.Clamp(currentHealth.Value + Time.fixedDeltaTime * regenerationRate, 0, maxHealth));
                }
            }
            else
            {
                currentRegenTime += Time.fixedDeltaTime;
            }
        }

        if (IsServer && healthThisFrame != currentHealth.AuthoritativeValue)
        {
            float delta = currentHealth.AuthoritativeValue - healthThisFrame;
            //We check if we've regenerated any health, and then we tell the clients that the owner of this object modified its health.
            HealthChanged_RPC(delta, currentHealth.AuthoritativeValue, (long)OwnerClientId);
        }
    }

}


