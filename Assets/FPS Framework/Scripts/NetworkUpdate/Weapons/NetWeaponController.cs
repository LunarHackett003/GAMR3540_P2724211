using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class NetWeaponController : LunarNetScript
{
    [SerializeField] internal bool primaryInput, secondaryInput;
    internal bool lastPrimary, lastSecondary, lastReload;
    internal int previousWeaponCount;

    [SerializeField] internal List<BaseNetWeapon> weapons;
    [SerializeField] internal int lastWeaponCount;
    [SerializeField]
    internal AnticipatedNetworkVariable<int> weaponIndex = new(0, StaleDataHandling.Reanticipate);
    [SerializeField]
    internal AnticipatedNetworkVariable<int> nextWeaponIndex = new(0, StaleDataHandling.Reanticipate);
    public BaseNetWeapon CurrentWeapon => weaponIndex.Value < weapons.Count ? weapons[weaponIndex.Value] : null;

    [SerializeField] BaseNetWeapon currentWeapon;

    [SerializeField] internal Collider[] colliders;
    internal HashSet<Collider> colliderSet; 
    [SerializeField] internal Transform fireOrigin;

    internal virtual bool FireBlocked => fireBlockedByAnimation;
    [SerializeField] internal bool fireBlockedByAnimation;
    internal float aimLerp = 0;
    internal float aimAmount;
    internal bool switchingWeapons;

    [SerializeField] internal NetWeaponAnimator animator;

    [SerializeField] internal bool hideWeapons;
    internal bool weaponsHiddenLast;
    public virtual float Spread(float value) => value * (1 - aimAmount);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        colliderSet = new(colliders);
    }

    public void WeaponAdded(BaseNetWeapon weapon)
    {
        if (!weapons.Contains(weapon))
            weapons.Add(weapon);
        weapon.InitialiseWeapon(this);

        ShowWeapon(weaponIndex.Value, false);

        RePollWeapons();
    }
    public void RePollWeapons()
    {
        Debug.Log("Repolling weapons");
        if (lastWeaponCount == 0)
        {
            Debug.Log("Repolled weapons and re-initialised");
            lastWeaponCount = weapons.Count;
            Initialise();
        }
    }
    public virtual void Initialise()
    {
        animator.Initialise();
        if (weapons.Count > 0)
        {
            ChangeCurrentWeapon(weapons[0], out _, out _);

            ShowWeapon(0, false);
        }
        lastWeaponCount = weapons.Count;
    }

    public override void LUpdate()
    {
        if(weaponsHiddenLast != hideWeapons)
        {
            ShowWeapon(weaponIndex.Value, hideWeapons);
            weaponsHiddenLast = hideWeapons;
        }



        if(CurrentWeapon != null)
        {
            if(lastPrimary != primaryInput)
            {
                CurrentWeapon.primaryInput = primaryInput;
                lastPrimary = primaryInput;
            }
            if(lastSecondary != secondaryInput)
            {
                CurrentWeapon.secondaryInput = secondaryInput;
                lastSecondary = secondaryInput;
            }
        }
    }


    public override void LTimestep()
    {
        base.LTimestep();

        if (lastWeaponCount != weapons.Count)
        {
            RePollWeapons();
            lastWeaponCount = weapons.Count;
        }
    }

    public void ShowWeapon(int indexToShow, bool hideAll = false)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            weapons[i].transform.localScale = hideAll ? Vector3.zero : (i == indexToShow ? Vector3.one : Vector3.zero);
        }
    }

    public virtual void ChangeCurrentWeapon(BaseNetWeapon newWeapon, out BaseNetWeapon oldWeapon, out bool success)
    {
        oldWeapon = CurrentWeapon;
        currentWeapon = newWeapon;
        success = newWeapon != null && newWeapon != CurrentWeapon;

        if (animator != null)
            animator.UpdateAnimations();

        switchingWeapons = false;
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
            weaponIndex.AuthoritativeValue = newIndex;
            nextWeaponIndex.AuthoritativeValue = newIndex;
        }
        else
        {
            weaponIndex.Anticipate(newIndex);
            nextWeaponIndex.Anticipate(newIndex);
        }

        animator.UpdateAnimations();
        ShowWeapon(newIndex, false);
    }
    public virtual void WeaponIndexUpdated()
    {
        
    }
    public virtual void ReceivePostAttack(BaseNetWeapon weapon)
    {

    }
}
