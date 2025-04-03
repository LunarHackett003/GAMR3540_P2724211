using UnityEngine;

public class Damageable : LunarScript
{

    public float maxHealth;
    public float CurrentHealth { get; private set; }

    public delegate void DamageableHit(Damageable d);
    public DamageableHit onHit;
    public MeshRenderer render;
    public Collider collide;
    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public bool CanDamage()
    {
        return false;
    }

    public void ModifyHealth(float deltaHealth)
    {
        CurrentHealth += deltaHealth;
        onHit?.Invoke(this);
        if(CurrentHealth <= 0)
        {
            render.enabled = false;
            collide.enabled = false;
        }
    }
    private void OnValidate()
    {
        if(render == null)
            render = GetComponent<MeshRenderer>();
        if(collide == null)
            collide = GetComponent<Collider>();
    }
}
