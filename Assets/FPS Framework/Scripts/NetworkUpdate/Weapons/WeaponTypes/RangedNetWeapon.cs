using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Pool;

/// <summary>
/// The latest iteration in the weapon system, combining hitscan AND projectile weapons.<br></br>
/// 
/// </summary>
public class RangedNetWeapon : BaseNetWeapon
{

    IObjectPool<NetProjectile> projectilePool;
    public int poolStartCapacity = 50, poolMaxCapacity = 500;

    public IObjectPool<NetProjectile> ProjectilePool
    {
        get
        {
            projectilePool ??= new ObjectPool<NetProjectile>(CreatePooledItem, TakeFromPool, ReturnToPool, DestroyPoolObject, true, poolStartCapacity, poolMaxCapacity);
            return projectilePool;
        }
    }

    NetProjectile CreatePooledItem()
    {
        NetProjectile np = Instantiate(ProjectilePrefab, fireOrigin.position, Quaternion.identity, null).GetComponent<NetProjectile>();
        np.gameObject.hideFlags = HideFlags.HideInHierarchy;
        return np;
    }
    void ReturnToPool(NetProjectile trace)
    {
        trace.gameObject.hideFlags = HideFlags.HideInHierarchy;
        trace.projectileEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        trace.gameObject.SetActive(false);
    }
    void TakeFromPool(NetProjectile trace)
    {
        trace.gameObject.SetActive(true);
        trace.gameObject.hideFlags = HideFlags.None;
    }
    void DestroyPoolObject(NetProjectile trace)
    {
        if (trace != null)
            Destroy(trace.gameObject);
    }







    public enum FireMode : int
    {
        single = 1,
        burst = 2,
        automatic = 4,
        animated = 8
    }

    [Tooltip("the radius of the base spread per unit distance covered by the shot.")]
    public float baseSpreadPerUnit = 0.1f;
    [Tooltip("the radius of the max spread influenced by movement.")]
    public float maxInfluencedSpreadPerUnit = 0.1f;
    [Tooltip("the current influence of the owner's movement.")]
    public float currentMovementInfluence = 0;


    public virtual Vector3 SpreadVector => (((Vector3)Random.insideUnitCircle *
        (baseSpreadPerUnit + (maxInfluencedSpreadPerUnit * controller.Spread(baseAttackSpread + attackSpreadAmount))))
        + Vector3.forward).normalized;

    public Quaternion FireRotation => controller.fireOrigin != null ? controller.fireOrigin.rotation : fireOrigin.rotation;
    public Vector3 FirePosition => controller.fireOrigin != null ? controller.fireOrigin.position : fireOrigin.position;

    public FireMode[] allowedFireModes = new FireMode[] { FireMode.automatic };
    public int fireModeIndex = 0;
    public float fireModeSwitchTime;
    public FireMode CurrentFireMode => allowedFireModes[fireModeIndex];
    public int roundsPerMinute;
    public int roundsInBurst;
    protected int burstRoundsFired;
    public float timeBetweenRounds;
    public float TrueTimeBetweenRounds => timeBetweenRounds /
        (chargeAffectsFireRate ? Mathf.Lerp(minChargeFireRateMultiplier, maxChargeFireRateMultiplier, chargeAmount) : 1);

    public float debugdisplay_truetime;
    public float burstCooldown;
    public bool autoBurst;

    [Tooltip("Does the weapon's charge affect its fire rate?")] public bool chargeAffectsFireRate = false;
    [Tooltip("")] public float minChargeFireRateMultiplier = 0.4f;
    [Tooltip("")] public float maxChargeFireRateMultiplier = 1f;
    [Tooltip("")] public float minChargeDamageMultiplier = 0.2f;
    [Tooltip("")] public float maxChargeDamageMultiplier = 0.2f;

    [SerializeField] internal float currentFireCooldown;
    protected bool burstFiring = false;

    public Transform fireOrigin;
    [Tooltip("How many rays a rweapon will shoot when firing.")]
    public int fireIterations = 1;

    public GameObject ProjectilePrefab;

    public override void LTimestep()
    {
        base.LTimestep();

        if (fired)
        {
            currentFireCooldown += Time.fixedDeltaTime;
        }
        if (currentFireCooldown >= TrueTimeBetweenRounds)
        {
            fired = false;
            currentFireCooldown = 0;
        }

        debugdisplay_truetime = TrueTimeBetweenRounds;
    }

    [Rpc(SendTo.Everyone, DeferLocal = true)]
    protected void FireWeapon_RPC(Quaternion rotation, Vector3 origin, bool primary = true)
    {
        if (IsOwner)
        {
            TriggerAnimation(primary ? PRIMARYATTACK : SECONDARYATTACK, TRIGGERTIMETINY);
        }
        if (IsServer)
        {
            ServerFire(rotation, origin);
        }
        PostAttackBehaviour();
    }
    public virtual void ServerFire(Quaternion rotation, Vector3 origin)
    {
        for (int i = 0; i < fireIterations; i++)
        {
            ProjectilePool.Get(out NetProjectile v);
            v.InitialiseProjectile(this, rotation * SpreadVector, chargeAmount);
        }
    }
    protected override void PrimaryBehaviour()
    {
        //base.PrimaryBehaviour();


        bool chargeMet = (chargeRate <= 0 || chargeAmount >= (chargeOnlyUntilMinimum ? minimumChargeToFire : 1));
        if (!fireOnRelease && (primaryInput || chargeHoldFrame) && chargeMet || (fireOnRelease && primaryPressed && !primaryInput))
        {
            if (!primaryUsesCharge || (chargeAmount > minimumChargeToFire))
            {
                TryFireRanged();
            }
        }
        primaryPressed = primaryInput && chargeMet;

    }
    protected virtual void TryFireRanged()
    {
        switch (CurrentFireMode)
        {
            case FireMode.single:
                if (!primaryPressed || fireOnRelease)
                {
                    FireWeapon_RPC(FireRotation, FirePosition);
                    fired = true;
                }
                break;
            case FireMode.automatic:
                FireWeapon_RPC(FireRotation, FirePosition);
                fired = true;
                break;
            case FireMode.animated:
                if (!animatedFirePending)
                {
                    animatedFirePending = true;
                    FireWeapon_RPC(FireRotation, FirePosition);
                }
                break;
            case FireMode.burst:
                if (roundsInBurst > 0 && !burstFiring)
                {
                    StartCoroutine(BurstFire());
                }
                break;
            default:
                break;
        }
    }
    protected override void SecondaryBehaviour()
    {

    }
    protected virtual void OnValidate()
    {
        timeBetweenRounds = 1 / ((float)roundsPerMinute / 60);
    }
    protected virtual IEnumerator BurstFire()
    {
        burstFiring = true;
        while (burstRoundsFired < roundsInBurst && (!useAmmunition || CurrentAmmo.Value > 0))
        {
            burstRoundsFired++;
            FireWeapon_RPC(FireRotation, FirePosition);
            yield return new WaitForSeconds(timeBetweenRounds);
        }
        yield return new WaitForSeconds(burstCooldown);
        if (!autoBurst)
        {
            yield return new WaitUntil(() => { return primaryInput == false; });
        }
        burstRoundsFired = 0;
        burstFiring = false;
        yield break;
    }
    public virtual void IncrementFireMode()
    {
        fireModeIndex++;
        fireModeIndex %= allowedFireModes.Length;
    }
}
