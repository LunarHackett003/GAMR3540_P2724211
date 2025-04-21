using UnityEngine;

[CreateAssetMenu(fileName = "AimParams", menuName = "Scriptable Objects/AimParams")]
public class AimParams : ScriptableObject
{
    [Tooltip("This value is subtracted from the Base FOV when it is in effect.")] public float aimFOV = -10, altAimFOV = -5;
    [Tooltip("How quickly, per degree of FOV in the transition, your view moves towards the target fov.")] public float aimSpeed = 15;

    [Tooltip("The linear offset to apply to the weapon when aiming")] public Vector3 baseAimPositionOffset = Vector3.zero;
    [Tooltip("The scale for the linear offset when aiming")] public Vector3 aimedWeaponPositionScale = Vector3.one;
    [Tooltip("The linear offset to apply to the weapon when aiming")] public Vector3 aimPositionOffsetAngled = Vector3.zero;
    [Tooltip("How much of the weapon's rotation to remove, from 0 to 1"), Range(0, 1)] public float aimRotationReduction = 0f;
    [Tooltip("The additive fov of the viewmodel camera when aiming")] public float viewmodelFOV = -20;
}
