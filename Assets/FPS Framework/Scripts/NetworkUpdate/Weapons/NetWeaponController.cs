using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class NetWeaponController : LunarNetScript
{
    [SerializeField] internal bool primaryInput, secondaryInput;
    internal bool lastPrimary, lastSecondary, lastReload;
    [SerializeField] internal List<BaseNetWeapon> weapons;
    [SerializeField]
    internal AnticipatedNetworkVariable<int> weaponIndex = new(0, StaleDataHandling.Reanticipate);
    [SerializeField]
    internal AnticipatedNetworkVariable<int> nextWeaponIndex = new(0, StaleDataHandling.Reanticipate);
    public BaseNetWeapon CurrentWeapon => weapons[weaponIndex.Value];

    internal Collider[] colliders;
    internal HashSet<Collider> colliderSet; 
    internal Transform fireOrigin;

    internal bool FireBlocked => fireBlockedByAnimation;
    [SerializeField] internal bool fireBlockedByAnimation;
    internal float aimLerp = 0;
    internal float aimAmount;
    internal bool switchingWeapons;

    [SerializeField] internal NetWeaponAnimator animator;

    public virtual float Spread(float value) => value * (1 - aimAmount);


    public virtual void Initialise()
    {
        colliderSet = new(colliders);
        if (weapons.Count == 0)
        {
            weapons.AddRange(GetComponentsInChildren<BaseWeapon>());
            //We have no weapons, exit early.
            if (weapons.Count == 0)
                return;
            //ChangeCurrentWeapon(weapons[0], out _, out _);

            for (int i = 0; i < weapons.Count; i++)
            {
                //if (weapons[i].animator != null)
                //{
                //    weapons[i].animator.controller = this;
                //    weapons[i].animator.Initialise();
                //}
                weapons[i].InitialiseWeapon(this);
                if (i == 0)
                    continue;

                ShowWeapon(weapons[i].gameObject, false);
            }
        }
    }
    public void ShowWeapon(GameObject weapon, bool show)
    {
        weapon.transform.localScale = show ? Vector3.one : Vector3.zero;
    }

    public virtual void ChangeCurrentWeapon(BaseNetWeapon newWeapon, out BaseNetWeapon oldWeapon, out bool success)
    {
        oldWeapon = CurrentWeapon;
        success = newWeapon != null && newWeapon != CurrentWeapon;

        if (oldWeapon != null)
            ShowWeapon(oldWeapon.gameObject, false);
        ShowWeapon(newWeapon.gameObject, true);

        //if (animator != null)
        //    animator.UpdateAnimations();

    }
    public virtual void SwitchToWeaponIndex(int index)
    {
        switchingWeapons = false;
        ChangeCurrentWeapon(weapons[index], out _, out bool success);
        if (success)
        {
            UpdateWeaponIndex_RPC(weaponIndex.AuthoritativeValue, index);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void UpdateWeaponIndex_RPC(int oldIndex, int newIndex)
    {
        if (IsServer)
        {
            weaponIndex.AuthoritativeValue = oldIndex;
            nextWeaponIndex.AuthoritativeValue = newIndex;
        }
        else
        {
            weaponIndex.Anticipate(oldIndex);
            nextWeaponIndex.Anticipate(newIndex);
        }
    }
    public virtual void WeaponIndexUpdated()
    {
        
    }
    public virtual void ReceivePostAttack(BaseNetWeapon weapon)
    {

    }
}
