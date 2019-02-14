using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DebugCharacterMovementController : MonoBehaviour
{
    public GameObject movementTarget;
    public bool followAttackTarget = true;
    [ReadOnly] public float momentaryVelocity;

    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private CharacterBattleController battleController;
    private GameObject deploymentTarget;
    private float turningSpeed; // in degrees per second

    private float animationSpeedMultiplier = 1f;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.autoBraking = false;

        battleController = GetComponent<CharacterBattleController>();
        if(battleController != null && battleController.characterDefinition != null)
            turningSpeed = battleController.characterDefinition.turningSpeed;
        else
            turningSpeed = 90f;

        animationSpeedMultiplier = Random.Range(0.98f, 1.02f);
        animator.speed *= animationSpeedMultiplier;
    }

    private void FixedUpdate()
    {
        momentaryVelocity = navMeshAgent.velocity.magnitude;

        // set or unset walk animation
        animator.SetFloat("Speed", momentaryVelocity);

        // find deployment target
        if(battleController != null)
        {
            int affiliation = battleController.affiliation;

            switch(battleController.currentDeployment)
            {
                case EDeploymentType.Attack:
                    deploymentTarget = BattleManager.Instance.GetAttackPoint(affiliation);
                    break;

                case EDeploymentType.Defense:
                    deploymentTarget = BattleManager.Instance.GetDefensePoint(affiliation);
                    break;

                default:
                    deploymentTarget = null;
                    break;
            }
        }

        // navigate to attack target that is assigned to the character battle controller
        if(followAttackTarget && battleController != null && battleController.attackTarget != null)
            navMeshAgent.destination = battleController.attackTarget.transform.position;

        // navigate to the override movement target
        else if(movementTarget != null && movementTarget.activeSelf)
            navMeshAgent.destination = movementTarget.transform.position;

        // navigate to the deployment target (common attack or defense point)
        else if(deploymentTarget != null && deploymentTarget.activeSelf)
            navMeshAgent.destination = deploymentTarget.transform.position;
        
        if(momentaryVelocity < 1f)
        {
            // always turn towards the navigation target
            float turningStep = turningSpeed * Time.deltaTime;
            Quaternion targetLookRotation = Quaternion.LookRotation(navMeshAgent.destination - transform.position);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLookRotation, turningStep);
        }
    }
}
