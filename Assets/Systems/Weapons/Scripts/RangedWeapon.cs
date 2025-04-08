using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : BaseWeapon
{
    [Tooltip("the radius of the base spread per unit distance covered by the shot.")]
    public float baseSpreadPerUnit = 0.1f;
    [Tooltip("the radius of the max spread influenced by movement.")]
    public float maxInfluencedSpreadPerUnit = 0.1f;
    [Tooltip("the current influence of the owner's movement.")]
    public float currentMovementInfluence = 0;

    public Vector3 SpreadVector => (((Vector3)Random.insideUnitCircle * (baseSpreadPerUnit + (currentMovementInfluence * maxInfluencedSpreadPerUnit))) + Vector3.forward).normalized;

    public int roundsPerMinute;
    public float timeBetweenRounds;
    float currentFireCooldown;
    protected bool fired = false;
    protected virtual bool FireBlocked => fired;
    public override void LTimestep()
    {
        base.LTimestep();
        if (fired)
        {
            currentFireCooldown += Time.fixedDeltaTime;
        }
        if(currentFireCooldown >= timeBetweenRounds)
        {
            fired = false;
            currentFireCooldown = 0;
        }

    }

    protected override void ProcessInput()
    {
        if (primaryInput && !secondaryPressedFirst && !FireBlocked)
            PrimaryBehaviour();
        if(secondaryInput && !primaryPressedFirst) 
            SecondaryBehaviour();
    }
    protected virtual void FireWeapon()
    {

    }
    protected override void PrimaryBehaviour()
    {
        fired = true;
    }
    protected override void SecondaryBehaviour()
    {

    }
    protected virtual void OnValidate()
    {
        timeBetweenRounds = 1 / ((float)roundsPerMinute / 60);
    }
}
