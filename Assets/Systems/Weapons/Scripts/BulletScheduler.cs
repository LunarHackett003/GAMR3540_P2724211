using System.Buffers;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class BulletScheduler : LunarScript
{
    public LayerMask bulletMask;
    public NativeArray<RaycastHit> hits;
    public NativeArray<RaycastCommand> commands;
    

    [System.Serializable]
    public struct RaycastData
    {
        public Vector3 start, dir;
        public float distance, baseDistance;
        public HitscanWeapon owner;
        public HitscanTracer tracer;
    }
    public struct ColliderHitData
    {
        public HitscanWeapon weapon;
        public float damageAccumulated;
        public int hits;
        public Vector3 forceAccumulated;
        public Vector3 hitPointAccumulated;
    }
    
    public static BulletScheduler Instance { get; private set; }
    public int maxRaycastsPerStep = 350;
    public RaycastData[,] raycastData = new RaycastData[2, 0];
    public float raycastDebugDisplayTime = 0.1f;
    Dictionary<Collider, ColliderHitData> collidersHitByWeapon;
    int raycastFlipFlop;
    public int raycastsWaiting;
    int raycastsHit;
    long timeNow;

#if UNITY_EDITOR
    public RaycastData[] rcArrayOne, rcArrayTwo;
    public bool buildRaycastArrays;
#endif

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        raycastData = new RaycastData[2, maxRaycastsPerStep];
        collidersHitByWeapon = new();
    }
    private void OnDestroy()
    {
        //Dispose of native arrays
        if (hits.IsCreated)
            hits.Dispose();
        if (commands.IsCreated)
            commands.Dispose();
    }
    public override void LTimestep()
    {
        base.LTimestep();
        if(raycastsWaiting > 0)
        {
            ProcessBullets();
        }

    }

    void ProcessBullets()
    {
        timeNow = System.DateTime.Now.Millisecond;
        //Can't use jobs themselves, have to use RaycastCommands. Unity refuses to allow Raycasts to be called from other threads.
        //bulletJob = new BulletJob(new NativeArray<BulletData>(movingBullets.ToArray(), Allocator.TempJob), bulletMask);
        //bulletJob.Run(movingBullets.Count);
        //bulletJob.bd.Dispose();
        QueryParameters qp = new()
        {
            layerMask = bulletMask,
            hitTriggers = QueryTriggerInteraction.Ignore,
        };
        commands = new(raycastsWaiting, Allocator.TempJob);
        for (int i = 0; i < raycastsWaiting; i++)
        {
            RaycastData bd = raycastData[raycastFlipFlop, i];

            commands[i] = new(bd.start, bd.dir, qp, bd.distance);
        }
        hits = new(commands.Length, Allocator.TempJob);
        JobHandle handle = RaycastCommand.ScheduleBatch(commands, hits, 1);
        handle.Complete();
        /* If any shots require more simulations, they will take place in the next timestep.
        *I have also now added a raycast "flip flop" between two parts of a 2d array.
        *This means we can store two arrays of raycast data, allowing for easy simulation of richochets or penetrations on the next step
        *The additional step ALSO enforces a kind of 
        */
        raycastsWaiting = 0;
        int flipflop = raycastFlipFlop;
        int checkedAndClear = 0;
        raycastFlipFlop = (raycastFlipFlop + 1) % 2;
        for (int i = 0; i < hits.Length; i++)
        {
            //raycastsHit = i + 1;
            if (hits[i].collider == null)
            {
                if (raycastData[flipflop, i].tracer != null)
                {
                    raycastData[flipflop, i].tracer.AddNextPoint(commands[i].from + (commands[i].direction * commands[i].distance));
                }
                else
                {
                    checkedAndClear++;
                }

                if(checkedAndClear >= 2)
                {
                    //If CheckedAndClear is 2 or more, then we've probably reached the end of all of the raycasts with tracers. We can safely break here, I hope.
                    break;
                }
                //RaycastCommands return un-written hit results so the data might not even be accessible after this point.
                //We will continue to the next index to avoid any potential problems, while still checking if any tracers exist.
                continue;
            }
            Collider c = hits[i].collider;
            RaycastData data = raycastData[flipflop, i];
            RaycastHit hit = hits[i];
            if(data.owner== null)
            {
                Debug.LogWarning("Tried to fire with a null owner!");
                continue;
            }
            float damageDealt = data.owner.GetDamageDealt(hit.distance + data.baseDistance);
            //Lets just... ignore that old "don't use null propagation" thingy, yeah? Ain't important. Trust me
            if (collidersHitByWeapon.TryGetValue(c, out ColliderHitData chd))
            {
                chd.damageAccumulated += damageDealt;
                chd.forceAccumulated += -hit.normal * damageDealt;
                chd.hitPointAccumulated += hit.point;
                chd.hits++;
                collidersHitByWeapon[c] = chd;
            }
            else
            {
                collidersHitByWeapon.TryAdd(c, new() 
                {
                    weapon = data.owner, 
                    damageAccumulated = damageDealt,
                    forceAccumulated = - hit.normal * damageDealt,
                    hitPointAccumulated = hit.point,
                    hits = 1
                });
            }
            if(data.owner.tracerPrefab != null)
            {
                if(data.tracer == null)
                {
                    data.owner.SendTracer(commands[i].from, hit.point, out HitscanTracer t);
                    data.tracer = t;
                }
                else
                {
                    data.tracer.AddNextPoint(hit.point);
                }
            }
            bool bulletAffected = false;
            {
                if (data.owner.shotsCanRichochet)
                {
                    if (data.owner.ShouldRicochet(hit, commands[i].direction))
                    {
                        bulletAffected = true;
                        float distanceLeft = (data.owner.shotMaxRange - (hit.distance + data.baseDistance)) * data.owner.shotRicochetRangeLoss;
                        if(distanceLeft > 0)
                        {
                            ScheduleBullet(hit.point, Vector3.Reflect(commands[i].direction, hit.normal), distanceLeft, data.baseDistance + hit.distance, data.owner, data.tracer);
                        }
                    }
                }
                if (data.owner.shotsCanPenetrate && !bulletAffected)
                {
                    if(data.owner.ShouldPenetrate(hit, commands[i].direction))
                    {
                        data.baseDistance += hit.distance;
                        float distanceLeft = (data.owner.shotMaxRange - (hit.distance + data.baseDistance)) * data.owner.shotPenetrateRangeLoss;
                        if( distanceLeft > 0)
                        {
                            ScheduleBullet(hit.point, commands[i].direction, distanceLeft, data.baseDistance + hit.distance, data.owner, data.tracer);
                        }
                    }
                }
            }
            Debug.DrawLine(commands[i].from, hits[i].point, Random.ColorHSV(), raycastDebugDisplayTime);
        }
        if (collidersHitByWeapon.Count > 0)
        {
            foreach (var item in collidersHitByWeapon)
            {
                if(item.Key.attachedRigidbody != null)
                {
                    item.Key.attachedRigidbody.AddForceAtPosition(item.Value.forceAccumulated * item.Value.weapon.knockbackForceMultiplier, item.Value.hitPointAccumulated / item.Value.hits);
                }
                if(item.Key.TryGetComponent(out Damageable d))
                {
                    d.ModifyHealth(item.Value.damageAccumulated);
                }
            }
        }

        collidersHitByWeapon.Clear();
        commands.Dispose();
        hits.Dispose();
        print($"time to complete: {System.DateTime.Now.Millisecond - timeNow}");
    }

    public static void ScheduleBullet(Vector3 start, Vector3 direction, float distance, float baseDistance, HitscanWeapon owner, HitscanTracer tracer = null)
    {
        if (Instance.raycastsWaiting < Instance.maxRaycastsPerStep)
        {
            RaycastData bd = new()
            {
                start = start,
                dir = direction,
                distance = distance,
                owner = owner,
                baseDistance = baseDistance,
                tracer = tracer
            };
            Instance.raycastData[Instance.raycastFlipFlop, Instance.raycastsWaiting] = bd;
            Instance.raycastsWaiting++;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (buildRaycastArrays)
        {
            rcArrayOne = new RaycastData[maxRaycastsPerStep];
            rcArrayTwo = new RaycastData[maxRaycastsPerStep];

            for (int i = 0; i < maxRaycastsPerStep; i++)
            {
                rcArrayOne[i] = raycastData[0, i];
                rcArrayTwo[i] = raycastData[1, i];
            }
        }
    }
#endif
}
