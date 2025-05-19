using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestLoadoutButton : MonoBehaviour
{
    public BaseNetWeapon weapon;
    public Image iconDisplay;
    public TMP_Text nameDisplay;
    public void Initialise(BaseNetWeapon weapon)
    {
        this.weapon = weapon;
        iconDisplay.sprite = weapon.weaponIcon;
        nameDisplay.text = weapon.displayName;
    }
}
