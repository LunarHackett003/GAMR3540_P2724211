using UnityEngine;

public class RangedWeapon : BaseWeapon
{

    protected override void ProcessInput()
    {
        if (primaryInput && !secondaryPressedFirst)
            PrimaryBehaviour();
        if(secondaryInput && !primaryPressedFirst) 
            SecondaryBehaviour();
    }
    protected virtual void FireWeapon()
    {

    }
    protected override void PrimaryBehaviour()
    {

    }
    protected override void SecondaryBehaviour()
    {

    }
}
