using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TurretWeaponController : WeaponController
{
    public Transform yawTransform, pitchTransform, pointerTransform;
    public Vector3 targetAngle;
    public float targetRotateSpeed;

    public float freeRotateSpeed;
    public float freeRotateWaitTime = .5f;
    public float freeRotateRange = 60f;
    bool freeRotateFlipFlop;
    float freeRotateTime;
    Vector3 currentAngle;

    public float viewRange;
    public float fieldOfView = 60;
    public int viewResolution = 10;
    public float colliderSizeMultiplier = 0.75f;
    [SerializeField] protected float dotFromFOV;

    public LayerMask viewLayerMask;
    public LayerMask obstructionLayerMask;
    public int viewMaxTargets = 10;
    public float viewPollInterval;
    [SerializeField] float viewPollTime;

    Collider[] viewSphereColliders = new Collider[0];

    NativeArray<RaycastCommand> boundsCheckCommands;
    NativeArray<RaycastHit> boundsCheckHits;
    HashSet<Rigidbody> bodiesInRange = new();
    Collider targetedCollider;
    [SerializeField] Rigidbody currentRigidbody;
    float lastTargetedYaw;

    protected override void Start()
    {
        base.Start();
        dotFromFOV = ((fieldOfView / 90) - 1) * -1;
    }

    public override void LTimestep()
    {
        base.LTimestep();
        if(viewPollTime > viewPollInterval)
        {
            viewPollTime = 0;
            if(viewRange > 0)
            {
                if (currentRigidbody != null)
                {
                    //Debug.Log("keeping existing target");
                    KeepOldTarget();
                    if (currentRigidbody == null)
                    {
                        AcquireNewTarget();
                    }
                }
                else
                {
                    AcquireNewTarget();
                }

                if (currentRigidbody)
                {
                    //Now we need to check if we're pointing at them sufficiently
                    Vector3 dir = (currentRigidbody.worldCenterOfMass - fireOrigin.position).normalized;
                    Debug.DrawRay(fireOrigin.position, dir);
                    if (Vector3.Dot(fireOrigin.forward, dir) > 0.9f)
                    {
                        primaryInput = true;
                    }
                    else
                    {
                        primaryInput = false;
                    }
                }
                else
                {
                    primaryInput = false;
                }
            }
        }
        else
        {
            viewPollTime += Time.fixedDeltaTime;
        }


        if (currentRigidbody == null)
        {
            //We don't have a target, so we'll free rotate
            if (freeRotateFlipFlop)
            {
                freeRotateTime -= (1 / freeRotateSpeed) * Time.fixedDeltaTime;
                if (freeRotateTime < -freeRotateWaitTime)
                    freeRotateFlipFlop.Flip();
            }
            else
            {
                freeRotateTime += (1 / freeRotateSpeed) * Time.fixedDeltaTime;
                if (freeRotateTime > 1 + freeRotateWaitTime)
                {
                    freeRotateFlipFlop.Flip();
                }
            }
            targetAngle = new(0, Mathf.Lerp(-freeRotateRange, freeRotateRange, freeRotateTime) + lastTargetedYaw, 0);
        }
        else
        {
            pointerTransform.LookAt(targetedCollider.bounds.center, transform.up);
            targetAngle = pointerTransform.localEulerAngles;
            lastTargetedYaw = targetAngle.y;
        }
        RotatePitch();
        RotateYaw();
    }

    protected virtual void RotatePitch()
    {
        pitchTransform.localRotation = Quaternion.RotateTowards(pitchTransform.localRotation, Quaternion.Euler(targetAngle.x, 0, 0), targetRotateSpeed * Time.fixedDeltaTime);
    }
    protected virtual void RotateYaw()
    {
        yawTransform.localRotation = Quaternion.RotateTowards(yawTransform.localRotation, Quaternion.Euler(0, targetAngle.y, 0), targetRotateSpeed * Time.fixedDeltaTime);
    }

    void AcquireNewTarget()
    {

        //Debug.Log("Finding new target");
        //We'll do the sphere check, to see what's actually within detection distance
        QueryParameters qp = new()
        {
            layerMask = obstructionLayerMask,
        };
        //viewSphereHits = new NativeArray<ColliderHit>(viewMaxTargets, Allocator.TempJob);
        //viewSphereCommands = new(1, Allocator.TempJob);
        //viewSphereCommands[0] = new(yawTransform.position, viewRange, qp);
        //JobHandle jh = OverlapSphereCommand.ScheduleBatch(viewSphereCommands, viewSphereHits, 1, viewMaxTargets);
        //jh.Complete();

        if(viewSphereColliders.Length != viewMaxTargets)
        {
            viewSphereColliders = new Collider[viewMaxTargets];
        }

        int validHits = Physics.OverlapSphereNonAlloc(yawTransform.position, viewRange, viewSphereColliders, viewLayerMask, QueryTriggerInteraction.Ignore);
        //Debug.Log($"{validHits} targets found by this turret", gameObject);
        boundsCheckCommands = new(viewMaxTargets * 8, Allocator.TempJob);

        for (int i = 0; i < validHits; i++)
        {
            Collider c = viewSphereColliders[i];
            if (c == null || c.attachedRigidbody == null)
            {
                //Invalid result
                continue;
            }
            if (bodiesInRange.Contains(c.attachedRigidbody))
            {
                //No rigidbody, cannot be a valid target
                continue;
            }
            float dot = Vector3.Dot(fireOrigin.forward, (c.transform.position - fireOrigin.position).normalized);
            Debug.DrawLine(yawTransform.position, c.bounds.center, Color.Lerp(Color.red, Color.green, dot), viewPollInterval);
            if (dot > dotFromFOV)
            {
                bodiesInRange.Add(c.attachedRigidbody);

                //We need to now construct the raycast commands to check each corner of the bounds.
                foreach (var item in GetBoundingPoints(c.bounds))
                {
                    boundsCheckCommands[validHits] = new(fireOrigin.position, (Vector3.Lerp(c.bounds.center, item, colliderSizeMultiplier) - fireOrigin.position).normalized, qp, viewRange * 1.2f);
                    Debug.DrawLine(yawTransform.position, item, Color.cyan, viewPollInterval);
                }
            }
        }

        if (validHits > 0)
        {

            boundsCheckHits = new(boundsCheckCommands.Length, Allocator.TempJob);
            //Debug.Log($"{boundsCheckCommands.Length} : {boundsCheckHits.Length}");
            JobHandle jh2 = RaycastCommand.ScheduleBatch(boundsCheckCommands, boundsCheckHits, 1);
            jh2.Complete();

            for (int i = 0; i < boundsCheckHits.Length; i++)
            {
                RaycastHit h = boundsCheckHits[i];
                if (h.collider == null)
                {
                    continue;
                }
                if (h.rigidbody != null && bodiesInRange.Contains(h.rigidbody))
                {
                    //We now have a target! We'll just bin off the rest of them for now lol
                    targetedCollider = h.collider;
                    currentRigidbody = h.rigidbody;
                    break;
                }
                else
                {
                    //Debug.DrawLine(yawTransform.position, h.point, Color.magenta, viewPollInterval);
                }
            }

            if (currentRigidbody)
            {
                //Do something here to process more logic maybe? idk yet
            }
        }
        else
        {
            //Debug.Log("Found no valid rigidbodies within radius! uh oh!");
        }

        if(bodiesInRange.Count > 0)
            bodiesInRange.Clear();
        if(boundsCheckHits.IsCreated)
            boundsCheckHits.Dispose();
        if(boundsCheckCommands.IsCreated)
            boundsCheckCommands.Dispose();
    }
    void KeepOldTarget()
    {
        foreach (var item in GetBoundingPoints(targetedCollider.bounds))
        {
            if (Physics.Raycast(fireOrigin.position, (Vector3.Lerp(targetedCollider.bounds.center, item, colliderSizeMultiplier) - fireOrigin.position), out RaycastHit hit, viewRange * 1.2f, obstructionLayerMask))
            {
                if(hit.rigidbody == currentRigidbody)
                {
                    return;
                }
            }
        }
        //We didn't manage to keep on our current target, so we'll bin it off
        currentRigidbody = null;
    }

    private Vector3[] GetBoundingPoints(Bounds bounds)
    {
        Vector3[] bounding_points =
        {
        bounds.min,
        bounds.max,
        new Vector3( bounds.min.x, bounds.min.y, bounds.max.z ),
        new Vector3( bounds.min.x, bounds.max.y, bounds.min.z ),
        new Vector3( bounds.max.x, bounds.min.y, bounds.min.z ),
        new Vector3( bounds.min.x, bounds.max.y, bounds.max.z ),
        new Vector3( bounds.max.x, bounds.min.y, bounds.max.z ),
        new Vector3( bounds.max.x, bounds.max.y, bounds.min.z )
    };

        return bounding_points;
    }
}
