using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DebugCharacterMovementController : MonoBehaviour
{
    public GameObject movementTarget;
    public bool followAttackTarget = true;
    [ReadOnly] public float momentaryVelocity;
    [ReadOnly] public Vector3 navigationTarget;

    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private AudioSource audioSource;
    private CharacterBattleController battleController;
    private GameObject deploymentTarget;
    private float turningSpeed; // in degrees per second

    private Vector3 deploymentPointOffset;
    private Vector3 dodgeTargetPosition;
    private float animationSpeedMultiplier = 1f;
    [ReadOnly] public bool doDodgeAside = false;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.autoBraking = false;

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.pitch *= Random.Range(0.95f, 1.05f);

        battleController = GetComponent<CharacterBattleController>();
        if(battleController != null && battleController.characterDefinition != null)
            turningSpeed = battleController.characterDefinition.turningSpeed;
        else
            turningSpeed = 90f;

        /* a random offset that the character has from the precise deployment point; this is used to avoid 
         * many characters from flocking together at exactly the deployment point */
        deploymentPointOffset = new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f));

        animationSpeedMultiplier = Random.Range(0.98f, 1.02f);
        animator.speed *= animationSpeedMultiplier;
    }

    private void FixedUpdate()
    {
        momentaryVelocity = navMeshAgent.velocity.magnitude;

        // set or unset walk animation
        animator.SetFloat("Speed", momentaryVelocity);

        if(!audioSource.isPlaying && momentaryVelocity >= 0.5f)
            audioSource.Play();
        else if(audioSource.isPlaying && momentaryVelocity < 0.5f)
            audioSource.Stop();

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

        // dodge aside if requested
        if(doDodgeAside)
        {
            float distanceSquared = MathUtilities.VectorDistanceSquared(transform.position, dodgeTargetPosition);
            if(distanceSquared <= 1.5f)
                doDodgeAside = false;
            else
            {
                navMeshAgent.destination = dodgeTargetPosition;
                if(navMeshAgent.isStopped)
                    navMeshAgent.isStopped = false;
            }
        }
        // navigate to attack target that is assigned to the character battle controller
        else if(followAttackTarget && battleController != null && battleController.attackTarget != null)
        {
            navMeshAgent.destination = battleController.attackTarget.transform.position;

            // adjust stopping distance
            if(battleController.attackableType == EAttackableType.CharacterLight)
            {
                float targetDistanceSquared = MathUtilities.VectorDistanceSquared(transform.position, 
                    battleController.attackTarget.transform.position);
                float factor = 0.85f;

                StructureBattleController structureBattleController = battleController.attackTarget.GetComponent<StructureBattleController>();
                if(structureBattleController != null)
                    factor = 0.4f;

                float stoppingDistanceSquared = battleController.attackDefinition.actionRadius * factor;
                stoppingDistanceSquared *= stoppingDistanceSquared;

                // stop movement if the character is closer to its target than the stopping distance (based on action radius)
                navMeshAgent.isStopped = targetDistanceSquared < stoppingDistanceSquared;
            }
        }

        // navigate to the override movement target
        else if(movementTarget != null && movementTarget.activeSelf)
            navMeshAgent.destination = movementTarget.transform.position;

        // navigate to the deployment target (common attack or defense point)
        else if(deploymentTarget != null && deploymentTarget.activeSelf)
            navMeshAgent.destination = deploymentTarget.transform.position + deploymentPointOffset;
        
        if(momentaryVelocity < 1f)
        {
            // always turn towards the navigation target
            float turningStep = turningSpeed * Time.deltaTime;
            Quaternion targetLookRotation = Quaternion.LookRotation(navMeshAgent.destination - transform.position);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetLookRotation, turningStep);
        }

        navigationTarget = navMeshAgent.destination;
    }

    /// <summary>
    /// Call this to let the character dodge a little to the side
    /// </summary>
    public void DodgeAside()
    {
        dodgeTargetPosition = transform.position + transform.right * 3f;
        doDodgeAside = true;

        LogSystem.Log(ELogMessageType.MovementControllerDodgingAside, "dodging <color=white>{0}</color> aside", name);
    }
}
