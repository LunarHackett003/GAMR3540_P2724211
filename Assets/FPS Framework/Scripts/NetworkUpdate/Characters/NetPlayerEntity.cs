using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The class that ties together all of the player related systems.
/// </summary>
public class NetPlayerEntity : NetEntity
{





    /// <summary>
    /// A dictionary of all players by their Client ID. Used for quick player lookups.
    /// </summary>
    internal static Dictionary<ulong, NetPlayerEntity> playersByID = new();


    [SerializeField] internal NetworkTimer netTimer;

    [SerializeField] internal NetPlayerMotor motor;

    [SerializeField] internal NetBufferManager bufferManager;

    [SerializeField] internal NetPlayerWeaponController weaponController;

    [SerializeField] internal Camera viewmodelCamera;

    [SerializeField] internal Renderer[] allRenderers;

    [SerializeField] internal float quickReviveHealthPortion = 0.5f;

    [SerializeField] internal NetworkObject playerReviveItemPrefab;

    internal NetworkObject reviveItemInstance;

    internal NetWeaponAnimator Animator => weaponController.animator;

    public ParticleSystem deathParticle;

    [SerializeField] internal InteractableObject currentInteractTarget;
    [SerializeField] internal InteractableObject carryTargetRequested;

    [SerializeField] internal Rigidbody interactTargetRigidbody;

    [SerializeField] internal bool carryConfirmed;

    [SerializeField] internal bool heldInteraction;

    [SerializeField] internal CapsuleCollider capsule;


    [SerializeField] internal InteractionConfig interactConfig;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        playersByID.Add(OwnerClientId, this);

        if (IsOwner)
        {
            if(GameplayCanvas.Instance != null)
                GameplayCanvas.player = this;
            Camera.main.GetUniversalAdditionalCameraData().cameraStack.Add(viewmodelCamera);
        }

        ;

        weaponController.Initialise();
    }

    public override void LTimestep()
    {
        base.LTimestep();

        if (!IsOwner)
            return;


        if (isDead.Value)
        {
            if (carryTargetRequested != null && carryConfirmed)
            {
                carryTargetRequested.GrabReleased_RPC(Vector3.zero, carryTargetRequested.transform.position);
                ReleaseGrabbedObject_RPC(carryTargetRequested, false);
            }
            if (heldInteraction && currentInteractTarget != null)
            {
                currentInteractTarget.InteractEnd_RPC(false);
            }
        }
        else
        {
            if (!heldInteraction && !carryConfirmed)
            {
                if (Physics.SphereCast(weaponController.fireOrigin.position, interactConfig.interactThickness, weaponController.fireOrigin.forward, out RaycastHit hit, interactConfig.maxInteractDistance, interactConfig.interactLayerMask))
                {
                    if (hit.rigidbody != null)
                    {
                        if(interactTargetRigidbody == null || hit.rigidbody != interactTargetRigidbody)
                        {
                            interactTargetRigidbody = hit.rigidbody;
                        }
                    }
                    else
                    {
                        interactTargetRigidbody = null;
                    }
                }
                else
                {
                    interactTargetRigidbody = null;
                }
            }

            if (interactTargetRigidbody != null && (!heldInteraction || !carryConfirmed) && interactTargetRigidbody.TryGetComponent(out currentInteractTarget))
            {
                bool canUse = currentInteractTarget.CanInteract(OwnerClientId);
                if (InputManager.InteractInput && currentInteractTarget.hasInteraction && canUse)
                {
                    if (currentInteractTarget.holdToInteract)
                    {
                        heldInteraction = true;
                    }
                    else
                    {
                        InputManager.InteractInput = false;
                    }
                    currentInteractTarget.InteractStart_RPC(OwnerClientId);
                }

                if(InputManager.GrabInput && currentInteractTarget.canCarry && canUse)
                {
                    InputManager.GrabInput = false;
                    InputManager.PrimaryInput = false;
                    InputManager.SecondaryInput = false;
                    carryTargetRequested = currentInteractTarget;
                    carryTargetRequested.TryGrab_RPC(OwnerClientId);
                }
            }

            if (currentInteractTarget != null && heldInteraction && !InputManager.InteractInput)
            {
                currentInteractTarget.InteractEnd_RPC(false);
            }




            if (carryTargetRequested != null && carryConfirmed)
            {
                if(OwnerClientId == carryTargetRequested.OwnerClientId)
                {
                    carryTargetRequested.rb.Move(Vector3.Lerp(carryTargetRequested.rb.position, weaponController.fireOrigin.TransformPoint(interactConfig.grabbedObjectOffsetFromWeaponPoint), interactConfig
                        .interactedObjectMoveSpeed * Time.fixedDeltaTime),
                        interactConfig.interactRotateUseSlerp ? Quaternion.Slerp(carryTargetRequested.rb.rotation, weaponController.fireOrigin.rotation, 
                        Time.fixedDeltaTime * interactConfig.interactedObjectRotateSpeed) 
                        : Quaternion.Lerp(carryTargetRequested.rb.rotation, weaponController.fireOrigin.rotation, Time.fixedDeltaTime * interactConfig.interactedObjectRotateSpeed));

                    
                    if (InputManager.PrimaryInput && carryConfirmed && carryTargetRequested != null)
                    {
                        carryTargetRequested.GrabReleased_RPC(weaponController.fireOrigin.forward * interactConfig.throwForce, carryTargetRequested.transform.position);
                        ReleaseGrabbedObject_RPC(carryTargetRequested, true);
                        InputManager.PrimaryInput = false;
                        carryConfirmed = false;
                        carryTargetRequested = null;
                    }
                    if (InputManager.SecondaryInput && carryConfirmed && carryTargetRequested != null)
                    {
                        carryTargetRequested.GrabReleased_RPC(Vector3.zero, carryTargetRequested.transform.position);
                        ReleaseGrabbedObject_RPC(carryTargetRequested, false);
                        InputManager.SecondaryInput = false;
                        carryConfirmed = false;
                        carryTargetRequested = null;
                    }
                }
            }


        }
    }

    public override void OnNetworkDespawn()
    {
        playersByID.Remove(OwnerClientId);
        base.OnNetworkDespawn();
    }

    public override void DamageableDied(NetworkBehaviourReference sourceObj, bool isCrit)
    {
        base.DamageableDied(sourceObj, isCrit);

        deathParticle.Play();

        for (int i = 0; i < weaponController.weapons.Count; i++)
        {
            weaponController.ShowWeapon(0, true);
        }

        if (IsServer)
        {
            reviveItemInstance = NetworkManager.SpawnManager.InstantiateAndSpawn(playerReviveItemPrefab, OwnerClientId, position: transform.position);
        }

        capsule.enabled = false;

        ToggleRenderers(false);

    }

    public virtual void Revive_RPC(ulong helperClientID, bool quickRevive)
    {
        if (quickRevive)
        {
            ModifyHealth(maxHealth * (quickRevive ? quickReviveHealthPortion : 1));
        }

        weaponController.ShowWeapon(weaponController.weaponIndex.Value, false);

        if (IsServer)
        {
            reviveItemInstance.Despawn();
        }

        capsule.enabled = true;

        ToggleRenderers(true);
    }

    public void ToggleRenderers(bool enabled)
    {
        for (int i = 0; i < allRenderers.Length; i++)
        {
            allRenderers[i].enabled = enabled;
        }
    }

    public void InteractionCompleted(bool holdInteraction, bool finished)
    {
        heldInteraction = false;
        currentInteractTarget = null;
    }

    [Rpc(SendTo.Everyone)]
    public void ConfirmGrabRequest_RPC(NetworkBehaviourReference objectTriedToGrab)
    {
        Debug.Log("Confirmed Grab Request!");
        if(objectTriedToGrab.TryGet(out InteractableObject io))
        {
            if (IsOwner)
            {
                if(io == carryTargetRequested)
                {
                    carryConfirmed = true;
                }
            }
            io.GrabbedCarriable();
            weaponController.ShowWeapon(0, true);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void ReleaseGrabbedObject_RPC(NetworkBehaviourReference objectReleased, bool thrown = true)
    {
        carryConfirmed = false;

        if (IsOwner)
        {
            carryTargetRequested = null;
        }
        weaponController.ShowWeapon(weaponController.weaponIndex.Value);
    }
}
