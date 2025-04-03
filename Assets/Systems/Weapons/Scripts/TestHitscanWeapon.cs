using UnityEngine;

public class TestHitscanWeapon : RangedWeapon
{
    public bool primaryInputValue;

    protected override void ProcessInput()
    {
        primaryInput = primaryInputValue;
        base.ProcessInput();
    }

    protected override void PrimaryBehaviour()
    {
        base.PrimaryBehaviour();

        if(BulletScheduler.Instance != null )
        {
            BulletScheduler.ScheduleBullet(transform.position, transform.TransformDirection(new Vector3(Random.value * 0.1f, Random.value * 0.1f, 1).normalized), 100);
        }
    }
}
