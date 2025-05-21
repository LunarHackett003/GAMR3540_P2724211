using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;

public class ProjectileSimulator : LunarNetScript
{

    public HashSet<NetProjectile> projectilesToTerminate = new();
    public static List<NetProjectile> allProjectiles;
    public struct HitData
    {
        public RangedNetWeapon weapon;
        public float damageAccumulated;
        public int hits;
        public Vector3 forceAccumulated;
        public Vector3 hitPointAccumulated;
    }
    public Dictionary<Collider, HitData> colliderHitData;
    public float raycastDebugTime = 0.1f;

    public LayerMask layermask;

    NativeArray<SpherecastCommand> castCommands;
    NativeArray<RaycastHit> hits;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            colliderHitData = new();
            allProjectiles = new();
        }
    }

    public override void LTimestep()
    {
        base.LTimestep();

        if (!IsServer)
            return;

        if (allProjectiles.Count == 0)
            return;
        SimulateProjectiles();

        for (int i = allProjectiles.Count - 1; i >= 0; i--)
        {
            NetProjectile projectile = allProjectiles[i];
            if (projectile.terminated) continue;

            projectile.TickProjectile();
        }
    }

    void SimulateProjectiles()
    {
        QueryParameters qp = new()
        {
            layerMask = layermask,
            hitTriggers = QueryTriggerInteraction.Collide,
        };
        castCommands = new(allProjectiles.Count, Allocator.TempJob);
        for (int i = 0; i < castCommands.Length; i++)
        {
            NetProjectile proj = allProjectiles[i];
            castCommands[i] = new(proj.transform.position, proj.thickness, proj.direction, qp, proj.velocity * Time.fixedDeltaTime);
        }
        hits = new NativeArray<RaycastHit>(allProjectiles.Count, Allocator.TempJob);
        JobHandle job = SpherecastCommand.ScheduleBatch(castCommands, hits, 1);
        job.Complete();

        int hitCount = 0;
        if(hits.Length > 0)
        {

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider == null)
            {
                continue;
            }

            Collider c = hits[i].collider;
            RaycastHit hit = hits[i];
            NetProjectile proj = allProjectiles[i];
            if (proj.ignoredColliders.Contains(c))
            {
                Debug.Log("Ignoring this collider and continuing");
                continue;
            }
            //If we progress past here, we hit something.
            float damageDealt = proj.weapon.GetDamage(proj.distanceTravelled + hit.distance);
            if(colliderHitData.TryGetValue(c, out HitData chd))
            {
                chd.damageAccumulated += damageDealt;
                chd.forceAccumulated += -hit.normal * damageDealt;
                chd.hitPointAccumulated += hit.point;
                chd.hits++;
                colliderHitData[c] = chd;
            }
            else
            {
                colliderHitData.TryAdd(c, new()
                {
                    weapon = proj.weapon,
                    damageAccumulated = damageDealt,
                    forceAccumulated = -hit.normal * damageDealt,
                    hitPointAccumulated = hit.point,
                    hits = 1,
                });
            }
            //Implement Ricochet and Penetration later on


            //Ricochet + Penetration
            proj.transform.position = hit.point;

            projectilesToTerminate.Add(proj);

            Debug.DrawRay(proj.transform.position, hits[i].point, Random.ColorHSV(), raycastDebugTime);


        }
        if(colliderHitData.Count > 0)
        {
            foreach (var item in colliderHitData)
            {
                if(item.Key.attachedRigidbody != null)
                {
                    item.Key.attachedRigidbody.AddForceAtPosition(item.Value.forceAccumulated, item.Value.hitPointAccumulated / item.Value.hits);
                }
                if (item.Key.TryGetComponent(out NetDamageable d))
                {
                    d.ModifyHealth(item.Value.damageAccumulated, item.Value.weapon, DamageSourceType.weapon, false);
                }
            }
        }
            castCommands.Dispose();
            hits.Dispose();
            if(projectilesToTerminate.Count > 0)
            {
                foreach (var item in projectilesToTerminate)
                {
                    allProjectiles.Remove(item);
                }
                projectilesToTerminate.Clear();
            }
        }
    }

}
