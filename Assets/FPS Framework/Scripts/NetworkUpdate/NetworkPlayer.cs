using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPlayer : LunarNetScript
{
    public NetworkObject projectileSimulatorPrefab;

    public static NetworkPlayer LocalNetworkPlayer;

    public NetworkObject playerPrefab;

    public TestLoadoutWeaponCollection weaponList;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            NetworkManager.SpawnManager.InstantiateAndSpawn(projectileSimulatorPrefab, destroyWithScene: true);
        }
        if (IsOwner)
        {
            LocalNetworkPlayer = this;
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnPlayer_RPC(int[] slots, RpcParams parameters = default)
    {
        ulong clientID = parameters.Receive.SenderClientId;
        if (NetPlayerEntity.playersByID.ContainsKey(clientID))
        {
            NetPlayerEntity.playersByID[clientID].NetworkObject.Despawn();
        }

        NetworkObject player = NetworkManager.SpawnManager.InstantiateAndSpawn(playerPrefab, clientID);

        for (int i = 0; i < slots.Length; i++)
        {
            if(slots[i] < 0)
            {
                //Invalid weapon index
                continue;
            }
            NetworkObject nob = NetworkManager.SpawnManager.InstantiateAndSpawn(weaponList.weapons[slots[i]].GetComponent<NetworkObject>(), clientID);
            nob.TrySetParent(player);
        }

    }
}
