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
        public float distance;
        public bool moving;
    }
    
    public static BulletScheduler Instance { get; private set; }
    public int maxRaycastsPerStep = 350;
    public RaycastData[] raycastData = new RaycastData[0];

    public int raycastsWaiting;
    int raycastsHit;
    long timeNow;

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
        raycastData = new RaycastData[maxRaycastsPerStep];
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
                RaycastData bd = raycastData[i];
                
                commands[i] = new(bd.start, bd.dir, qp, bd.distance);
            }
            hits = new(commands.Length, Allocator.TempJob);
            JobHandle handle = RaycastCommand.ScheduleBatch(commands, hits, 1);
            handle.Complete();

            for (int i = 0; i < hits.Length; i++)
            {
                raycastsHit = i + 1;
                if (hits[i].collider == null)
                {
                    //RaycastCommands return un-written hit results so the data might not even be accessible after this point.
                    //We will break to avoid any potential problems.
                    break;
                }
                //Lets just... ignore that old "don't use null propagation" thingy, yeah? Ain't important. Trust me
                hits[i].collider.attachedRigidbody?.AddForceAtPosition(hits[i].normal, hits[i].point);
                Debug.DrawLine(commands[i].from, hits[i].point, Random.ColorHSV());
            }
            commands.Dispose();
            hits.Dispose();
            //print($"time to complete: {System.DateTime.Now.Millisecond - timeNow}, expected {raycastsWaiting} shots, fired {raycastsHit} times this tick.");
            raycastsWaiting = 0;
        }

    }

    public static void ScheduleBullet(Vector3 start, Vector3 direction, float distance)
    {
        if (Instance.raycastsWaiting < Instance.maxRaycastsPerStep)
        {
            RaycastData bd = new()
            {
                start = start,
                dir = direction,
                distance = distance,
                moving = true
            };
            Instance.raycastData[Instance.raycastsWaiting] = bd;
            Instance.raycastsWaiting++;
        }
    }
}
