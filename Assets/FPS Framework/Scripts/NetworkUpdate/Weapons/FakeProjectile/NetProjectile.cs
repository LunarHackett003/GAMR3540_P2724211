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

    [SerializeField] internal bool terminated;

    public float returnTimeAfterTerminate = 1;
    [SerializeField] protected float terminateTime;
    [SerializeField] protected bool waitingToFire = true;
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
            terminated = false;
            timeAlive = 0;
            distanceTravelled = 0;
            terminateTime = 0;
            waitingToFire = false;
            projectileEffect.Play();

            ProjectileSimulator.allProjectiles.Add(this);

            GetComponent<NetworkObject>().SpawnWithOwnership(weapon.OwnerClientId);
        }
    }
    public void TerminateProjectile(bool reasonIsHit)
    {

        Debug.Log($"Terminating projectile - reason: {(reasonIsHit ? "Hit Object" : "Ran Out Of Time")}");

        timeAlive = 0;
        terminated = true;

        ProjectileSimulator.projectilesToTerminate.Add(this);
    }

    public override void LTimestep()
    {
        if (IsServer)
        {
            if (terminated && !waitingToFire)
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
                TerminateProjectile(false);
            }

            transform.position += Time.fixedDeltaTime * velocity * direction;
            direction += gravityMultiplier * Time.fixedDeltaTime * Time.fixedDeltaTime * Physics.gravity;
        }
    }
    void TerminatedTick()
    {
        terminateTime += Time.fixedDeltaTime;
        if(terminateTime > returnTimeAfterTerminate)
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
            waitingToFire = true;
        }
    }
}
