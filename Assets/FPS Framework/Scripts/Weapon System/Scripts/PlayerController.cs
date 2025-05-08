using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : WeaponController
{
    public bool myPlayer;
    public Damageable damageable;

    public Vector2 moveSpreadVelocityBounds;
    public Vector2 moveSpreadMultiplier;
    public float crouchInaccuracyMultiplier = 0.5f;
    [SerializeField] protected RBPlayerMotor rbpm;
    internal float currentFOV;
    [SerializeField] internal Transform weaponPositionOffset, weaponRotationInvert, weaponTargetTransform;
    Vector3 weaponPosStart;
    internal float fovLerp = 0;
    public override float Spread(float value)
    {
        return value * Mathf.Clamp01((1 - aimAmount) - (rbpm.currentCrouchLerp * crouchInaccuracyMultiplier)) 
            + Mathf.Lerp(moveSpreadMultiplier.x, moveSpreadMultiplier.y, Mathf.InverseLerp(moveSpreadVelocityBounds.x, moveSpreadVelocityBounds.y, rbpm.rb.velocity.magnitude));
    }


    protected override void Start()
    {
        
        base.Start();

        weaponPosStart = weaponPositionOffset.localPosition;

        if (myPlayer)
        {
            GameplayCanvas.playerController = this;
        }

        if (currentWeapon)
        {
            currentWeapon.controller = this;
        }
    }

    public override void LTimestep()
    {
        base.LTimestep();
    }

    public override void LUpdate()
    {

        if (!myPlayer || InputManager.Instance == null)
        {
            /* EITHER!
             * we have no input manager
             * OR
             * we are NOT this player
             */
            return;
        }
        primaryInput = InputManager.PrimaryInput && !FireBlocked;
        secondaryInput = InputManager.SecondaryInput && !FireBlocked;

        if (currentWeapon != null) {
            if (!switchingWeapons && InputManager.WeaponSwitchInput)
            {
                currentWeapon.TriggerAnimation(BaseWeapon.CHANGEWEAPON, 0.2f);
                nextWeaponIndex = (nextWeaponIndex + 1) % weapons.Count;
                switchingWeapons = true;
                return;
            }

            if (InputManager.ReloadInput)
            {
                if (currentWeapon.useAmmunition && (currentWeapon.currentAmmo < currentWeapon.maxAmmo || (currentWeapon.useAmmoPhases && currentWeapon.currentAmmoPhase != 0)))
                {
                    currentWeapon.TriggerAnimation(currentWeapon.currentAmmo > 0 ? BaseWeapon.PARTIALRELOAD : BaseWeapon.EMPTYRELOAD, 0.2f);
                }
                InputManager.ReloadInput = false;
            }
            switch (currentWeapon)
            {
                case RangedWeapon rw:
                    if (InputManager.FireSwitchInput && !FireBlocked)
                    {
                        if(rw.allowedFireModes.Length > 1)
                        {
                            int oldIndex = rw.fireModeIndex;
                            int newindex = (rw.fireModeIndex + 1) % rw.allowedFireModes.Length;
                            currentWeapon.TriggerAnimation(oldIndex > newindex ? BaseWeapon.FIRESWITCHDOWN : BaseWeapon.FIRESWITCHUP, 0.2f);
                            InputManager.FireSwitchInput = false;
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        //moved FOV update to Player Weapon Controller
        UpdateFOV();
        UpdateWeaponOrientation();


        base.LUpdate();


    }
    public void UpdateWeaponOrientation()
    {
        if(currentWeapon == null || currentWeapon.aimParams == null || weaponTargetTransform == null || weaponPositionOffset == null || weaponRotationInvert == null)
        {
            //Invalid setup, cannot do anything
            if(aimLerp > 0)
            {
                aimAmount = Mathf.MoveTowards(aimAmount, 0, rbpm.viewParams.fovMoveSpeed * Time.deltaTime);
                aimLerp = Mathf.Lerp(aimLerp, aimAmount, Time.deltaTime * rbpm.viewParams.fovMoveSpeed);
            }
            return;
        }

        aimAmount = Mathf.MoveTowards(aimAmount, rbpm.aiming ? 1 : 0, currentWeapon.aimParams.aimSpeed * Time.deltaTime);
        //aimLerp = Mathf.Lerp(aimLerp, aimAmount, Time.deltaTime * currentWeapon.aimParams.aimSpeed);
        aimLerp = currentWeapon.aimParams.aimLerpCurve.Evaluate(aimAmount);
        float crouchLerp = currentWeapon.aimParams.crouchLerpCurve.Evaluate(rbpm.currentCrouchLerp);

        //We need to scale the local position of the weapon target, and apply that to the weapon offset
        weaponPositionOffset.localPosition = currentWeapon.aimParams.crouchPositionOffset * (crouchLerp * (1 - (aimLerp * currentWeapon.aimParams.aimRotationReduction))) 
            + (currentWeapon.aimParams.baseAimPositionOffset + 
            weaponTargetTransform.localPosition.Multiply(currentWeapon.aimParams.aimedWeaponPositionScale) + 
            (currentWeapon.aimParams.aimPositionOffsetAngled * (1 - currentWeapon.aimParams.aimRotationReduction))) * aimLerp;


        weaponRotationInvert.localRotation = Quaternion.Lerp(
            Quaternion.Lerp(Quaternion.identity, currentWeapon.aimParams.crouchRotationOffset, crouchLerp),
            Quaternion.Inverse(weaponTargetTransform.localRotation) * currentWeapon.aimParams.aimRotationOffset,
            aimLerp * currentWeapon.aimParams.aimRotationReduction);
    }

    public void UpdateFOV()
    {

        //The mother of all ternary statements...
        float fov =
            //Are we dashing?
            rbpm.dashing ? rbpm.dashCurrentFOV :
            //Are we sliding
            rbpm.isSliding ? rbpm.viewParams.slideFOV :
            //Are we sprinting or sliding?
            ((rbpm.sprinting && InputManager.MoveInput != Vector2.zero) || rbpm.isSliding) ? rbpm.viewParams.sprintFOV :
            //Are we moving normally or crouching?
            0;
        //currentFOV = Mathf.Lerp(rbpm.viewParams.baseFOV, rbpm.viewParams.baseFOV + fov, aimLerp);
        fovLerp = Mathf.Lerp(fovLerp, fov + (currentWeapon != null ? currentWeapon.aimParams.aimFOV * aimLerp : 0), Time.deltaTime * rbpm.viewParams.fovMoveSpeed);
        currentFOV = rbpm.viewParams.baseFOV + fovLerp;
        //rbpm.viewCineCam.m_Lens.FieldOfView = Mathf.Lerp(rbpm.viewParams.viewmodelBaseFOV, rbpm.viewParams.viewmodelBaseFOV + currentWeapon.aimParams.viewmodelFOV, aimLerp);
        rbpm.viewCineCam.m_Lens.FieldOfView = rbpm.viewParams.viewmodelBaseFOV + (currentWeapon != null ? (currentWeapon.aimParams.viewmodelFOV * aimLerp) : 0);
        rbpm.worldCineCam.m_Lens.FieldOfView = currentFOV;
    }
}
