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
    public static Dictionary<ulong, NetPlayerEntity> playersByID = new();

    [SerializeField] internal NetPlayerMotor motor;




    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        playersByID.Add(OwnerClientId, this);
    }

    public override void OnNetworkDespawn()
    {
        playersByID.Remove(OwnerClientId);
        base.OnNetworkDespawn();
    }
}
