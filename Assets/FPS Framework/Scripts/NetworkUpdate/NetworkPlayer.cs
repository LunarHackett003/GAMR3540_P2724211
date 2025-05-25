using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class NetworkPlayer : LunarNetScript
{

    public NetworkObject projectileSimulatorPrefab;

    public static NetworkPlayer LocalNetworkPlayer;
    // Since some things are held on the Server's Network Player
    public static NetworkPlayer ServerNetworkPlayer;

    public static Dictionary<ulong, NetworkPlayer> netPlayers = new Dictionary<ulong, NetworkPlayer>();

    internal NetworkVariable<int> teamIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public static Dictionary<ulong, int> playersOnTeams = new Dictionary<ulong, int>();

    public NetworkVariable<int> serverTeamCount = new(2, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkObject playerPrefab;

    public TestLoadoutWeaponCollection weaponList;

    public static bool GetPlayerTeam(ulong clientID, out int index)
    {
        if (netPlayers.TryGetValue(clientID, out NetworkPlayer npe))
        {
            index = npe.teamIndex.Value;
            return true;
        }
        else
        {
            index = -1;
            return false;
        }
    }
    public static bool IsPlayerOnMyTeam(ulong myID, ulong theirID)
    {
        GetPlayerTeam(myID, out int myteam);
        GetPlayerTeam(theirID, out int theirTeam);

        return myteam == theirTeam && myteam != -1 && theirTeam != -1;
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && IsOwner)
        {
            NetworkManager.SpawnManager.InstantiateAndSpawn(projectileSimulatorPrefab, destroyWithScene: true);
        }
        if (IsServer)
        {
            DetermineTeamForPlayer();
        }

        if (IsOwner)
        {
            LocalNetworkPlayer = this;
        }
        if (!IsOwnedByServer && !IsHost)
        {
            netPlayers.Add(OwnerClientId, this);
        }
        teamIndex.OnValueChanged += TeamsUpdated;

        if (IsOwnedByServer)
        {
            ServerNetworkPlayer = this;
        }

    }

    public void TeamsUpdated(int previous, int current)
    {
        if (!playersOnTeams.ContainsKey(OwnerClientId))
        {
            playersOnTeams.Add(OwnerClientId, current);
        }
        else
        {
            playersOnTeams[OwnerClientId] = current;
        }
    }

    public void DetermineTeamForPlayer()
    {
        //We need to check how many players are on each team
        //We can remove the hardcoded team limit later on, but for now we'll work with 2
        int smallestTeamIndex = 0;
        if(playersOnTeams.Count > 0)
        {
            List<int> teamPlayerCounts = new List<int>(serverTeamCount.Value);
            foreach (var item in playersOnTeams)
            {
                teamPlayerCounts[item.Value]++;
            }
            
            int smallestTeamCount = 999;
            
            for (int i = 0; i < teamPlayerCounts.Count; i++)
            {
                if (teamPlayerCounts[i] < smallestTeamCount)
                {
                    smallestTeamIndex = i;
                    smallestTeamCount = teamPlayerCounts[i];
                }
            }
        }

        teamIndex.Value = smallestTeamIndex;
    }

    public override void OnNetworkDespawn()
    {
        netPlayers.Remove(OwnerClientId);

        base.OnNetworkDespawn();
    }

    [Rpc(SendTo.Server)]
    public void SpawnPlayer_RPC(int[] slots, RpcParams parameters = default)
    {
        ulong clientID = parameters.Receive.SenderClientId;
        if (NetPlayerEntity.playersByID.ContainsKey(clientID))
        {
            NetPlayerEntity thisplayer = NetPlayerEntity.playersByID[clientID];

            foreach (var item in thisplayer.weaponController.weapons)
            {
                item.NetworkObject.Despawn();
            }
            thisplayer.NetworkObject.Despawn();
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
