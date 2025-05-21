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
    public static NetPlayerEntity player;
    private void Awake()
    {
        Initialise();
    }
    public void Initialise()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public UIModule healthModule, weaponModule;
    public UIModule crosshairModule, compassModule;

    private void OnGUI()
    {
        GUILayout.Space(100);
        if (Instance)
        {
            GUILayout.Label("Gameplay Canvas Active!");
        }
        else
        {
            GUILayout.Label("Gameplay Canvas Not Working!");
        }
    }

    public override void LPostUpdate()
    {
        if (Instance == null)
            Instance = this;


        base.LPostUpdate();

        if(player == null)
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
