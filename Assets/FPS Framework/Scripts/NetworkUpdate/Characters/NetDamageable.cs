using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetDamageable : LunarNetScript
{
    public AnticipatedNetworkVariable<float> currentHealth = new(100, StaleDataHandling.Reanticipate);

    [SerializeField] internal int maxHealth;
    public int IntHealth => Mathf.RoundToInt(currentHealth.Value);
}
