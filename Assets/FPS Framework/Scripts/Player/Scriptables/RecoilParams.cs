using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Recoil Params")]
public class RecoilParams : ScriptableObject
{
    public AnimationCurve recoilCurve = AnimationCurve.Linear(0, 1, 1, 0);
    public float maxRecoilShots = 10;
    public float recoilShotClearTime = 1;
    public Vector2 recoilDecay;
    public Vector2 baseRecoilIncrement;
    public float recoilSnappiness = 5;
    public float recoilSpeed = 5;
}
