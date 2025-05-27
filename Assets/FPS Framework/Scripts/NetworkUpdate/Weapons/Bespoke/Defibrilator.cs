using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Defibrilator : BaseNetWeapon
{
    [SerializeField] internal float defibRange;
    [SerializeField] internal float defibThickness;
    [SerializeField] internal float defibDamageOnEnemyHit;

    [SerializeField] internal LayerMask defibLayerMask;

    protected override bool ChargeInput => base.ChargeInput && (CurrentAmmo.Value > 0 || CurrentAmmo.Value > 0);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        CurrentAmmo.OnValueChanged += AmmoChanged;
    }
    void AmmoChanged(float previous, float current)
    {
        if (current > 0)
            fired = false;
    }


    protected override void UpdateCharge()
    {
        base.UpdateCharge();

        if (IsOwner && chargeHoldFrame && CurrentAmmo.Value > 0 && !fired)
        {
            fired = true;
            if (!IsServer)
            {
                PostAttackBehaviour();
            }
            TryDefib_RPC(controller.fireOrigin.position, controller.fireOrigin.forward);
            chargeAmount = 0;
            chargeHoldFrame = false;
            TriggerAnimation(PRIMARYATTACK, TRIGGERTIMESHORT, true);
        }
    }

    [Rpc(SendTo.Server)]
    public void TryDefib_RPC(Vector3 pos, Vector3 forward)
    {
        if(Physics.SphereCast(pos, defibThickness, forward, out RaycastHit hit, defibRange, defibLayerMask))
        {
            if(hit.rigidbody != null)
            {
                if (hit.rigidbody.TryGetComponent(out ReviveTrophy trophy) && trophy.friendly)
                {
                    if (trophy.HitByQuickRevive(OwnerClientId))
                    {
                        Debug.Log("Defibrillated!");
                    }
                }
                else if(hit.rigidbody.TryGetComponent(out NetPlayerEntity player))
                {
                    if (!NetworkPlayer.IsPlayerOnMyTeam(OwnerClientId, player.OwnerClientId))
                    {
                        player.ModifyHealth(-defibDamageOnEnemyHit, this, DamageSourceType.weapon, false);
                    }
                }
                Debug.Log("Defibrillated!");
                equipmentCharges.Value--;
            }
        }
        PostAttackBehaviour();
    }
}
