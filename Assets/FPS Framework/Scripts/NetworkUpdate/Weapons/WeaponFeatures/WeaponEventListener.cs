using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class WeaponEventListener : MonoBehaviour
{
    public BaseNetWeapon weapon;

    public bool applyRecoil;
    public bool playFireSound;
    public bool playFireEffects;
    public VisualEffect fireEffect;
    public ParticleSystem fireParticle;

    public bool playReloadSound;
    private void OnEnable()
    {
        if(playFireEffects || playFireSound || applyRecoil)
            weapon.onWeaponFired += WeaponFired;
        if (playReloadSound)
            weapon.onReloadEvent += ReloadEvent;
    }
    private void OnDisable()
    {
        if (playFireEffects || playFireSound || applyRecoil)
            weapon.onWeaponFired -= WeaponFired;
    }

    public void WeaponFired(float charge)
    {
        weapon.controller.ReceivePostAttack();
        if (applyRecoil)
        {
            weapon.controller.ReceiveRecoil(charge, out _);
        }
        if (playFireSound)
        {

        }
        if (playFireEffects)
        {
            if (fireEffect != null)
                fireEffect.Play();
            if(fireParticle != null)
                fireParticle.Play();
        }
    }
    public void ReloadEvent(bool emptyReload, bool canceled)
    {

    }
}
