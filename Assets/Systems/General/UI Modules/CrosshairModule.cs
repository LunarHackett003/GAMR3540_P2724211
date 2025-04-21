using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairModule : UIModule
{
    public CanvasGroup crosshairGroup;
    public float currentCrosshairSize = 1f;
    public float crosshairLerpSpeed = 3f;
    public float baseCrosshairSize = 100f;

    public RectTransform crosshairRect;
    public Image weaponChargeImage;
    public float weaponChargeFillAmount;

    public BaseWeapon Weapon => GameplayCanvas.playerController.currentWeapon;

    public override void UpdateModule()
    {
        if (Weapon == null)
            return;
        crosshairGroup.alpha = 1 - Weapon.controller.aimAmount;

        currentCrosshairSize = Mathf.Lerp(currentCrosshairSize, Weapon.crosshairSpreadBase + (Weapon.attackSpreadAmount * Weapon.crosshairSpreadMax), Time.deltaTime * crosshairLerpSpeed);
        crosshairRect.sizeDelta = new Vector2(currentCrosshairSize, currentCrosshairSize) * baseCrosshairSize;
    }
}
