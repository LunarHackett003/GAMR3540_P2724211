using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements;

public class PlayerWeaponController : WeaponController
{
    public bool myPlayer;
    [SerializeField] protected RBPlayerMotor rbpm;
    internal float currentFOV;
    [SerializeField] internal Transform weaponPositionOffset, weaponRotationInvert, weaponTargetTransform;
    Vector3 weaponPosStart;
    internal float aimLerp = 0;

    private void Start()
    {
        weaponPosStart = weaponPositionOffset.localPosition;
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
        primaryInput = InputManager.PrimaryInput;
        secondaryInput = InputManager.SecondaryInput;

        base.LUpdate();

        
        //switch (currentWeapon)
        //{
        //    case RangedWeapon rw:
        //        break;
        //    default:
        //        break;
        //}
        //moved FOV update to Player Weapon Controller
        UpdateFOV();
        UpdateWeaponOrientation();
    }
    public void UpdateWeaponOrientation()
    {
        if(currentWeapon == null || weaponTargetTransform == null || weaponPositionOffset == null || weaponRotationInvert == null)
        {
            //Invalid setup, cannot do anything
            return;
        }
        
        weaponPositionOffset.localPosition = Vector3.Lerp(weaponPosStart, weaponPosStart + rbpm.aimParams.baseAimPositionOffset + 
            (rbpm.aimParams.aimPositionOffsetAngled * (1 - rbpm.aimParams.aimRotationReduction)), aimLerp);
        weaponRotationInvert.localRotation = Quaternion.Lerp(Quaternion.identity, Quaternion.Inverse(weaponTargetTransform.localRotation), aimLerp * rbpm.aimParams.aimRotationReduction);
    }

    public void UpdateFOV()
    {

        //The mother of all ternary statements...
        float fov = rbpm.viewParams.baseFOV +
            //Are we dashing?
            (rbpm.dashing ? rbpm.dashCurrentFOV :
            //Do we have a weapon?
            currentWeapon != null && currentWeapon.aimOnSecondary ? (rbpm.altAiming ? rbpm.aimParams.altAimFOV : rbpm.aiming ? rbpm.aimParams.aimFOV : 0) : 
            //Are we sliding
            rbpm.isSliding ? rbpm.viewParams.slideFOV :
            //Are we sprinting or sliding?
            ((rbpm.sprinting && InputManager.MoveInput != Vector2.zero) || rbpm.isSliding) ? rbpm.viewParams.sprintFOV :
            //Are we moving normally or crouching?
            0);
        aimLerp = Mathf.Lerp(aimLerp, rbpm.aimAmount, Time.deltaTime * rbpm.aimParams.fovMoveSpeed);
        currentFOV = Mathf.Lerp(currentFOV, fov, Time.deltaTime * rbpm.aimParams.fovMoveSpeed);
        rbpm.viewCineCam.m_Lens.FieldOfView = Mathf.Lerp(rbpm.viewParams.viewmodelBaseFOV, rbpm.viewParams.viewmodelBaseFOV + rbpm.aimParams.viewmodelFOV, aimLerp);
        rbpm.worldCineCam.m_Lens.FieldOfView = currentFOV;
    }
}
