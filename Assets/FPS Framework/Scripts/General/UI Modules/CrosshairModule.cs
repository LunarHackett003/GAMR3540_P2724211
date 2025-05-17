using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairModule : UIModule
{
    public CanvasGroup crosshairGroup;
    public float currentCrosshairSize = 1f;
    public float crosshairTargetSize = 0;
    public float crosshairLerpSpeed = 3f;
    public float baseCrosshairSize = 100f;

    public RectTransform crosshairRect;
    public Image weaponChargeImage;
    public float weaponChargeFillAmount;

    public BaseWeapon Weapon => GameplayCanvas.playerController.currentWeapon;

    public override void UpdateModule()
    {
        if (Weapon == null || Weapon.controller == null)
            return;
        crosshairGroup.alpha = 1 - Weapon.controller.aimAmount;

        crosshairTargetSize = 1 + Weapon.controller.Spread(Weapon.baseAttackSpread + Weapon.attackSpreadAmount);

        currentCrosshairSize = Mathf.Lerp(currentCrosshairSize, Weapon.crosshairSpreadBase * (crosshairTargetSize * Weapon.crosshairSpreadMax), Time.deltaTime * crosshairLerpSpeed);
        crosshairRect.sizeDelta = new Vector2(currentCrosshairSize, currentCrosshairSize) * baseCrosshairSize;
    }
}
