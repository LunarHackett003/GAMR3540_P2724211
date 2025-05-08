using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectileWeapon : RangedWeapon
{
    ObjectPool<BaseProjectile> projectilePool;

    public int poolStartCapacity = 50, poolMaxCapacity = 500;


    public ObjectPool<BaseProjectile> ProjectilePool
    {
        get
        {
            InitialiseProjectilePool();
            return projectilePool;
        }
    }

    void InitialiseProjectilePool()
    {
        projectilePool = new ObjectPool<BaseProjectile>(CreatePooledItem, TakeFromPool, ReturnToPool, DestroyPoolObject, true, poolStartCapacity, poolMaxCapacity);
    }

    BaseProjectile CreatePooledItem()
    {
        BaseProjectile projectile = Instantiate(projectilePrefab, fireOrigin.position, Quaternion.identity).GetComponent<BaseProjectile>();
        projectile.InitialiseFromPool();
        projectile.InitialiseFromWeapon(this);
        projectile.transform.forward = fireOrigin.forward;
        return projectile;
    }
    void ReturnToPool(BaseProjectile projectile)
    {
        projectile.gameObject.SetActive(false);
        projectile.ReturnToPool();
        projectile.hideFlags = HideFlags.HideInHierarchy;
    }
    void TakeFromPool(BaseProjectile projectile)
    {
        projectile.gameObject.SetActive(true);
        projectile.hideFlags = HideFlags.None;
    }
    void DestroyPoolObject(BaseProjectile projectile)
    {
        if (projectile != null)
            Destroy(projectile.gameObject);
    }

    [SerializeField] internal GameObject projectilePrefab;



    protected override void Start()
    {
        base.Start();
        InitialiseProjectilePool();
    }

    protected override void FireWeapon(bool primary = true)
    {
        CreateProjectile(primary);
        base.FireWeapon(primary);
    }

    protected virtual void CreateProjectile(bool primary = true)
    {
        BaseProjectile[] createdProjectiles = new BaseProjectile[fireIterations];
        for (int i = 0; i < fireIterations; i++)
        {
            BaseProjectile bp;
            bp = ProjectilePool.Get();
            bp.transform.forward = controller.fireOrigin.TransformDirection(SpreadVector);
            bp.transform.position = controller.fireOrigin.position;
            bp.transform.position += bp.transform.forward * 0.1f;
            createdProjectiles[i] = bp;
            if ((primary && primaryUsesCharge) || secondaryUsesCharge)
            {
                bp.damageMultiplier = chargeAmount;
            }
            bp.InitialiseFromPool();
            for (int x = 0; x < controller.colliders.Length; x++)
            {
                for (int z = 0; z < bp.colliders.Length; z++)
                {
                    Physics.IgnoreCollision(controller.colliders[x], bp.colliders[z]);
                }
                bp.colliders[i].enabled = true;
            }
        }
    }





}
