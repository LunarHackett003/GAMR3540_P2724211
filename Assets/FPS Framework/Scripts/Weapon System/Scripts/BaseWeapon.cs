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
        FIRESWITCHUP = "FireSwitchUp", FIRESWITCHDOWN = "FireSwitchDown", COUNTEDRELOAD = "CountedReload", MANUALACTION = "ManualAction", CHANGEWEAPON = "ChangeWeapon";

    public const float TRIGGERTIMESHORT = 0.4f, TRIGGERTIMELONG = 0.8f;



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
    [SerializeField] internal float currentAmmo;
    [SerializeField] internal bool useAmmoPhases;
    [SerializeField] internal int ammoPhases;
    [SerializeField] internal int currentAmmoPhase;
    [SerializeField] internal float partialReloadTime, emptyReloadTime;

    [SerializeField] internal bool queuedReloadAnimation;
    [SerializeField] protected bool fired = false;
    protected virtual bool PrimaryBlocked => fired || (useAmmunition && currentAmmo <= 0);

    internal bool animatedFirePending;
    internal bool animatedFireLast;

    public WeaponAnimator animator;

    public void SetPrimaryInput(bool input) => primaryInput = input;
    public void SetSecondaryInput(bool input) => secondaryInput = input;

    protected virtual void Start()
    {
        controller = GetComponentInParent<WeaponController>();

        if (useAmmunition)
        {
            currentAmmo = maxAmmo;
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
        if (useAttackSpread)
        {
            attackSpreadAmount = Mathf.Clamp01(attackSpreadAmount - (Time.fixedDeltaTime * attackSpreadDecay));
        }
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
            currentAmmo-= ammoPerShot;
            if(currentAmmo <= 0)
            {
                if (useAmmoPhases && currentAmmoPhase < ammoPhases)
                {
                    TriggerAnimation(AMMOPHASE, TRIGGERTIMELONG);

                    return;
                }
                TriggerAnimation(EMPTYRELOAD, TRIGGERTIMESHORT);
            }
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
        currentAmmo = maxAmmo;
    }


    internal virtual void TriggerAnimation(string parameter, float time)
    {
        if(controller != null && controller.animator != null)
            controller.animator.TriggerAnimation(parameter, time);
        if(animator != null)
            animator.TriggerAnimation(parameter, time);
    }

    internal virtual void SetBool(string parameter, bool value)
    {
        if(controller != null)
            controller.animator.SetAnimationBool(parameter, value);
        if(animator != null)
            animator.SetAnimationBool(parameter, value);
    }
}
