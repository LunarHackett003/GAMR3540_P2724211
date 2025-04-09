using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedWeapon : BaseWeapon
{

    public enum FireMode : int
    {
        single = 1,
        automatic = 2,
        animated = 4
    }

    [Tooltip("the radius of the base spread per unit distance covered by the shot.")]
    public float baseSpreadPerUnit = 0.1f;
    [Tooltip("the radius of the max spread influenced by movement.")]
    public float maxInfluencedSpreadPerUnit = 0.1f;
    [Tooltip("the current influence of the owner's movement.")]
    public float currentMovementInfluence = 0;

    public Vector3 SpreadVector => (((Vector3)Random.insideUnitCircle * (baseSpreadPerUnit + (currentMovementInfluence * maxInfluencedSpreadPerUnit))) + Vector3.forward).normalized;

    public FireMode[] allowedFireModes = new FireMode[] { FireMode.automatic };
    public int fireModeIndex = 0;
    public FireMode CurrentFireMode => allowedFireModes[fireModeIndex];
    public int roundsPerMinute;
    public int roundsInBurst;
    protected int burstRoundsFired;
    public float timeBetweenRounds;
    public float burstCooldown;
    float currentFireCooldown;
    [SerializeField] protected bool fired = false;
    protected bool burstFiring = false;
    protected virtual bool PrimaryBlocked => fired;
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
        if (!secondaryPressedFirst && !PrimaryBlocked)
            PrimaryBehaviour();
        if(!primaryPressedFirst) 
            SecondaryBehaviour();
    }
    protected virtual void FireWeapon()
    {

    }
    protected override void PrimaryBehaviour()
    {
        if (!primaryPressed && primaryInput)
        {
            if (roundsInBurst > 0)
            {
                if (!burstFiring)
                {
                    StartCoroutine(BurstFire());
                }
            }
            else 
            {
                FireWeapon();
                fired = true;
            }

        }

        switch (CurrentFireMode)
        {
            case FireMode.single:
                primaryPressed = primaryInput;
                break;
            case FireMode.automatic:
                break;
            case FireMode.animated:
                if(primaryInput)
                    primaryPressed = true;
                break;
            default:
                break;
        }

    }
    protected override void SecondaryBehaviour()
    {

    }
    protected virtual void OnValidate()
    {
        timeBetweenRounds = 1 / ((float)roundsPerMinute / 60);
    }

    protected virtual IEnumerator BurstFire()
    {
        burstFiring = true;
        while (burstRoundsFired < roundsInBurst)
        {
            burstRoundsFired++;
            FireWeapon();
            yield return new WaitForSeconds(timeBetweenRounds);
        }
        yield return new WaitForSeconds(burstCooldown);
        if (CurrentFireMode == FireMode.single)
        {
            yield return new WaitWhile(() => { return primaryInput == false; });
        }
        burstRoundsFired = 0;
        burstFiring = false;
        yield break;
    }
}
