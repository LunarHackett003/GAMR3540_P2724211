using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BaseWeapon provides core functionality that is shared across all weapons. <br></br>
/// This functionality might not be particularly broad, but it provides a common base for all weapon types.
/// </summary>
public abstract class BaseWeapon : LunarScript
{
    //Constant animation keys
    public const string PRIMARYATTACK = "Primary", SECONDARYATTACK = "Secondary", AMMOPHASE = "AmmoPhase", EMPTYRELOAD = "EmptyReload", PARTIALRELOAD = "TacReload",
        FIRESWITCHUP = "FireSwitchUp", FIRESWITCHDOWN = "FireSwitchDown", COUNTEDRELOAD = "CountedReload", MANUALACTION = "ManualAction", CHANGEWEAPON = "ChangeWeapon",
        CHARGEAMOUNT = "Charge", CHARGING = "Charging";

    public const float TRIGGERTIMETINY = 0.1f, TRIGGERTIMESHORT = 0.4f, TRIGGERTIMELONG = 0.8f;



    public string displayName = "New Weapon";
    public AnimationSetScriptableObject animSet;
    internal bool primaryInput, secondaryInput, primaryPressedFirst, secondaryPressedFirst, primaryPressed, secondaryPressed;
    [SerializeField] internal bool attackOnPrimary, attackOnSecondary, primaryBlocksSecondary, secondaryBlocksPrimary;
    [SerializeField] internal bool aimOnSecondary;
    [SerializeField] internal AimParams aimParams;
    public WeaponController controller;
    //Animation
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
    [SerializeField] internal float CurrentAmmo;
    [SerializeField] internal bool useAmmoPhases;
    [SerializeField] internal int ammoPhases;
    [SerializeField] internal int currentAmmoPhase;
    [SerializeField] internal float partialReloadTime, emptyReloadTime;

    [SerializeField] internal bool queuedReloadAnimation;
    [SerializeField] protected bool fired = false;

    [SerializeField, Tooltip("Should the weapon charge for the primary attack? Takes priority over secondary charge")] protected bool primaryUsesCharge;
    [SerializeField, Tooltip("Should the weapon charge for the secondary attack? Does not work if primary uses charge")] protected bool secondaryUsesCharge;
    [SerializeField, Tooltip("How much charge the weapon accumulates every second")] protected float chargeRate;
    [SerializeField, Tooltip("How much charge the weapon loses every second when not charge")] protected float chargeDecayRate;
    [SerializeField, Tooltip("How much charge is required to fire the weapon?")] protected float minimumChargeToFire;
    [SerializeField, Tooltip("Resets charge to zero after firing")] protected bool resetChargeOnFire;
    [SerializeField, Tooltip("How much charge the weapon currently has")] protected float chargeAmount;
    [SerializeField, Tooltip("Will the weapon charge to full, even if the player releases the fire input?")] protected bool chargeUntilFire;
    [SerializeField, Tooltip("Will the forced charge end when we reach minimum charge, if we've released the fire input?")] protected bool chargeOnlyUntilMinimum;
    [SerializeField, Tooltip("Fires the weapon when we release the fire input")] protected bool fireOnRelease;
    protected virtual bool PrimaryBlocked => fired || (useAmmunition && CurrentAmmo <= 0);
    protected virtual bool ChargeInput => (primaryUsesCharge && primaryInput) || (secondaryUsesCharge && secondaryInput) || chargeHoldFrame;
    internal bool animatedFirePending;
    internal bool animatedFireLast;

    public WeaponAnimator animator;

    [SerializeField] internal bool charging;
    internal bool chargeHoldFrame;

    public void SetPrimaryInput(bool input) => primaryInput = input;
    public void SetSecondaryInput(bool input) => secondaryInput = input;

    protected virtual void Start()
    {
        controller = GetComponentInParent<WeaponController>();
        if (useAmmunition)
        {
            CurrentAmmo = maxAmmo;
        }
    }

    public override void LTimestep()
    {
        base.LTimestep();

        if (animatedFireLast != animatedFirePending)
        {
            animatedFireLast = animatedFirePending;
            SetBool(MANUALACTION, animatedFirePending);
        }
        UpdateInputPriority();
        ProcessInput();

        if (primaryUsesCharge || secondaryUsesCharge)
        {
            UpdateCharge();
        }

        if (useAttackSpread)
        {
            attackSpreadAmount = Mathf.Clamp01(attackSpreadAmount - (Time.fixedDeltaTime * attackSpreadDecay));
        }
    }

    protected virtual void UpdateCharge()
    {
        bool charge = charging || ChargeInput || chargeHoldFrame;
        //animator.SetAnimationBool(CHARGING, primaryUsesCharge ? primaryInput : (secondaryUsesCharge && secondaryInput));
        SetBool(CHARGING, charge);

        //if ((primaryUsesCharge && primaryInput) || (secondaryUsesCharge && secondaryInput))
        //{
        //    chargeAmount += Time.fixedDeltaTime * 
        //}
        //else
        //{

        //}
        

        //If we are not charging this weapon via a coroutine...
        if (!charging)
        {
            //Start charging 
            if (chargeUntilFire && chargeAmount < (chargeOnlyUntilMinimum ? minimumChargeToFire : 1) && !charging && ChargeInput)
            {
                StartCoroutine(ChargeWeaponCoroutine());
            }
            if(!chargeHoldFrame)
                chargeAmount += Time.fixedDeltaTime * (charge ? chargeRate : -chargeDecayRate);
        }

        chargeAmount = Mathf.Clamp01(chargeAmount);
        SetFloat(CHARGEAMOUNT, chargeAmount);
    }
    protected void UpdateInputPriority()
    {
        if (primaryBlocksSecondary)
        {
            if (primaryInput && !secondaryInput)
                primaryPressedFirst = true;
            if (!primaryInput)
            {
                primaryPressedFirst = false;
            }
        }
        if (secondaryBlocksPrimary)
        {
            if (secondaryInput && !primaryInput)
            {
                secondaryPressedFirst = true;
            }
            if (!secondaryInput)
            {
                secondaryPressedFirst = false;
            }
        }
    }
    protected abstract void ProcessInput();
    protected abstract void PrimaryBehaviour();
    protected abstract void SecondaryBehaviour();

    protected virtual void PostAttackBehaviour()
    {
        if (useAttackSpread)
        {
            attackSpreadAmount = Mathf.Clamp01(attackSpreadAmount + attackSpreadIncrement);
        }
        if (useAmmunition)
        {
            CurrentAmmo-= ammoPerShot;
            if(CurrentAmmo <= 0)
            {
                if (useAmmoPhases && currentAmmoPhase < ammoPhases)
                {
                    TriggerAnimation(AMMOPHASE, TRIGGERTIMELONG, true);

                    return;
                }
                TriggerAnimation(EMPTYRELOAD, TRIGGERTIMESHORT, true);
            }
        }
        if(primaryUsesCharge || secondaryUsesCharge && resetChargeOnFire)
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
        CurrentAmmo = maxAmmo;
    }


    internal virtual void TriggerAnimation(string parameter, float time, bool reset = false)
    {
        if(controller != null && controller.animator != null)
            controller.animator.TriggerAnimation(parameter, time, reset);
        if(animator != null)
            animator.TriggerAnimation(parameter, time, reset);
    }

    internal virtual void SetBool(string parameter, bool value)
    {
        if(controller != null && controller.animator != null)
            controller.animator.SetAnimationBool(parameter, value);
        if(animator != null)
            animator.SetAnimationBool(parameter, value);
    }

    internal virtual void SetFloat(string parameter, float value)
    {
        if(controller != null && controller.animator != null)
            controller.animator.SetAnimationFloat(parameter, value);
        if(animator != null)
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
