using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetProjectile : NetworkBehaviour
{
    public float damageMultiplier = 1;
    public float velocityMultiplier = 1;
    public ParticleSystem projectileEffect;
    public float maxAliveTime = 10;
    public float maxDistance = 100;

    internal float distanceTravelled;
    internal float timeAlive;

    internal RangedNetWeapon weapon;

    [SerializeField] internal float thickness;
    [SerializeField] internal Vector3 direction;
    [SerializeField] internal float velocity = 10;
    [SerializeField] internal float gravityMultiplier = 1;

    public HashSet<Collider> ignoredColliders;

    public void InitialiseProjectile(RangedNetWeapon weapon, Vector3 direction, float charge)
    {

        ignoredColliders = weapon.controller.colliderSet;

        this.weapon = weapon;
        damageMultiplier = charge != 0 ? Mathf.Lerp(weapon.minChargeDamageMultiplier, weapon.maxChargeDamageMultiplier, charge) : 1;
        transform.position = weapon.fireOrigin.position;
        projectileEffect.Play();

        ProjectileSimulator.allProjectiles.Add(this);
    }
    public void TerminateProjectile()
    {
        ProjectileSimulator.allProjectiles.Remove(this);
    }

    public void TickProjectile()
    {
        distanceTravelled += Time.fixedDeltaTime * velocity;
        timeAlive += Time.fixedDeltaTime;

        transform.position += direction * velocity;
        direction += Physics.gravity * gravityMultiplier;
        velocity = direction.magnitude;

        direction.Normalize();
    }
}
