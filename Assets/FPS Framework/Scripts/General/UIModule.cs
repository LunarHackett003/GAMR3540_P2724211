using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UIModule : MonoBehaviour
{

    public PlayerController Player => GameplayCanvas.playerController;

    public abstract void UpdateModule();
}
