using UnityEngine;

public class TestHitscanWeapon : RangedWeapon
{
    public bool primaryInputValue;
    public float raysPerShot = 1;

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
            for (int i = 0; i < raysPerShot; i++)
            {
                BulletScheduler.ScheduleBullet(transform.position, transform.TransformDirection(new Vector3(Random.value * 0.1f, Random.value * 0.1f, 1).normalized), 100);
            }
        }
    }
    private void OnValidate()
    {
        if (raysPerShot < 1)
            raysPerShot = 1;
    }
}
