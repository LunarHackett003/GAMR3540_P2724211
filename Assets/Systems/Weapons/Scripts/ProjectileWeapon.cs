using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectileWeapon : RangedWeapon
{
    ObjectPool<Projectile> projectilePool;

    public int poolStartCapacity = 50, poolMaxCapacity = 500;


    public ObjectPool<Projectile> ProjectilePool
    {
        get
        {
            projectilePool ??= new ObjectPool<Projectile>(CreatePooledItem, TakeFromPool, ReturnToPool, DestroyPoolObject, true, poolStartCapacity, poolMaxCapacity);
            return projectilePool;
        }
    }

    Projectile CreatePooledItem()
    {
        return null;
    }
    void ReturnToPool(Projectile projectile)
    {

    }
    void TakeFromPool(Projectile projectile)
    {

    }
    void DestroyPoolObject(Projectile projectile)
    {

    }

    private void Start()
    {
        
    }






}
