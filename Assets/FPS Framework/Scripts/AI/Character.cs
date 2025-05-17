using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : Damageable
{
    [Tooltip("The team of this character.")]
    public int teamIndex = 0;

    [Tooltip("The rigidbody used by this character")] public Rigidbody rb;


}
