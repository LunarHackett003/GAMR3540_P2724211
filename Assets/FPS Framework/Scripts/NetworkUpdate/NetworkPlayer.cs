using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayer : LunarNetScript
{
    public ProjectileSimulator projectileSimulatorPrefab;

    public static NetworkPlayer localNetworkPlayer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            NetworkManager.SpawnManager.InstantiateAndSpawn(projectileSimulatorPrefab.NetworkObject, destroyWithScene: true);
        }
        if (IsOwner)
        {
            localNetworkPlayer = this;
        }
    }
}
