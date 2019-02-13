using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DebugCharacterMovementController : MonoBehaviour
{
    public GameObject movementTarget;
    public bool followAttackTarget = true;
    public float momentaryVelocity;
    public float dotTargetLookDirection;

    private Rigidbody rbody;
    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private CharacterBattleController battleController;
    private float turningSpeed; // in degrees per second

    private float timeSinceLastTurn = 0f;
    private float movementVelocityMultiplier = 1f;

    void Start()
    {
        rbody = GetComponent<Rigidbody>();
        //rbody.velocity = movementVelocity;

        animator = GetComponentInChildren<Animator>();

        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.autoBraking = false;

        battleController = GetComponent<CharacterBattleController>();
        if(battleController != null && battleController.characterDefinition != null)
            turningSpeed = battleController.characterDefinition.turningSpeed;
        else
            turningSpeed = 90f;
    }

    private void OnValidate()
    {
        //if(rbody != null)
        //    rbody.isKinematic = activatePatrol;
    }

    void FixedUpdate()
    {
        momentaryVelocity = navMeshAgent.velocity.magnitude;

        // set or unset walk animation
        animator.SetFloat("Speed", momentaryVelocity);

        if(followAttackTarget && battleController != null && battleController.attackTarget != null)
        {
            navMeshAgent.destination = battleController.attackTarget.transform.position;
        }
        else if(movementTarget != null)
        {
            //if(navMeshAgent.pathPending && navMeshAgent.remainingDistance > 0.2f)
            navMeshAgent.destination = movementTarget.transform.position;
        }
        
        if(momentaryVelocity < 1f)
        {
            if(movementTarget != null)
            {
                float turningStep = turningSpeed * Time.deltaTime;
                Quaternion targetLookRotation = Quaternion.LookRotation(movementTarget.transform.position - transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLookRotation, turningStep);
            }
        }
    }

    //public void SetTarget(GameObject target)
    //{
    //    movementTarget = target;
    //}
}
