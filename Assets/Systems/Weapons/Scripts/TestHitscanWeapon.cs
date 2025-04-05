using UnityEngine;

public class TestHitscanWeapon : RangedWeapon
{
    public bool primaryInputValue;
    public int raysPerShot = 1;
    public float shotDivergence = 0.1f;
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
                if(shotDivergence > 0)
                {
                    Vector2 randCirc = Random.insideUnitCircle * shotDivergence;
                    BulletScheduler.ScheduleBullet(transform.position, transform.TransformDirection((Vector3)randCirc + Vector3.forward).normalized, 100);
                }
                else
                {
                    BulletScheduler.ScheduleBullet(transform.position, transform.TransformDirection(Vector3.forward), 100);
                }
            }
        }
    }
    private void OnValidate()
    {
        if (raysPerShot < 1)
            raysPerShot = 1;
    }
}
