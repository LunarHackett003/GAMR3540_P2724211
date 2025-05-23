using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetPlayerWeaponController : NetWeaponController
{
    [SerializeField] internal bool reloadInput, meleeInput, weaponSwitchInput;
    [SerializeField] internal NetPlayerEntity player;


    [SerializeField] float crouchAccuracyMultiplier;
    internal float currentFOV;
    [SerializeField] internal Transform weaponPositionOffset, weaponRotationInvert, weaponTargetTransform;
    internal float fovLerp = 0;
    
    public override float Spread(float value)
    {
        return value * Mathf.Clamp01((1 - aimAmount) - (player.motor.currentCrouchLerp * crouchAccuracyMultiplier));
    }
    public override void LPostUpdate()
    {
        base.LPostUpdate();
        if (CurrentWeapon != null)
        {
            CurrentWeapon.transform.SetPositionAndRotation(weaponRotationInvert.position, weaponRotationInvert.rotation);
        }
    }

    public override void LUpdate()
    {
        if(!IsOwner && !IsServer)
        {
            return;
        }

        if (IsOwner)
        {
            primaryInput = InputManager.PrimaryInput && !FireBlocked;
            secondaryInput = InputManager.SecondaryInput && !FireBlocked;


            weaponSwitchInput = InputManager.WeaponSwitchInput;
            reloadInput = InputManager.ReloadInput;


            if (primaryInput != lastPrimary || secondaryInput != lastSecondary || reloadInput != lastReload)
            {
                SendInputToServer_RPC(primaryInput, secondaryInput, reloadInput);
            }
        }

        if(CurrentWeapon != null)
        {
            if(!switchingWeapons && weaponSwitchInput)
            {
                CurrentWeapon.TriggerAnimation(BaseNetWeapon.CHANGEWEAPON, BaseNetWeapon.TRIGGERTIMETINY, true);
                if (IsOwner)
                {
                    nextWeaponIndex.Anticipate((nextWeaponIndex.Value + 1) % weapons.Count);
                }
                switchingWeapons = true;
                return;
            }
            if (reloadInput)
            {
                if(CurrentWeapon.useAmmunition && (CurrentWeapon.CurrentAmmo.Value < CurrentWeapon.maxAmmo) || (CurrentWeapon.useAmmoPhases && CurrentWeapon.currentAmmoPhase != 0)){
                    CurrentWeapon.TriggerAnimation(CurrentWeapon.CurrentAmmo.Value > 0 ? BaseNetWeapon.PARTIALRELOAD : BaseNetWeapon.EMPTYRELOAD, 0.2f, true);
                }
                if (IsOwner)
                {
                    InputManager.ReloadInput = false;
                }
                reloadInput = false;
            }
        }

        player.motor.aiming = secondaryInput;

        UpdateFOV();
        UpdateWeaponOrientation();


        base.LUpdate();
    }

    public void UpdateWeaponOrientation()
    {
        if (CurrentWeapon == null || CurrentWeapon.aimParams == null || weaponTargetTransform == null || weaponPositionOffset == null || weaponRotationInvert == null)
        {
            //Invalid setup, cannot do anything
            if (aimLerp > 0)
            {
                aimAmount = Mathf.MoveTowards(aimAmount, 0, player.motor.viewParams.fovMoveSpeed * Time.deltaTime);
                aimLerp = Mathf.Lerp(aimLerp, aimAmount, Time.deltaTime * player.motor.viewParams.fovMoveSpeed);
            }
            return;
        }

        aimAmount = Mathf.MoveTowards(aimAmount, player.motor.aiming ? 1 : 0, CurrentWeapon.aimParams.aimSpeed * Time.deltaTime);
        //aimLerp = Mathf.Lerp(aimLerp, aimAmount, Time.deltaTime * CurrentWeapon.aimParams.aimSpeed);
        aimLerp = CurrentWeapon.aimParams.aimLerpCurve.Evaluate(aimAmount);
        float crouchLerp = CurrentWeapon.aimParams.crouchLerpCurve.Evaluate(player.motor.currentCrouchLerp);

        //We need to scale the local position of the weapon target, and apply that to the weapon offset
        weaponPositionOffset.localPosition = CurrentWeapon.aimParams.crouchPositionOffset * (crouchLerp * (1 - (aimLerp * CurrentWeapon.aimParams.aimRotationReduction)))
            + (CurrentWeapon.aimParams.baseAimPositionOffset +
            weaponTargetTransform.localPosition.Multiply(CurrentWeapon.aimParams.aimedWeaponPositionScale) +
            (CurrentWeapon.aimParams.aimPositionOffsetAngled * (1 - CurrentWeapon.aimParams.aimRotationReduction))) * aimLerp;


        weaponRotationInvert.localRotation = Quaternion.Lerp(
            Quaternion.Lerp(Quaternion.identity, CurrentWeapon.aimParams.crouchRotationOffset, crouchLerp),
            Quaternion.Inverse(weaponTargetTransform.localRotation) * CurrentWeapon.aimParams.aimRotationOffset,
            aimLerp * CurrentWeapon.aimParams.aimRotationReduction);
    }

    public void UpdateFOV()
    {

        //The mother of all ternary statements...
        float fov =
            //Are we sliding
            player.motor.sliding ? player.motor.viewParams.slideFOV :
            //Are we sprinting or sliding?
            ((player.motor.sprinting && player.motor.moveInput != Vector2.zero) || player.motor.sliding) ? player.motor.viewParams.sprintFOV :
            //Are we moving normally or crouching?
            0;
        //currentFOV = Mathf.Lerp(rbpm.viewParams.baseFOV, rbpm.viewParams.baseFOV + fov, aimLerp);
        fovLerp = Mathf.Lerp(fovLerp, fov + (CurrentWeapon != null ? CurrentWeapon.aimParams.aimFOV * aimLerp : 0), Time.deltaTime * player.motor.viewParams.fovMoveSpeed);
        currentFOV = player.motor.viewParams.baseFOV + fovLerp;
        //rbpm.viewCineCam.m_Lens.FieldOfView = Mathf.Lerp(rbpm.viewParams.viewmodelBaseFOV, rbpm.viewParams.viewmodelBaseFOV + currentWeapon.aimParams.viewmodelFOV, aimLerp);
        player.motor.viewCamera.m_Lens.FieldOfView = player.motor.viewParams.viewmodelBaseFOV + 
            (CurrentWeapon != null ? (CurrentWeapon.aimParams.viewmodelFOV * aimLerp) : 0);
        player.motor.worldCamera.m_Lens.FieldOfView = currentFOV;
    }

    [Rpc(SendTo.Server)]
    void SendInputToServer_RPC(bool primary, bool secondary, bool reload)
    {
        primaryInput = primary;
        secondaryInput = secondary;
        reloadInput = reload;
    }
}
