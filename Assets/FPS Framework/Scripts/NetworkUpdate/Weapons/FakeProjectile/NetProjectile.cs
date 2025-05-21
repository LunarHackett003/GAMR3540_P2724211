using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetProjectile : LunarNetScript
{
    public float damageMultiplier = 1;
    public float velocityMultiplier = 1;
    public ParticleSystem projectileEffect;
    public float maxAliveTime = 10;
    public float maxDistance = 100;

    internal float distanceTravelled;
    [SerializeField] internal float timeAlive;

    internal RangedNetWeapon weapon;

    internal bool terminated;

    public float returnTimeAfterTerminate = 1;

    [SerializeField] internal float thickness;
    [SerializeField] internal Vector3 direction;
    [SerializeField] internal float velocity = 10;
    [SerializeField] internal float gravityMultiplier = 1;

    public HashSet<Collider> ignoredColliders;

    public void InitialiseProjectile(RangedNetWeapon weapon, Vector3 direction, float charge)
    {

        if (NetworkManager.Singleton.IsServer)
        {
            ignoredColliders = weapon.controller.colliderSet;

            this.weapon = weapon;
            damageMultiplier = charge != 0 ? Mathf.Lerp(weapon.minChargeDamageMultiplier, weapon.maxChargeDamageMultiplier, charge) : 1;
            transform.position = weapon.fireOrigin.position;
            transform.forward = direction;
            this.direction = direction;
            timeAlive = 0;
            terminated = false;
            projectileEffect.Play();

            ProjectileSimulator.allProjectiles.Add(this);

            GetComponent<NetworkObject>().Spawn();
        }
    }
    public void TerminateProjectile()
    {
        timeAlive = 0;
        terminated = true;
        ProjectileSimulator.allProjectiles.Remove(this);
    }

    public override void LTimestep()
    {
        if (IsServer)
        {
            if (terminated)
            {
                TerminatedTick();
            }
        }
    }

    public void TickProjectile()
    {
        if (!terminated)
        {

            distanceTravelled += Time.fixedDeltaTime * velocity;
            timeAlive += Time.fixedDeltaTime;

            if(timeAlive >= maxAliveTime || distanceTravelled >= maxDistance)
            {
                TerminateProjectile();
            }

            transform.position += Time.fixedDeltaTime * velocity * direction;
            direction += gravityMultiplier * Time.fixedDeltaTime * Time.fixedDeltaTime * Physics.gravity;
        }
    }
    void TerminatedTick()
    {
        timeAlive += Time.fixedDeltaTime;
        if(timeAlive > returnTimeAfterTerminate)
        {
            RemoveProjectile();
        }
    }
    public void RemoveProjectile()
    {
        if (terminated)
        {
            weapon.ProjectilePool.Release(this);
            NetworkObject.Despawn(false);
            terminated = false;
        }
    }
}
