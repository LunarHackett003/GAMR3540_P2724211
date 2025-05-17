using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDamageable : Damageable
{
    public Renderer[] allRenderers;
    public Rigidbody rb;
    public Collider col;

    protected override void Awake()
    {
        base.Awake();
        allRenderers = GetComponentsInChildren<Renderer>();
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }


    public override void DamageableDied()
    {
        base.DamageableDied();

        if (canRespawn)
        {
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer item = allRenderers[i];
                item.enabled = false;
            }
            if (rb != null)
                rb.isKinematic = true;
            if (col != null) 
                col.enabled = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public override void DamageableRespawned(float healthAmount = 1)
    {
        base.DamageableRespawned(healthAmount);

        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer item = allRenderers[i];
            item.enabled = true;
        }
        if (rb != null)
            rb.isKinematic = false;
        if (col != null)
            col.enabled = true;
    }

}
