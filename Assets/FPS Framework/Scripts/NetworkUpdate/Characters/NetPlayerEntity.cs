using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        playersByID.Add(OwnerClientId, this);

        if (IsOwner && GameplayCanvas.Instance != null)
        {
            GameplayCanvas.player = this;
        }

        weaponController.Initialise();
    }

    public override void OnNetworkDespawn()
    {
        playersByID.Remove(OwnerClientId);
        base.OnNetworkDespawn();
    }
}
