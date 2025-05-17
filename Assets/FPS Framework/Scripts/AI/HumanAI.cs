using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HumanAI : BaseAI
{
    [SerializeField] protected NavMeshAgent nma;
    [SerializeField] protected Character character;

    [SerializeField] protected Vector2 waitTimeBounds;
    [SerializeField] protected float newCommandDistance;

    private void OnValidate()
    {
        if(nma == null)
        {
            nma = GetComponent<NavMeshAgent>();
        }
        if(character == null)
        {
            character = GetComponent<Character>();
        }
    }

    public override void LTimestep()
    {
        base.LTimestep();


    }

    public void FindTarget()
    {

    }
    public void TakeCover()
    {

    }

    
}
