using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReviveTrophy : InteractableObject
{

    public MeshRenderer renderer;
    public Material friendlyMaterial, enemyMaterial;

    protected bool friendly;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        NetworkPlayer.GetPlayerTeam(NetworkManager.LocalClientId, out int reviveTeam);
        NetworkPlayer.GetPlayerTeam(OwnerClientId, out int myTeam);

        friendly = reviveTeam == myTeam && reviveTeam != -1 && myTeam != -1;

        renderer.material = friendly ? friendlyMaterial : enemyMaterial;
    }


    public override void InteractionCompleted()
    {
        base.InteractionCompleted();

        NetPlayerEntity.playersByID[OwnerClientId].Revive_RPC(currentInteractor.OwnerClientId, false);
    }

    public bool HitByQuickRevive(ulong clientID)
    {
        if (friendly)
        {
            NetPlayerEntity.playersByID[OwnerClientId].Revive_RPC(clientID, true);
            return true;
        }
        return false;

    }
}
