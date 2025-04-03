using UnityEngine;

public class DestructionTestGun : LunarScript
{
    public float spread;
    public float fireDelay;
    float currentFireTime;
    public GameObject bulletHit;
    public float bulletHitDestroyTime = 1;
    public override void LTimestep()
    {
        base.LTimestep();

        if(currentFireTime >= 0 && currentFireTime < fireDelay)
        {
            currentFireTime += Time.fixedDeltaTime;
        }
        if(currentFireTime >= fireDelay && InputManager.FireInput)
        {
            Fire();
            currentFireTime = 0;
        }
    }

    public void Fire()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 100))
        {
            if (hit.collider.TryGetComponent(out Damageable d))
            {
                d.ModifyHealth(-10);
            }
            Destroy(Instantiate(bulletHit, hit.point, Quaternion.LookRotation(hit.normal)), bulletHitDestroyTime);
        }
    }
}
