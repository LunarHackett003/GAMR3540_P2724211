using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitscanTracer : LunarScript
{
    public TrailRenderer trail;
    public Vector3 start;
    public List<Vector3> ends;
    public int currentEndIndex;
    public float distanceIncrement;
    float progress;
    public float aliveTimeAfterEnd;
    public HitscanWeapon owner;
    public float expirationTrailSpeed;
    public float expirationGravityMultiplier;
    float speed;
    bool expiring;
    Vector3 direction;
    public void SendTracer(Vector3 s, Vector3 e, float speed)
    {
        transform.position = s;
        ends.Clear();
        trail.Clear();
        currentEndIndex = 0;
        progress = 0;
        start = s;
        ends.Add(e);
        this.speed = speed;
        CalculateTracer(s, e, speed);
        expiring = false;
    }
    public void AddNextPoint(Vector3 point)
    {
        ends.Add(point);
    }
    public override void LTimestep()
    {
        base.LTimestep();
        Debug.DrawRay(transform.position, direction);
        if (!expiring && progress < 1 && currentEndIndex < ends.Count)
        {
            progress += Time.fixedDeltaTime * distanceIncrement;
            transform.position = Vector3.Lerp(currentEndIndex == 0 ? start : ends[currentEndIndex - 1], ends[currentEndIndex], progress);
        }
        else
        {
            progress += Time.fixedDeltaTime;
            if(currentEndIndex < ends.Count)
            {
                progress = 0;
                if(currentEndIndex + 1 >= ends.Count)
                {
                    expiring = true;
                    currentEndIndex++;
                }
                else
                {
                    currentEndIndex = Mathf.Min(currentEndIndex + 1, ends.Count - 1);
                    CalculateTracer(ends.Count == 1 ? start : ends[currentEndIndex - 1], ends[currentEndIndex], speed);
                }
                
            }
            else
            {
                direction += Time.fixedDeltaTime * Time.fixedDeltaTime * expirationGravityMultiplier * Physics.gravity;
                transform.position += (expirationTrailSpeed * Time.fixedDeltaTime * direction);
                if (progress >= 1 + (aliveTimeAfterEnd * 0.8f))
                {
                    trail.emitting = false;
                }
                if(progress >= aliveTimeAfterEnd + 1f)
                {
                    owner.TracerPool.Release(this);
                }
            }
        }
    }
    void CalculateTracer(Vector3 s, Vector3 e, float speed)
    {
        float dist = Vector3.Distance(s, e);
        direction = (e - s).normalized;
        distanceIncrement = speed / dist;
    }
}
