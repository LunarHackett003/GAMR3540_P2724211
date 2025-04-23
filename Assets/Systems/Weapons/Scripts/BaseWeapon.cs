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
        FIRESWITCHUP = "FireSwitchUp", FIRESWITCHDOWN = "FireSwitchDown";




    public string displayName = "New Weapon";
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
                    controller.TriggerAnimation(AMMOPHASE, 0.2f);
                    currentAmmoPhase++;
                    ReloadWeapon(false);
                    return;
                }
                controller.TriggerAnimation(EMPTYRELOAD, 0.2f);
            }
        }
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
        currentAmmo = maxAmmo;
    }

}
