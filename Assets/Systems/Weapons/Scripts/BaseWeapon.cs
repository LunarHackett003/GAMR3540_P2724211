using UnityEngine;

/// <summary>
/// BaseWeapon provides core functionality that is shared across all weapons. <br></br>
/// This functionality might not be particularly broad, but it provides a common base for all weapon types.
/// </summary>
public abstract class BaseWeapon : LunarScript
{
    protected bool primaryInput, secondaryInput, primaryPressedFirst, secondaryPressedFirst;
    [SerializeField] protected bool attackOnPrimary, attackOnSecondary, primaryBlocksSecondary, secondaryBlocksPrimary;
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
