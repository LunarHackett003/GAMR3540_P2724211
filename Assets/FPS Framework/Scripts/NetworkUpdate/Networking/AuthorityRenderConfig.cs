using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class AuthorityRenderConfig : NetworkBehaviour
{
    [System.Serializable]
    public struct RendererConfig
    {
        //public ShadowCastingMode shadowCastingMode;
        //public bool shadowsOnly;
        public ShadowCastingMode shadowMode;
        public bool updateOffscreen;
        public bool canSee;
    }
    public RendererConfig ownerConfig, remoteConfig;
    public MeshRenderer meshRenderer;
    public SkinnedMeshRenderer skinnedMeshRenderer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        SetRendererConfig(IsOwner ? ownerConfig : remoteConfig);
    }
    void SetRendererConfig(RendererConfig config)
    {
        if (skinnedMeshRenderer)
        {
            skinnedMeshRenderer.shadowCastingMode = config.shadowMode;
            skinnedMeshRenderer.updateWhenOffscreen = config.updateOffscreen;
            skinnedMeshRenderer.enabled = config.canSee;
        }
        if (meshRenderer)
        {
            meshRenderer.enabled = config.canSee;
            meshRenderer.shadowCastingMode = config.shadowMode;
        }
    }
}
