using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class RangedWeapon : BaseWeapon
{

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
    public float burstCooldown;
    public bool autoBurst;

    [Tooltip("Does the weapon's charge affect its fire rate?")] public bool chargeAffectsFireRate = false;
    [Tooltip("")] public float minChargeFireRateMultiplier = 0.4f;
    [Tooltip("")] public float maxChargeFireRateMultiplier = 1f;
    [Tooltip("")] public float minChargeDamageMultiplier = 0.2f;
    [Tooltip("")] public float maxChargeDamageMultiplier = 0.2f;

    float currentFireCooldown;
    protected bool burstFiring = false;

    public Transform fireOrigin;
    [Tooltip("How many rays a rweapon will shoot when firing.")]
    public int fireIterations = 1;



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
    }

    protected override void ProcessInput()
    {
        if (!secondaryPressedFirst && !PrimaryBlocked)
            PrimaryBehaviour();
        if(!primaryPressedFirst) 
            SecondaryBehaviour();
    }
    protected virtual void FireWeapon(bool primary = true)
    {
        TriggerAnimation(primary ? PRIMARYATTACK : SECONDARYATTACK, TRIGGERTIMETINY);   
        PostAttackBehaviour();
    }
    protected override void PrimaryBehaviour()
    {

        //if ((!fireOnRelease && primaryInput))
        //{
        //    TryFireRanged();
        //}
        //else if ((fireOnRelease && primaryPressed && !primaryInput))
        //{
        //    TryFireRanged();
        //}

        if((!fireOnRelease && primaryInput) || (fireOnRelease && primaryPressed && !primaryInput))
        {
            if(!primaryUsesCharge || (chargeAmount > minimumChargeToFire))
            {
                TryFireRanged();
            }
        }
        primaryPressed = primaryInput;
    }
    protected virtual void TryFireRanged()
    {
        switch (CurrentFireMode)
        {
            case FireMode.single:
                if (!primaryPressed || fireOnRelease)
                {
                    FireWeapon();
                    fired = true;
                }
                break;
            case FireMode.automatic:
                FireWeapon();
                fired = true;
                break;
            case FireMode.animated:
                if (!animatedFirePending)
                {
                    animatedFirePending = true;
                    FireWeapon();
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
        while (burstRoundsFired < roundsInBurst && (!useAmmunition || currentAmmo > 0))
        {
            burstRoundsFired++;
            FireWeapon();
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
