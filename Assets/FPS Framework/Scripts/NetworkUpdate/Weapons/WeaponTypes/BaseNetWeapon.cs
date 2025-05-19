using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BaseNetWeapon : LunarNetScript
{

    public const string PRIMARYATTACK = "Primary", SECONDARYATTACK = "Secondary", AMMOPHASE = "AmmoPhase", EMPTYRELOAD = "EmptyReload", PARTIALRELOAD = "TacReload",
        FIRESWITCHUP = "FireSwitchUp", FIRESWITCHDOWN = "FireSwitchDown", COUNTEDRELOAD = "CountedReload", MANUALACTION = "ManualAction", CHANGEWEAPON = "ChangeWeapon",
        CHARGEAMOUNT = "Charge", CHARGING = "Charging";
    public const float TRIGGERTIMETINY = 0.1f, TRIGGERTIMESHORT = 0.4f, TRIGGERTIMELONG = 0.8f;

    public string displayName = "Networked Weapon";
    public Sprite weaponIcon;
    public WeaponAnimationSetScriptable animationSet;
    internal bool primaryInput, secondaryInput, primaryPressed, secondaryPressed;

    [SerializeField] internal NetWeaponController controller;
    [SerializeField] NetWeaponAnimator animator;
    [SerializeField] internal bool canCrit;
    [SerializeField] internal float critMultiplier;

    [SerializeField] internal float crosshairSpreadBase;
    [SerializeField] internal float crosshairSpreadMax;
    [SerializeField] internal bool useAttackSpread, spreadOnPrimary, spreadOnSecondary;
    [SerializeField] internal float attackSpreadIncrement;
    [SerializeField] internal float attackSpreadDecay;
    [SerializeField] internal float baseAttackSpread = 0.1f;
    [SerializeField] internal float attackSpreadAmount;

    [SerializeField] internal bool useAmmunition, ammoConsumeOnPrimary, ammoConsumeOnSecondary;
    [SerializeField] internal int maxAmmo;
    [SerializeField] internal float ammoPerShot;
    [SerializeField] internal AnticipatedNetworkVariable<float> CurrentAmmo = new(0, StaleDataHandling.Reanticipate);
    [SerializeField] internal bool useAmmoPhases;
    [SerializeField] internal int ammoPhases;
    [SerializeField] internal int currentAmmoPhase;
    [SerializeField] internal float partialReloadTime, emptyReloadTime;

    [SerializeField] internal bool queuedReloadAnimation;
    [SerializeField] protected bool fired = false;

    [SerializeField, Tooltip("Should the weapon charge for the primary attack? Takes priority over secondary charge")] internal bool primaryUsesCharge;
    [SerializeField, Tooltip("Should the weapon charge for the secondary attack? Does not work if primary uses charge")] internal bool secondaryUsesCharge;
    [SerializeField, Tooltip("How much charge the weapon accumulates every second")] internal float chargeRate;
    [SerializeField, Tooltip("How much charge the weapon loses every second when not charge")] protected float chargeDecayRate;
    [SerializeField, Tooltip("How much charge is required to fire the weapon?")] protected float minimumChargeToFire;
    [SerializeField, Tooltip("Resets charge to zero after firing")] protected bool resetChargeOnFire;
    [SerializeField, Tooltip("How much charge the weapon currently has")] protected float chargeAmount;
    [SerializeField, Tooltip("Will the weapon charge to full, even if the player releases the fire input?")] protected bool chargeUntilFire;
    [SerializeField, Tooltip("Will the forced charge end when we reach minimum charge, if we've released the fire input?")] protected bool chargeOnlyUntilMinimum;
    [SerializeField, Tooltip("Fires the weapon when we release the fire input")] protected bool fireOnRelease;
    protected virtual bool PrimaryBlocked => fired || (useAmmunition && CurrentAmmo.Value <= 0);
    protected virtual bool ChargeInput => (primaryUsesCharge && primaryInput) || (secondaryUsesCharge && secondaryInput) || chargeHoldFrame;
    internal bool animatedFirePending;
    internal bool animatedFireLast;

    [SerializeField] internal bool charging;
    internal bool chargeHoldFrame;

    /// <summary>
    /// Calculates the damage that should be dealt at the supplied distance.
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public virtual float GetDamage(float distance = 0)
    {
        return 0;
    }

    public virtual void InitialiseWeapon(NetWeaponController controller)
    {
        this.controller = controller;
    }

    public override void LTimestep()
    {
        base.LTimestep();
        if(animatedFireLast != animatedFirePending)
        {
            animatedFireLast = animatedFirePending;
            
        }


        if(primaryInput && !secondaryPressed)
        {
            PrimaryBehaviour();
        }
        if(secondaryInput && !primaryPressed)
        {
            SecondaryBehaviour();
        }
    }

    protected virtual void PrimaryBehaviour()
    {

    }
    protected virtual void SecondaryBehaviour()
    {

    }
    protected virtual void PostAttackBehaviour()
    {
        if (useAttackSpread)
        {
            attackSpreadAmount = Mathf.Clamp01(attackSpreadAmount + attackSpreadIncrement);
        }
        if (useAmmunition)
        {
            CurrentAmmo.Anticipate(CurrentAmmo.Value - ammoPerShot);
            if (CurrentAmmo.Value <= 0)
            {
                if (useAmmoPhases && currentAmmoPhase < ammoPhases)
                {
                    TriggerAnimation(AMMOPHASE, TRIGGERTIMELONG, true);

                    return;
                }
                TriggerAnimation(EMPTYRELOAD, TRIGGERTIMESHORT, true);
            }
        }
        if (primaryUsesCharge || secondaryUsesCharge && resetChargeOnFire)
        {
            chargeAmount = 0;
        }
    }
    public void IncrementAmmoPhase()
    {
        currentAmmoPhase++;
        ReloadWeapon(false);
    }
    /// <summary>
    /// Restores the weapon's ammunition to MaxAmmo, and optionally reset the ammo phase.
    /// </summary>
    /// <param name="resetAmmoPhase">Reset the ammo phase back to zero?</param>
    internal virtual void ReloadWeapon(bool resetAmmoPhase = false)
    {
        if (useAmmoPhases && resetAmmoPhase)
        {
            currentAmmoPhase = 0;
        }
        primaryPressed = false;
        animatedFirePending = false;
        CurrentAmmo.Anticipate(maxAmmo);
    }


    internal virtual void TriggerAnimation(string parameter, float time, bool reset = false)
    {
        if (controller != null && controller.animator != null)
            controller.animator.TriggerAnimation(parameter, time, reset);
        if (animator != null)
            animator.TriggerAnimation(parameter, time, reset);
    }

    internal virtual void SetBool(string parameter, bool value)
    {
        if (controller != null && controller.animator != null)
            controller.animator.SetAnimationBool(parameter, value);
        if (animator != null)
            animator.SetAnimationBool(parameter, value);
    }

    internal virtual void SetFloat(string parameter, float value)
    {
        if (controller != null && controller.animator != null)
            controller.animator.SetAnimationFloat(parameter, value);
        if (animator != null)
            animator.SetAnimationFloat(parameter, value);
    }

    public virtual IEnumerator ChargeWeaponCoroutine()
    {
        charging = true;
        float threshold = chargeOnlyUntilMinimum ? minimumChargeToFire : 1;
        while (chargeAmount < threshold)
        {
            chargeAmount += Time.fixedDeltaTime * chargeRate;
            if (chargeAmount >= minimumChargeToFire && chargeOnlyUntilMinimum && ChargeInput)
                threshold = 1;
            yield return new WaitForFixedUpdate();
        }
        chargeHoldFrame = true;
        charging = false;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        chargeHoldFrame = false;
        yield break;
    }
}
