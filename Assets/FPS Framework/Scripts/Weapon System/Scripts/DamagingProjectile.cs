using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagingProjectile : BaseProjectile
{
    protected float distanceTravelled;
    [SerializeField] internal float minDamageRange, maxDamageRange;
    [SerializeField] internal float damageAtMinRange, damageAtMaxRange;



    public override void LTimestep()
    {
        base.LTimestep();
        distanceTravelled += rb.velocity.magnitude * Time.fixedDeltaTime;
    }

    public override void ProjectileHitEvent(Collision collision)
    {
        
        if(collision.collider.TryGetComponent(out Damageable d))
        {


            float dmg = Mathf.Lerp(damageAtMinRange, damageAtMaxRange, Mathf.Clamp01(Mathf.InverseLerp(minDamageRange, maxDamageRange, distanceTravelled))) * damageMultiplier;
            Debug.Log($"hit a collider and dealt {dmg} damage after moving {distanceTravelled} metres");
            d.OnHealthEvent(-dmg);
        }
        base.ProjectileHitEvent(collision);

    }
}
