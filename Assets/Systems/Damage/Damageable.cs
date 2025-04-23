using UnityEngine;

public class Damageable : LunarScript
{

    public float maxHealth;
    public float CurrentHealth { get; private set; }

    public delegate void DamageableHit(Damageable d);
    public DamageableHit healthChanged;

    public bool regenerateHealth;
    public float regenDelay, regenRate;
    float currentRegenTime;
    public bool immune;

    public virtual bool CanTakeDamage => !immune && CurrentHealth > 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }
    public override void LTimestep()
    {
        base.LTimestep();

        if (regenerateHealth)
        {
            if(currentRegenTime < regenDelay)
            {
                currentRegenTime += Time.fixedDeltaTime;
            }
            else if(CurrentHealth <= maxHealth)
            {
                HealthEvent(Time.fixedDeltaTime * regenRate);
            }
        }
    }
    public void HealthEvent(float deltaHealth)
    {
        if (CanTakeDamage)
        {
            ModifyHealth(deltaHealth);
            if (regenerateHealth)
            {
                currentRegenTime = 0;
            }
            healthChanged?.Invoke(this);
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

    }
}
