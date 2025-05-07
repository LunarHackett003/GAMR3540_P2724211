using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine;

public class HitscanWeapon : RangedWeapon
{
    ObjectPool<HitscanTracer> tracerPool;
    public int poolStartCapacity = 50, poolMaxCapacity = 500;

    public ObjectPool<HitscanTracer> TracerPool
    {
        get
        {
            tracerPool ??= new ObjectPool<HitscanTracer>(CreatePooledItem, TakeFromPool, ReturnToPool, DestroyPoolObject, true, poolStartCapacity, poolMaxCapacity);
            return tracerPool;
        }
    }

    HitscanTracer CreatePooledItem()
    {
        HitscanTracer ht = Instantiate(tracerPrefab, fireOrigin.position, Quaternion.identity, null).gameObject.GetComponent<HitscanTracer>();
        ht.gameObject.hideFlags = HideFlags.HideInHierarchy;
        ht.trail.emitting = false;
        return ht;
    }
    void ReturnToPool(HitscanTracer trace)
    {
        trace.trail.emitting = false;
        trace.gameObject.hideFlags = HideFlags.HideInHierarchy;
        trace.gameObject.SetActive(false);
    }
    void TakeFromPool(HitscanTracer trace)
    {
        trace.trail.Clear();
        trace.trail.emitting = true;
        trace.gameObject.SetActive(true);
        trace.gameObject.hideFlags = HideFlags.None;
    }
    void DestroyPoolObject(HitscanTracer trace)
    {
        if(trace != null)
            Destroy(trace.gameObject);
    }

    [Tooltip("Can bullets bounce off of surfaces?")]
    public bool shotsCanRichochet = false;
    [Tooltip("Can bullets penetrate surfaces?")]
    public bool shotsCanPenetrate = false;
    [Tooltip("The maximum range of the ray")]
    public float shotMaxRange = 100;
    [Tooltip("What portion (0-1) of the remaining range is lost when ricocheting?")]
    public float shotRicochetRangeLoss = 0.5f;
    [Tooltip("What portion (0-1) of the remaining range is lost when penetrating?")]
    public float shotPenetrateRangeLoss = 0.2f;
    [Tooltip("The maximum surface normal a shot can ricochet from")]
    public float maxRicochetNormal = 0.2f;
    [Tooltip("The minimum surface normal a shot can ricochet from")]
    public float minRicochetNormal = 0.2f;
    [Tooltip("The maximum surface normal a shot can penetrate through")]
    public float minPenetrateNormal = 0.5f;
    [Tooltip("How much damage is dealt at 0 range")]
    public float maxDamage = 30;
    [Tooltip("The multiplier to knockback force")]
    public float knockbackForceMultiplier = 0.1f;
    public AnimationCurve damageFalloff = AnimationCurve.Linear(0, 1, 1, 0);
    public HitscanTracer tracerPrefab;
    public float tracerSpeed;
    protected override void Start()
    {
        base.Start();
        Debug.Log(TracerPool.CountAll);
    }
    public bool ShouldRicochet(RaycastHit hit, Vector3 direction)
    {
        float dot = Vector3.Dot(hit.normal, direction);
        Debug.DrawRay(hit.point, hit.normal, Color.yellow, BulletScheduler.Instance.raycastDebugDisplayTime);
        return dot < maxRicochetNormal && dot > minRicochetNormal;
    }
    public bool ShouldPenetrate(RaycastHit hit, Vector3 direction)
    {
        return Vector3.Dot(hit.normal, -direction) > minPenetrateNormal;
    }
    public float GetDamageDealt(float range)
    {
        return damageFalloff.Evaluate(Mathf.InverseLerp(0, shotMaxRange, range)) * maxDamage;
    }
    protected virtual void FireHitscan()
    {
        Transform t = controller == null ? transform : controller.fireOrigin;
        if (BulletScheduler.Instance != null)
        {
            for (int i = 0; i < fireIterations; i++)
            {
                if (baseSpreadPerUnit > 0 || maxInfluencedSpreadPerUnit > 0)
                {
                    BulletScheduler.ScheduleBullet(fireOrigin.position, t.TransformDirection(SpreadVector), shotMaxRange, 0, this);
                }
                else
                {
                    BulletScheduler.ScheduleBullet(fireOrigin.position, t.TransformDirection(Vector3.forward), shotMaxRange, 0, this);
                }
            }
        }
    }
    protected override void FireWeapon(bool primary = true)
    {
        FireHitscan();
        base.FireWeapon(primary);
    }
    public void SendTracer(Vector3 start, Vector3 end, out HitscanTracer t)
    {
        tracerPool.Get(out t);
        if(t != null)
        {
            t.SendTracer(start, end, tracerSpeed);
            t.owner = this;
        }
    }
    public void AddEndToTracer(HitscanTracer t, Vector3 end, bool hardStop)
    {
        t.AddNextPoint(end, hardStop);
    }
    protected override void OnValidate()
    {
        base.OnValidate();
        if (fireIterations < 1)
            fireIterations = 1;
    }
    private void OnDestroy()
    {
        TracerPool?.Dispose();
    }
}
