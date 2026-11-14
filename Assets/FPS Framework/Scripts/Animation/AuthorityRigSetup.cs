using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AuthorityRigSetup : LunarNetScript
{
    [SerializeField] MultiPositionConstraint leftShoulderOffset, rightShoulderOffset, viewmodelRoot, topSpine;
    [SerializeField] MultiRotationConstraint lowerSpine;

    [SerializeField] Vector3 lowerSpineRotationOffset;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SetupRig();
    }

    void SetupRig()
    {
        if (leftShoulderOffset != null)
            leftShoulderOffset.weight = IsOwner ? 1 : 0;
        if (rightShoulderOffset != null)
            rightShoulderOffset.weight = IsOwner ? 1 : 0;
    }

    private void OnValidate()
    {
        if(lowerSpine != null)
            lowerSpine.data.offset = lowerSpineRotationOffset;
    }
}
