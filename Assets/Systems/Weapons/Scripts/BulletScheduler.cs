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
    public struct BulletData
    {
        public Vector3 start, dir;
        public float distance;
        public bool moving;
    }

    public static BulletScheduler Instance { get; private set; }

    public List<BulletData> movingBullets = new();
    public List<BulletData> bulletData = new();
  
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
        if(bulletData.Count > 0 )
        {
            movingBullets = bulletData.FindAll(x => x.moving);
            //Can't use jobs themselves, have to use RaycastCommands. Unity refuses to allow Raycasts to be called from other threads.
            //bulletJob = new BulletJob(new NativeArray<BulletData>(movingBullets.ToArray(), Allocator.TempJob), bulletMask);
            //bulletJob.Run(movingBullets.Count);
            //bulletJob.bd.Dispose();
            QueryParameters qp = new()
            {
                layerMask = bulletMask,
                hitTriggers = QueryTriggerInteraction.Ignore,
            };
            commands = new(movingBullets.Count, Allocator.TempJob);
            for (int i = 0; i < movingBullets.Count; i++)
            {
                BulletData bd = movingBullets[i];
                
                commands[i] = new(bd.start, bd.dir, qp, bd.distance);
            }
            hits = new(commands.Length, Allocator.TempJob);
            JobHandle handle = RaycastCommand.ScheduleBatch(commands, hits, 1);
            handle.Complete();
            commands.Dispose();
            hits.Dispose();
            bulletData.Clear();
        }
        else if(movingBullets.Count > 0)
        {
            movingBullets.Clear();
        }
    }

    public static void ScheduleBullet(Vector3 start, Vector3 direction, float distance)
    {
        BulletData bd = new()
        {
            start = start,
            dir = direction,
            distance = distance,
            moving = true
        };
        Instance.bulletData.Add(bd);
    }
}
