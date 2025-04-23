using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthBar;
    public Damageable damageable;
    private void Start()
    {
        if (damageable != null || TryGetComponent(out damageable))
        {
            healthBar.maxValue = damageable.maxHealth;
            healthBar.value = damageable.maxHealth;
            damageable.healthChanged += DamageableHit;
        }
    }
    void DamageableHit(Damageable d)
    {
        healthBar.value = d.CurrentHealth;
    }
}
