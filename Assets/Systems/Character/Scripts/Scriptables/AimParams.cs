using UnityEngine;

[CreateAssetMenu(fileName = "AimParams", menuName = "Scriptable Objects/AimParams")]
public class AimParams : ScriptableObject
{
    [Tooltip("This value is subtracted from the Base FOV when it is in effect.")] public float aimFOV = -10, altAimFOV = -5;
    [Tooltip("How quickly, per degree of FOV in the transition, your view moves towards the target fov.")] public float fovMoveSpeed = 0.02f;
}
