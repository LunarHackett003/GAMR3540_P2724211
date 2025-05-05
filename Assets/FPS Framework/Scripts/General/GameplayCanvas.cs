using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// The GameplayCanvas is a script that controls the parts of the UI that the player can see when they're not in a menu.<br></br>
/// This is everything from ammo counters and weapon names, to crosshairs and leaderboards.
/// </summary>
public class GameplayCanvas : LunarScript
{
    public static GameplayCanvas Instance { get; private set; }
    public static PlayerController playerController;
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            enabled = false;
            return;
        }
    }

    public UIModule healthModule, weaponModule;
    public UIModule crosshairModule, compassModule;

    public override void LPostUpdate()
    {
        base.LPostUpdate();

        if(playerController == null)
        {
            return;
        }

        UpdateModule(healthModule);
        UpdateModule(weaponModule);
        UpdateModule(crosshairModule);
        UpdateModule(compassModule);
    }

    public void UpdateModule(UIModule module)
    {
        if(module != null)
            module.UpdateModule();
    }
}
