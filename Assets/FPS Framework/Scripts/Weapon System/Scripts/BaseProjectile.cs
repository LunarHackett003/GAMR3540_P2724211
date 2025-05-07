using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseProjectile : LunarScript
{
    protected Rigidbody rb;

    [SerializeField, Range(0, 50)] internal int maxBounceCount = 1;
    [SerializeField] internal bool useBounceCount;
    [SerializeField] internal float velocity = 15;
    [SerializeField] internal float gravityMultiplier = 1;
    [SerializeField] internal float maxAliveTime;

    internal ProjectileWeapon owner;
    protected int bounceCount;
    protected bool terminated;
    public override void LTimestep()
    {
        base.LTimestep();

        if(gravityMultiplier != 1)
        {
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(bounceCount <= 0)
        {
            ProjectileHitEvent(collision);
            return;
        }
        if(useBounceCount)
        {
            bounceCount--;
        }
    }

    private void OnValidate()
    {
        if(rb == null && !TryGetComponent(out rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }

    /// <summary>
    /// Called by the weapon firing this object. Executes AFTER initialise from pool.
    /// </summary>
    /// <param name="weapon"></param>
    public virtual void InitialiseFromWeapon(ProjectileWeapon weapon)
    {
        owner = weapon;
    }

    ///<summary>
    /// Called when the projectile can no longer bounce.
    /// </summary>
    /// <param name="collision"></param>
    public virtual void ProjectileHitEvent(Collision collision)
    {
        terminated = true;
        owner.ProjectilePool.Release(this);
    }

    /// <summary>
    /// Called when this projectile is created by the object pool
    /// </summary>
    public virtual void CreatedForPool()
    {
        if(rb != null)
        {
            rb.isKinematic = true;
        }
        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Called when this projectile is returned to the object pool
    /// </summary>
    public virtual void ReturnToPool()
    {
        rb.isKinematic = true;
    }
    /// <summary>
    /// Called when this projectile is spawned from the object pool
    /// </summary>
    public virtual void InitialiseFromPool()
    {
        terminated = false;
        rb.isKinematic = false;
        if (useBounceCount)
        {
            bounceCount = 1;
        }
        else
        {
            bounceCount = maxBounceCount;
        }
        rb.useGravity = gravityMultiplier == 1;
        rb.velocity = transform.forward * velocity;
    }
}
