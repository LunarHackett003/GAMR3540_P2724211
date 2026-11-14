using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[Obsolete("Authority Layer Set behaviour has been replaced by FOV Shader usage. Please remove Authority Layer Set components from object.")]
public class AuthorityLayerSet : NetworkBehaviour
{
    public int ownerLayerIndex, remoteLayerIndex;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        gameObject.layer = IsOwner ? ownerLayerIndex : remoteLayerIndex;
    }
}
