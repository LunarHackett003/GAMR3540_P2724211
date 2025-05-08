using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

[RequireComponent(typeof(Rigidbody))]
public abstract class BaseProjectile : LunarScript
{
    [SerializeField] protected Rigidbody rb;

    [SerializeField, Range(0, 50)] internal int maxBounceCount = 1;
    [SerializeField] internal bool useBounceCount;
    [SerializeField] internal float velocity = 15;
    [SerializeField] internal float gravityMultiplier = 1;
    [SerializeField] internal float maxAliveTime;

    [SerializeField] internal float damageMultiplier = 1;

    internal ProjectileWeapon owner;
    [SerializeField] protected int bounceCount;
    protected bool terminated;

    public GameObject hitEffectPrefab;
    public float hitEffectRemoveTime;
    public bool attachHitEffectToCollider;

    public bool disableCollidersOnHit;
    public Collider[] colliders;

    protected float aliveTime;

    public bool followVelocity;

    GameObject hitEffect;

    public override void LTimestep()
    {
        base.LTimestep();

        if(gravityMultiplier != 1)
        {
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
        aliveTime += Time.fixedDeltaTime;
        if(maxAliveTime > 0 && aliveTime > maxAliveTime )
        {
            Terminate();
        }
        if (followVelocity)
        {
            transform.forward = rb.velocity;
        }

        if (hitEffect)
        {
            hitEffect.transform.SetPositionAndRotation(transform.position, transform.rotation);
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

        }
    }

    /// <summary>
    /// Called by the weapon firing this object. Executes AFTER initialise from pool.
    /// </summary>
    /// <param name="weapon"></param>
    public virtual void InitialiseFromWeapon(ProjectileWeapon weapon)
    {
        owner = weapon;
        CreateHitPrefab();
    }

    ///<summary>
    /// Called when the projectile can no longer bounce.
    /// </summary>
    /// <param name="collision"></param>
    public virtual void ProjectileHitEvent(Collision collision)
    {
        terminated = true;

        if (attachHitEffectToCollider)
        {
            ParentConstraint pc = hitEffect.AddComponent<ParentConstraint>();
            pc.AddSource(new ConstraintSource()
            {
                sourceTransform = collision.collider.transform,
                weight = 1
            });
            pc.constraintActive = true;
            pc.locked = true;
        }
        if(hitEffectRemoveTime > 0)
        {
            Destroy(hitEffect, hitEffectRemoveTime);
        }
        if (disableCollidersOnHit)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }
        Terminate();
    }

    protected virtual void Terminate()
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
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
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
        aliveTime = 0;
        if (!useBounceCount)
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

    void CreateHitPrefab()
    {
        hitEffect = Instantiate(hitEffectPrefab);
        hitEffect.transform.SetPositionAndRotation(position: transform.position,
            rotation: transform.rotation);
    }
}
