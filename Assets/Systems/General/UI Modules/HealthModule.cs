using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class HealthModule : UIModule
{
    public bool useBar;
    public Slider healthBar;
    public bool useText;
    public TMP_Text healthText;
    public bool useMaterial;
    public bool useFullscreenMaterial;
    public bool materialUsesInverse;
    public Material fullscreenMaterial;
    public FullScreenPassRendererFeature fullscreenPass;
    public string fullscreenMaterialKey;
    int fullscreenMaterialID;

    public float healthInverseLerp = 0;
    public float healthLastValue = -1;

    private void Start()
    {
        fullscreenMaterialID = Shader.PropertyToID(fullscreenMaterialKey);
        healthBar.gameObject.SetActive(useBar);
        healthText.gameObject.SetActive(useText);
        fullscreenPass.SetActive(useMaterial);
        if (useMaterial)
        {
            fullscreenMaterial = new(fullscreenMaterial);
            fullscreenPass.passMaterial = fullscreenMaterial;
        }
    }

    public override void UpdateModule()
    {
        healthInverseLerp = Mathf.InverseLerp(0, GameplayCanvas.playerController.damageable.maxHealth, GameplayCanvas.playerController.damageable.CurrentHealth);

        if (healthLastValue == healthInverseLerp)
            return;

        healthLastValue = healthInverseLerp;
        if (useBar)
        {
            healthBar.value = healthInverseLerp;
        }
        if(useText)
        {
            healthText.text = GameplayCanvas.playerController.damageable.CurrentHealth.ToString("0");
        }
        if (useMaterial)
        {
            fullscreenMaterial.SetFloat(fullscreenMaterialID, materialUsesInverse ? (1 - healthInverseLerp) : healthInverseLerp);
        }
    }
}
