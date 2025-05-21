using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEngine;

public class ProjectileSimulator : LunarNetScript
{

    public static HashSet<NetProjectile> projectilesToTerminate = new();
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
        CheckAndTerminateProjectiles();
    }

    void SimulateProjectiles()
    {
        QueryParameters qp = new()
        {
            layerMask = layermask,
            hitTriggers = QueryTriggerInteraction.Collide,
            hitMultipleFaces = true
        };
        castCommands = new(allProjectiles.Count, Allocator.TempJob);
        for (int i = 0; i < castCommands.Length; i++)
        {
            NetProjectile proj = allProjectiles[i];
            castCommands[i] = new(proj.transform.position, proj.thickness, proj.direction, qp, proj.velocity * Time.fixedDeltaTime);
        }

        //Multiply by 16 to allow us to hit UP TO 16 targets.
        hits = new NativeArray<RaycastHit>(allProjectiles.Count * 16, Allocator.TempJob);
        JobHandle job = SpherecastCommand.ScheduleBatch(castCommands, hits, 1, 16);
        job.Complete();

        int rayCount = 0;
        float distance = 0;
        int indexOfClosestCollider = 0;
        if(hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i += 16)
            {
                SpherecastCommand command = castCommands[rayCount];
                Debug.DrawRay(command.origin, 5 * command.distance * command.direction, Color.red, raycastDebugTime);
                distance = float.MaxValue;
                RaycastHit hit = hits[i];
                NetProjectile proj = allProjectiles[rayCount];
                rayCount++;
                for (int j = 0; j < 16; j++)
                {
                    if (hits[i + j].collider == null || !proj.ignoredColliders.Contains(hits[i + j].collider))
                        continue;
                    float compareDistance = Vector3.Distance(command.origin, hit.point);
                    if (compareDistance <= distance)
                    {
                        indexOfClosestCollider = i + j;
                        distance = compareDistance;
                    }
                }

                //Now that we've found the closest collider, we can replace the hit used to query the above bit
                hit = hits[indexOfClosestCollider];
                //Cache the collider, and now everything SHOULD function as it did before, right?
                Collider c = hit.collider;
                if (c == null)
                    continue;
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

                Debug.DrawRay(proj.transform.position, hits[i].point, Color.green, raycastDebugTime);

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
        }
        castCommands.Dispose();
        hits.Dispose();
        colliderHitData.Clear();
        CheckAndTerminateProjectiles();
    }
    void CheckAndTerminateProjectiles()
    {
        if (projectilesToTerminate.Count > 0)
        {
            foreach (var item in projectilesToTerminate)
            {
                item.TerminateProjectile(true);
                allProjectiles.Remove(item);
            }
            projectilesToTerminate.Clear();
        }
    }

}
