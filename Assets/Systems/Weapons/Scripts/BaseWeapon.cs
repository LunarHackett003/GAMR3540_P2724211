using UnityEngine;

/// <summary>
/// BaseWeapon provides core functionality that is shared across all weapons. <br></br>
/// This functionality might not be particularly broad, but it provides a common base for all weapon types.
/// </summary>
public abstract class BaseWeapon : LunarScript
{
    internal bool primaryInput, secondaryInput, primaryPressedFirst, secondaryPressedFirst, primaryPressed, secondaryPressed;
    [SerializeField] internal bool attackOnPrimary, attackOnSecondary, primaryBlocksSecondary, secondaryBlocksPrimary;
    [SerializeField] internal bool aimOnSecondary;
    public WeaponController controller;

    public void SetPrimaryInput(bool input) => primaryInput = input;
    public void SetSecondaryInput(bool input) => secondaryInput = input;

    public override void LTimestep()
    {
        base.LTimestep();
        UpdateInputPriority();
        ProcessInput();
    }
    protected void UpdateInputPriority()
    {
        if (primaryBlocksSecondary)
        {
            if (primaryInput && !secondaryInput)
                primaryPressedFirst = true;
            if (!primaryInput)
            {
                primaryPressedFirst = false;
            }
        }
        if (secondaryBlocksPrimary)
        {
            if (secondaryInput && !primaryInput)
            {
                secondaryPressedFirst = true;
            }
            if (!secondaryInput)
            {
                secondaryPressedFirst = false;
            }
        }
    }
    protected abstract void ProcessInput();
    protected abstract void PrimaryBehaviour();
    protected abstract void SecondaryBehaviour();
}
