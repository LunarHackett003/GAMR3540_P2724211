using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : LunarScript
{
    Rigidbody rb;

    [SerializeField, Range(0, 50)] int maxBounceCount = 1;
    [SerializeField] float velocity = 15;
    [SerializeField] float gravityMultiplier = 1;

    internal ProjectileWeapon owner;
    int bounceCount;

    private void Start()
    {
        if(rb == null)
            rb = GetComponent<Rigidbody>();

        rb.useGravity = gravityMultiplier == 1;
        rb.velocity = transform.forward * velocity;
    }

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
        if(bounceCount == 0)
        {
            owner.ProjectilePool.Release(this);
            return;
        }
        bounceCount--;
    }

    private void OnValidate()
    {
        if(rb == null && !TryGetComponent(out rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
    }
}
