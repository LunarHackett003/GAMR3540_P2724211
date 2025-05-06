using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponModule : UIModule
{
    BaseWeapon Weapon => GameplayCanvas.playerController.currentWeapon;
    float lastAmmo; int lastAmmoPhase;
    BaseWeapon lastWeapon;
    public TMP_Text weaponNameText, weaponAmmoText;
    public GameObject weaponPhaseParent;
    public Image[] weaponPhaseIndicators;
    public Color ammoPhaseDefaultColour = Color.white, ammoPhasePassedColour = Color.grey;
    public override void UpdateModule()
    {
        if(Weapon == null)
        {
            return;
        }

        if (Weapon.useAmmunition)
        {
            UpdatePhaseDisplay();
        }
        if(lastWeapon != Weapon)
        {
            lastWeapon = Weapon;
            weaponNameText.text = Weapon.displayName;
            UpdatePhaseDisplay();
        }
    }
    void UpdatePhaseDisplay()
    {
        bool @override = lastWeapon != Weapon;
        if (lastAmmo != Weapon.currentAmmo || Weapon.currentAmmoPhase != lastAmmoPhase || @override)
        {
            lastAmmo = Weapon.currentAmmo;
            weaponAmmoText.text = $"{Weapon.currentAmmo:0}/{Weapon.maxAmmo}";
            if (Weapon.useAmmoPhases)
            {
                weaponPhaseParent.SetActive(true);
                if (lastAmmoPhase != Weapon.currentAmmoPhase || @override)
                {
                    lastAmmoPhase = Weapon.currentAmmoPhase;
                    for (int i = 0; i < weaponPhaseIndicators.Length; i++)
                    {
                        Image img = weaponPhaseIndicators[i];
                        img.gameObject.SetActive(i < Weapon.ammoPhases);
                        img.color = i >= Weapon.currentAmmoPhase ? ammoPhaseDefaultColour : ammoPhasePassedColour;
                    }
                }
            }
            else
            {
                weaponPhaseParent.SetActive(false);
            }
        }
    }
}
