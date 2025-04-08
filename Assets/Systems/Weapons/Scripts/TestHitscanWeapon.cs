using UnityEngine;

[SelectionBase]
public class TestHitscanWeapon : HitscanWeapon
{
    public bool primaryInputValue;
    public bool trySingleShot;

    protected override void ProcessInput()
    {
        primaryInput = primaryInputValue;
        base.ProcessInput();
        if(trySingleShot)
            primaryInputValue = false;
    }

    protected override void PrimaryBehaviour()
    {
        base.PrimaryBehaviour();

        FireHitscan();

    }


}
