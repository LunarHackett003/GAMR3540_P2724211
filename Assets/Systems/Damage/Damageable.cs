using UnityEngine;

public class Damageable : LunarScript
{

    public float maxHealth;
    public float CurrentHealth { get; private set; }

    public delegate void DamageableHit(Damageable d);
    public DamageableHit onHit;

    public bool immune;

    public virtual bool CanTakeDamage => !immune && CurrentHealth > 0;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void ReceiveHit(float deltaHealth)
    {
        if (CanTakeDamage)
        {
            onHit?.Invoke(this);
            ModifyHealth(deltaHealth);
        }
    }
    public virtual void ModifyHealth(float deltaHealth)
    {
        CurrentHealth += deltaHealth;

        if (CurrentHealth <= 0)
        {
            DamageableDied();
        }
    }
    public virtual void DamageableDied()
    {

    }
}
