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

    public void InitialiseAfterSpawningWeapons()
    {

    }

    public override void LTimestep()
    {

        
        if (IsOwner)
        {
            primaryInput = InputManager.PrimaryInput;
            secondaryInput = InputManager.SecondaryInput;
            reloadInput = InputManager.ReloadInput;
            //Implement later
            //meleeInput = InputManager.MeleeInput;
            weaponSwitchInput = InputManager.WeaponSwitchInput;


            if(primaryInput != lastPrimary || secondaryInput != lastSecondary || reloadInput != lastReload)
            {
                SendInputToServer_RPC(primaryInput, secondaryInput, reloadInput);
            }
        }


    }

    [Rpc(SendTo.Server)]
    void SendInputToServer_RPC(bool primary, bool secondary, bool reload)
    {
        primaryInput = primary;
        secondaryInput = secondary;
        reloadInput = reload;
    }
}
