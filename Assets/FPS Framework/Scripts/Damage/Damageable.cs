using System.Collections;
using UnityEngine;

public class Damageable : LunarScript
{

    public float maxHealth;
    public float CurrentHealth { get; private set; }

    public struct HealthChangeEvent
    {
        public HealthChangeEvent(Damageable damageable, float previous, float current)
        {
            this.damageable = damageable;
            previousHealth = previous;
            currentHealth = current;
        }
        public Damageable damageable;
        public float previousHealth, currentHealth;
    }

    public delegate void HealthChanged(HealthChangeEvent hit);
    public HealthChanged onHealthChanged;

    public bool canRespawn;
    public float respawnTime = 5;
    public float healthRestoredOnRespawn = 1;

    public bool regenerateHealth;
    public float regenDelay, regenRate;
    public float currentRegenTime;
    public bool immune;

    public virtual bool CanTakeDamage => !immune && CurrentHealth > 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }
    public override void LTimestep()
    {
        base.LTimestep();

        if (regenerateHealth && CurrentHealth > 0)
        {
            if(currentRegenTime < regenDelay)
            {
                currentRegenTime += Time.fixedDeltaTime;
            }
            else if(CurrentHealth <= maxHealth)
            {
                OnHealthEvent(Time.fixedDeltaTime * regenRate);
            }
        }
    }
    public void OnHealthEvent(float deltaHealth)
    {
        if (CanTakeDamage || deltaHealth > 0)
        {
            float prev = CurrentHealth;
            ModifyHealth(deltaHealth);
            if (regenerateHealth && deltaHealth < 0)
            {
                currentRegenTime = 0;
            }
            onHealthChanged?.Invoke(new(this, prev, CurrentHealth));
        }
    }
    public virtual void ModifyHealth(float deltaHealth)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + deltaHealth, 0, maxHealth);

        if (CurrentHealth <= 0)
        {
            DamageableDied();
        }
    }
    public virtual void DamageableDied()
    {
        if (canRespawn)
        {
            StartCoroutine(WaitForRespawn(respawnTime, healthRestoredOnRespawn));
        }
    }
    public virtual void DamageableRespawned(float healthAmount = 1)
    {
        CurrentHealth = maxHealth * healthAmount;
    }
    /// <summary>
    /// Triggers a respawn after a time period.
    /// </summary>
    /// <param name="respawnTime">Time (in seconds) to wait before respawning this object. Defaults to 5 seconds.</param>
    /// <param name="healthAmount">0-1 multiplier of max health restored on respawn. Defaults to 1.</param>
    /// <returns></returns>
    public virtual IEnumerator WaitForRespawn(float respawnTime = 5, float healthAmount = 1)
    {
        yield return new WaitForSeconds(respawnTime);
        DamageableRespawned(healthAmount);
    }
}
