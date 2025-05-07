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
            projectilePool ??= new ObjectPool<BaseProjectile>(CreatePooledItem, TakeFromPool, ReturnToPool, DestroyPoolObject, true, poolStartCapacity, poolMaxCapacity);
            return projectilePool;
        }
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
        projectile.InitialiseFromPool();
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
    }

    protected override void FireWeapon(bool primary = true)
    {
        CreateProjectile();
        base.FireWeapon(primary);
    }

    protected virtual void CreateProjectile()
    {
        BaseProjectile[] createdProjectiles = new BaseProjectile[fireIterations];
        for (int i = 0; i < fireIterations; i++)
        {
            createdProjectiles[i] = ProjectilePool.Get();
        }
    }





}
