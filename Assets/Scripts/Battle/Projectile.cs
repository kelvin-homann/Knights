using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The launching mode of a projectile setting one or several specified launch characteristics as fixed.
/// </summary>
public enum EProjectileLaunchMode
{
    FixedAll, 
    FixedLaunchAngle, 
    FixedLaunchVelocity
}

/// <summary>
/// A projectile that travels through space and wraps an individual attack instance; causes damage specified in the attack instance 
/// to the IAttackable that the projectile collided with/triggered.
/// </summary>
public class Projectile : MonoBehaviour
{
    public const float HEIGHT_THRESHOLD = 0.05f;

    public static Transform lastLaunchedProjectile; // used for special camera view
    private static int staticsLayerId = -2;

    public Transform target;
    public Attack attackData;
    [HideInInspector]
    public EProjectileAimMode projectileAimMode;
    public float targetVelocityEstimationError = 0.1f;
    [Range(-20f, 70f)]
    public float launchAngle = 40f;
    public float launchVelocity = 10f; // in units per second
    public float velocityMultiplier = 1f;
    public EProjectileLaunchMode projectileLaunchMode;
    public float timeToLife = 30f; // maximum time for projectile to life if not hitting anything
    public AudioSource audioSource;
    public AudioClip projectileHitSound;

    public bool debugMode = false;
    [Range(0f, 10f)]
    public float autoLaunchDelay = 1f;
    public bool launchHigh = false;
    public GameObject projectileMesh;
    public bool spawnProjectileCarcassAtTarget = true;
    public bool spawnProjectileCarcassOnGround = true;
    public bool deleteProjectileWhenHitGround = false;
    [Range(1f, 60f)]
    public float projectileCarcassLifespan = 15f;
    public Material hitIndicatorMaterial;
    public float hitIndicatorDuration = 0.2f;
    public Collider spawnerCollider;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 targetPosition;
    private static Transform projectileCarcassesParent;

    private Rigidbody rbody;
    private bool launched = false;
    private bool hit = false;
    private bool touchingGround = false;
    private bool missedTarget = false;
    private bool markForDestroy = false;
    private int coroutinesRunning = 0;

    private void Awake()
    {
        // static member initialization
        if(staticsLayerId == -2)
        {
            staticsLayerId = LayerMask.NameToLayer("Statics");
            if(staticsLayerId == -1)
                Debug.Log("Projectile.Awake(): could not get layer id for layer <color=white>Statics</color>");
        }

        rbody = GetComponent<Rigidbody>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;

        if(!debugMode)
            EnableRigidbody(false);

        if(spawnerCollider != null)
            Physics.IgnoreCollision(spawnerCollider, GetComponent<Collider>());

        lastLaunchedProjectile = transform;

        // create parent object for projectile carcasses
        if(projectileCarcassesParent == null)
        {
            GameObject projectileCarcassesParentGameObject = new GameObject("Projectile Carcasses");
            projectileCarcassesParent = projectileCarcassesParentGameObject.transform;
        }

        // this couroutine stops if projectile hit something before time to life runs out
        StartCoroutine(DestroyAfterTimeToLife(timeToLife));
    }

    private void FixedUpdate()
    {
        // reorient projectile towards the target
        if(rbody != null && !touchingGround && !rbody.velocity.Equals(Vector3.zero))
            transform.rotation = Quaternion.LookRotation(rbody.velocity);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // don't collide with the spawner itself (could happen if rotated unfavorably)
        if(collision.gameObject.Equals(spawnerCollider))
            return;

        CharacterBattleController attacker = attackData.Attacker;
        GameObject attackTarget = attackData.AttackTarget;
        GameObject colliderTarget = collision.gameObject;

        LogSystem.Log(ELogMessageType.ProjectileColliding, "Projectile.OnCollisionEnter(): projectile hit gameobject <color=white>{0}</color>", colliderTarget.name);

        /* ########################################### *
         * EXERT DAMAGE ON IATTACKABLE WHEN ONE IS HIT */

        IAttackable attackable = collision.gameObject.GetComponent<IAttackable>();
        if(attackable != null && !missedTarget && launched)
        {
            launched = false;

            CharacterBattleController attackTargetBattleController = collision.gameObject.GetComponent<CharacterBattleController>();

            // check if wrong affiliation hit
            if(attackTargetBattleController != null && attacker.affiliation == attackTargetBattleController.affiliation)
            {
                attackData.AttackResult = EAttackResult.Failed_HitWrongAffiliation;
                DestroyGameObjectGuided();
                return;
            }

            // check if wrong target hit
            if(!colliderTarget.Equals(attackTarget))
            {
                // update attack data
                AttackDefinition attackDefinition = attackData.AttackDefinition;
                EAttackResult attackResult;
                attackDefinition.GenerateAttack(attacker, colliderTarget, ref attackData, out attackResult);
            }

            // exert attack on target
            attackable.OnAttack(attackData);

            LogSystem.Log(ELogMessageType.ProjectileHitting, "Projectile.OnCollisionEnter(): projectile hit gameobject <color=white>{0}</color>", 
                colliderTarget.name);

            // play hit sound
            if(audioSource != null && projectileHitSound != null)
                audioSource.PlayOneShot(projectileHitSound);

            // spawn projectile carcass mesh within the target
            if(spawnProjectileCarcassAtTarget && projectileMesh != null)
            {
                Transform projectileCarcassParent = collision.gameObject.transform;
                Transform meshGameObject = projectileCarcassParent.FindChildWithTag("Mesh Game Object");

                GameObject projectileCarcass = Instantiate(projectileMesh, meshGameObject ?? projectileCarcassParent, true);
                if(projectileCarcass != null)
                {
                    projectileCarcass.name = projectileMesh.name + "Carcass";
                    // use an exact theoretical contact point as projectile carcass position since the actual projectile position is subject to tunneling
                    projectileCarcass.transform.position = collision.contacts[0].point;
                    StartCoroutine(DestroyCarcass(projectileCarcass, projectileCarcassLifespan));
                    //lastLaunchedProjectile = null;
                }

                if(debugMode)
                    ResetToInitialState();
            }

            // debug: switch material of hit object to hit indicator material
            //if(hitIndicatorMaterial != null && !attackable.Destroyed)
            //{
            //    MeshRenderer meshRenderer = collision.gameObject.GetComponent<MeshRenderer>();
            //    if(meshRenderer != null)
            //    {
            //        Material original = meshRenderer.material;
            //        meshRenderer.material = hitIndicatorMaterial;
            //        StartCoroutine(SetMaterial(original, meshRenderer, hitIndicatorDuration));
            //    }
            //}

            EnableRigidbody(false);
            hit = true;

            //Debug.LogFormat("Projectile.OnCollisionEnter(): projectile hit speed = {0:0.00} u/s", collision.impulse.magnitude);

            DestroyGameObjectGuided();
        }

        /* ################################################################### *
         * DO WHATEVER IS REQUIRED WHEN PROJECTILE DOES NOT HIT AN IATTACKABLE */

        else if(collision.gameObject.layer == staticsLayerId && launched)
        {
            launched = false;
            touchingGround = true;
            missedTarget = true;

            // call attack executed callback
            if(attackData.AttackExecutedCallback != null)
            {
                attackData.AttackResult = EAttackResult.Failed_Mishit;
                attackData.AttackExecutedCallback(attackData);
            }

            LogSystem.Log(ELogMessageType.ProjectileMishitting, "Projectile.OnCollisionEnter(): projectile mishit target and hit some statics");

            if(deleteProjectileWhenHitGround)
            {
                DestroyGameObjectGuided();
            }
            else
            {
                // spawn projectile carcass mesh on the ground
                if(spawnProjectileCarcassOnGround && projectileMesh != null)
                {
                    GameObject projectileCarcass = Instantiate(projectileMesh);
                    if(projectileCarcass != null)
                    {
                        projectileCarcass.name = projectileMesh.name + "Carcass";
                        projectileCarcass.transform.SetPositionAndRotation(projectileMesh.transform.position, projectileMesh.transform.rotation);
                        projectileCarcass.transform.parent = projectileCarcassesParent;
                        StartCoroutine(DestroyCarcass(projectileCarcass, projectileCarcassLifespan));
                    }
                    ResetToInitialState();
                }

                EnableRigidbody(false);
                hit = true;

                DestroyGameObjectGuided();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if(collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            touchingGround = false;
    }

    public void Launch()
    {
        Launch(launchHigh);
    }

    /// <summary>
    /// Aims and launches the projectile in order to hit the target.
    /// </summary>
    public void Launch(bool highTrajectory)
    {
        if(launched || target == null)
            return;

        if(!debugMode)
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        EnableRigidbody(true);

        float targetDistance = 0f, G = Physics.gravity.y, sinTheta = 0f, theta = 0f, actualLaunchAngle = 0f, tanAlpha = 0f, targetHeight = 0f;
        float localInitialVelocityZ = 0f, localInitialVelocityY = 0f, timeOfFlight = 0f;
        Vector3 localInitialVelocity, predictedTargetTranslation;

        /* ###################
         * CALCULATE ACTUAL AIM TARGET POSITION IN RESPECT TO AIM MODE
         * ################### */

        // aim straight even if target is moving
        if(projectileAimMode == EProjectileAimMode.Straight)
        {
            // aim straight for the target and assume momentary target position (stupid technique)
            targetPosition = target.transform.position;
        }
        // aim ahead if target is moving otherwise aim straight
        else
        {
            targetPosition = target.transform.position;
            Vector3 targetVelocity = Vector3.zero;

            // get velocity of target
            // TODO: aquire velocity in a generic way (maybe via an IMoveable interface)
            Rigidbody targetRbody = target.GetComponent<Rigidbody>();
            NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();

            if(targetAgent != null && targetAgent.isActiveAndEnabled)
            {
                targetVelocity = targetAgent.velocity;
            }
            else if(targetRbody != null)
            {
                targetVelocity = targetRbody.velocity;
            }

            // if target is moving aim ahead (human and inhuman precision)
            if(!targetVelocity.Equals(Vector3.zero))
            {
                targetDistance = Vector3.Distance(initialPosition, targetPosition);
                targetHeight = targetPosition.y - initialPosition.y;

                // estimate velocity for human felt predictive ahead aiming
                if(projectileAimMode == EProjectileAimMode.Predictive)
                {
                    // determine velocity estimation error
                    float velocityEstimationError = targetVelocity.magnitude * targetVelocityEstimationError;

                    // add random variation to target velocity to simulate an estimation error
                    targetVelocity.x += Random.Range(-velocityEstimationError, velocityEstimationError);
                    targetVelocity.z += Random.Range(-velocityEstimationError, velocityEstimationError);
                }

                // determine launch velocity for fixed launch angle
                if(projectileLaunchMode == EProjectileLaunchMode.FixedLaunchAngle)
                {
                    // determine basis angle towards target (theta) and adjust actual launch angle
                    sinTheta = targetHeight / targetDistance;
                    theta = Mathf.Asin(sinTheta) * Mathf.Rad2Deg;
                    actualLaunchAngle = theta + launchAngle;
                    tanAlpha = Mathf.Tan(actualLaunchAngle * Mathf.Deg2Rad);

                    // calculate initial projectile velocity required for momentary target position
                    localInitialVelocityZ = Mathf.Sqrt(G * targetDistance * targetDistance / (2f * (targetHeight - targetDistance * tanAlpha)));
                    localInitialVelocityY = tanAlpha * localInitialVelocityZ;
                    localInitialVelocity = new Vector3(0f, localInitialVelocityY, localInitialVelocityZ);

                    // approximate time of flight based on initial target position and initial projectile velocity
                    timeOfFlight = targetDistance / localInitialVelocity.magnitude;

                    // determine target movement direction in respect to initial position (receding or approaching)
                    Vector3 targetDirection = targetPosition - initialPosition;
                    float dot = Vector3.Dot(targetDirection.normalized, targetVelocity.normalized);
                    float timeOfFlightMultiplier = dot > 0f ? 1.4f : 1.1f;

                    // predict flight time
                    float predictedTimeOfFlight = timeOfFlight * timeOfFlightMultiplier;

                    // predict target translation
                    predictedTargetTranslation = targetVelocity * predictedTimeOfFlight;
                }

                // determine launch angle for fixed launch velocity
                else
                {
                    float t = targetDistance / launchVelocity;
                    float a = 0.5f * G * t * t;
                    float c = a - targetHeight;
                    float b = Mathf.Sqrt(targetDistance * targetDistance - targetHeight * targetHeight);

                    // calculate possible launch angles at the given launch velocity
                    double[] tans_theta = MathUtilities.SolveQuadratic(a, b, c);
                    float tan_theta = 1f;

                    // if there is a solution (i.e. target can be hit with the given launch velocity)
                    if(tans_theta != null) {
                        if(tans_theta.Length == 1)
                            tan_theta = (float)tans_theta[0];
                        else {
                            if(highTrajectory)
                                tan_theta = Mathf.Max((float)tans_theta[0], (float)tans_theta[1]);
                            else
                                tan_theta = Mathf.Min((float)tans_theta[0], (float)tans_theta[1]);
                        }
                    }

                    float launchAngleRad = Mathf.Atan(tan_theta);
                    float timeOfPeakHeight = launchVelocity * Mathf.Sin(launchAngleRad) / -G;

                    // simple method: projectile start and landing points are at the same level
                    if(Mathf.Abs(targetHeight) <= HEIGHT_THRESHOLD)
                    {
                        // calculate time of flight
                        timeOfFlight = 2f * timeOfPeakHeight;

                        // calculate predicted target translation
                        predictedTargetTranslation = timeOfFlight * targetVelocity;
                    }
                    // complex method: projectile start and landing points are at different levels
                    else
                    {
                        // calculate time of flight (launch angle is just an approximation derived from initial target position, since the whole
                        // undertake is to determine the specific launch angle for a moving target)
                        float peakHeight = launchVelocity * Mathf.Sin(launchAngleRad) * timeOfPeakHeight + 0.5f * -G * timeOfPeakHeight * timeOfPeakHeight;
                        float heightAboveTarget = peakHeight - targetHeight;
                        float timeOfDescent = Mathf.Sqrt(heightAboveTarget / (2f * -G));
                        timeOfFlight = timeOfPeakHeight + timeOfDescent;

                        //Debug.LogFormat("peakHeight = {0:0.0000000}; heightAboveTarget = {1:0.0000000}; timeOfDescent = {2:0.0000000}; launchAngleApprox = {3:0.0000000} deg",
                        //    peakHeight, heightAboveTarget, timeOfDescent, launchAngleRad * Mathf.Rad2Deg);

                        // determine target movement direction in respect to initial position (receding or approaching)
                        Vector3 targetDirection = targetPosition - initialPosition;
                        float dot = Vector3.Dot(targetDirection.normalized, targetVelocity.normalized);
                        float timeOfFlightMultiplier = dot > 0f ? 1.4f : 1.2f;

                        //Debug.LogFormat("dot = {0}", dot);

                        // predict flight time
                        float predictedTimeOfFlight = timeOfFlight * timeOfFlightMultiplier;

                        // calculate predicted target translation
                        predictedTargetTranslation = targetVelocity * predictedTimeOfFlight;
                    }

                    // calculate predicted time of flight at target
                    //float predictedTimeOfFlight = predictedTargetTranslation.magnitude / targetVelocity.magnitude;

                    //Debug.LogFormat("timeOfFlight = {0:0.0000000} s; predictedTimeOfFlight = {1:0.0000000} s", timeOfFlight, predictedTimeOfFlight);
                }

                // calculate future target position by adding predicted target translation
                targetPosition += predictedTargetTranslation;
            }

            // if target is stationary
            else
            {
                targetPosition = target.transform.position;
            }
        }

        /* ###################
         * CALCULATE ACTUAL TRAJECTORY LAUNCH PARAMETERS (LAUNCH ANGLE OR LAUNCH VELOCITY)
         * ################### */

        targetDistance = Vector3.Distance(initialPosition, targetPosition);
        targetHeight = targetPosition.y - initialPosition.y;

        //Vector3 projectileXZPosition = new Vector3(initialPosition.x, 0f, initialPosition.z);
        Vector3 targetXZPosition = new Vector3(targetPosition.x, initialPosition.y, targetPosition.z);
        transform.LookAt(targetXZPosition);

        if(projectileLaunchMode == EProjectileLaunchMode.FixedLaunchAngle)
        {
            // determine basis angle towards target (theta) and adjust actual launch angle
            sinTheta = targetHeight / targetDistance;
            theta = Mathf.Asin(sinTheta) * Mathf.Rad2Deg;
            actualLaunchAngle = theta + launchAngle;
            tanAlpha = Mathf.Tan(actualLaunchAngle * Mathf.Deg2Rad);

            //Debug.LogFormat("actual target angle = {0:0.00} deg", actualLaunchAngle);

            localInitialVelocityZ = Mathf.Sqrt(G * targetDistance * targetDistance / (2f * (targetHeight - targetDistance * tanAlpha)));
            localInitialVelocityY = tanAlpha * localInitialVelocityZ;
            localInitialVelocity = new Vector3(0f, localInitialVelocityY, localInitialVelocityZ);
        }
        else //if(projectileLaunchMode == EProjectileLaunchMode.FixedLaunchVelocity)
        {
            float t = targetDistance / launchVelocity;
            float a = 0.5f * G * t * t;
            float c = a - targetHeight;
            float b = Mathf.Sqrt(targetDistance * targetDistance - targetHeight * targetHeight);

            // calculate possible launch angles at the given launch velocity
            double[] tans_theta = MathUtilities.SolveQuadratic(a, b, c);
            float tan_theta = 1f; // default angle if no solution exists

            // if there is a solution (i.e. target can be hit with the given launch velocity)
            if(tans_theta != null) {
                if(tans_theta.Length == 1)
                    tan_theta = (float)tans_theta[0];
                else {
                    if(highTrajectory)
                        tan_theta = Mathf.Max((float)tans_theta[0], (float)tans_theta[1]);
                    else
                        tan_theta = Mathf.Min((float)tans_theta[0], (float)tans_theta[1]);
                }
            }
            //else
            //    notify projectile spawner about inaccessibility of target

            float launchAngleRad = Mathf.Atan(tan_theta);

            localInitialVelocityZ = launchVelocity * Mathf.Cos(launchAngleRad);
            localInitialVelocityY = launchVelocity * Mathf.Sin(launchAngleRad);
            localInitialVelocity = new Vector3(0f, localInitialVelocityY, localInitialVelocityZ);

            //Debug.LogFormat("launchAngleCalculated = {0:0.0000000} deg", launchAngleRad * Mathf.Rad2Deg);

            //Debug.LogFormat("h = {0}; c1 = {10}, c2 = {11}, a = {1}; b = {2}; c = {3}; theta = {4} rad = {5} deg{6}, vz = {7}, vy = {8}, v0 = {9}",
            //    targetHeight, a, b, c, launchAngleRad, launchAngleRad * Mathf.Rad2Deg, tans_theta == null ? "; no solution" : "",
            //    localInitialVelocityZ, localInitialVelocityY, localInitialVelocity.magnitude, c1, c2);
        }

        /* ###################
         * ASSIGN THE ACTUAL LAUNCH VELOCITY TO THE RIGIDBODY AND START ITS SIMULATION
         * ################### */

        Vector3 initialVelocity = transform.TransformDirection(localInitialVelocity);
        rbody.velocity = initialVelocity;
        launched = true;

        //Debug.LogFormat("Projectile.Launch(): projectile launch speed = {0:0.00} u/s", initialVelocity.magnitude);
    }

    /// <summary>
    /// Resets this projectile to where it was when last launched (normal) or when instantiated (debug).
    /// Also resets control flags and unsets rigidbody velocity.
    /// </summary>
    private void ResetToInitialState()
    {
        rbody.velocity = Vector3.zero;
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        touchingGround = false;
        launched = false;
        missedTarget = false;
    }

    /// <summary>
    /// Used for timed invocation. Short hand for reset and launch
    /// </summary>
    private void ResetAndLaunch()
    {
        ResetToInitialState();
        Launch();
    }

    /// <summary>
    /// Enables or disables this rigidbody only when needed (i.e. between launch and impact)
    /// </summary>
    /// <param name="enable"></param>
    private void EnableRigidbody(bool enable)
    {
        rbody.isKinematic = !enable;
        rbody.detectCollisions = enable;
    }

    /// <summary>
    /// Set the collider of the spawner so that the spawner doesn't get hit by the projectile during launch
    /// </summary>
    /// <param name="spawnerCollider"></param>
    public void SetSpawnerCollider(Collider spawnerCollider)
    {
        if(spawnerCollider != null)
        {
            this.spawnerCollider = spawnerCollider;
            Physics.IgnoreCollision(spawnerCollider, GetComponent<Collider>());
        }
    }

    /// <summary>
    /// Disables collision detection between this projectile and any collider attached to a CharacterBattleController or StructureController with the specified affiliation number
    /// </summary>
    /// <param name="affiliation"></param>
    public void SetIgnoreCollisionForAffiliation(int affiliation)
    {
        Collider projectileCollider = GetComponent<Collider>();
        int collidersIgnored = 0;

        // TODO: access managed list of all battle controllers (of one affiliation); may be situated in a PlayerController, Kingdom or GameManager class
        var battleControllers = FindObjectsOfType<CharacterBattleController>();
        foreach(CharacterBattleController battleController in battleControllers)
        {
            if(battleController.affiliation != affiliation)
                continue;

            Collider collider = battleController.GetComponent<Collider>();
            if(collider != null)
            {
                Physics.IgnoreCollision(collider, projectileCollider);
                collidersIgnored++;
            }
        }

        //Debug.LogFormat("SetIgnoreCollisionForAffiliation(): ignored {0} colliders for affiliation {1}", collidersIgnored, affiliation);
    }

    /// <summary>
    /// Debug: used for timed coroutine; temporarily sets hit indicator material to hit target mesh renderer
    /// </summary>
    /// <param name="material"></param>
    /// <param name="meshRenderer"></param>
    /// <param name="forSeconds"></param>
    /// <returns></returns>
    private IEnumerator SetMaterial(Material material, MeshRenderer meshRenderer, float forSeconds)
    {
        coroutinesRunning++;
        yield return new WaitForSeconds(forSeconds);
        meshRenderer.material = material;
        coroutinesRunning--;
    }

    /// <summary>
    /// Used for timed coroutine; destroys projectile carcass after a given time
    /// </summary>
    /// <param name="carcass"></param>
    /// <param name="afterSeconds"></param>
    /// <returns></returns>
    private IEnumerator DestroyCarcass(GameObject carcass, float afterSeconds)
    {
        coroutinesRunning++;
        yield return new WaitForSeconds(afterSeconds);
        Destroy(carcass);
        coroutinesRunning--;
    }

    /// <summary>
    /// Destroys this script and its game object in a guided manner after all coroutines have executed.
    /// First removes rigidbody and collider components as well as all children objects.
    /// Marks game objects for destroy and starts actual game object destroy at last.
    /// </summary>
    private void DestroyGameObjectGuided()
    {
        Destroy(GetComponent<Rigidbody>());
        Destroy(GetComponent<Collider>());

        foreach(Transform child in transform)
            Destroy(child.gameObject);

        markForDestroy = true;
        StartCoroutine(DestroyAfterCoroutines());
    }

    /// <summary>
    /// Used for waiting coroutine; destroys this game object when marked for destroy and after all other coroutines have finished
    /// </summary>
    /// <returns></returns>
    private IEnumerator DestroyAfterCoroutines()
    {
        if(!markForDestroy)
            yield break;
        while(coroutinesRunning > 0)
            yield return false;

        // call attack executed callback if not yet happened
        if(attackData != null && attackData.AttackExecutedCallback != null)
        {
            attackData.AttackResult = EAttackResult.Failed_Mishit;
            attackData.AttackExecutedCallback(attackData);
        }

        Destroy(gameObject, 1f);
        yield return true;
    }

    private IEnumerator DestroyAfterTimeToLife(float timeToLife)
    {
        if(hit)
            yield break;
        yield return new WaitForSeconds(timeToLife);

        // call attack executed callback if not yet happened
        if(attackData != null && attackData.AttackExecutedCallback != null)
        {
            attackData.AttackResult = EAttackResult.Failed_Mishit;
            attackData.AttackExecutedCallback(attackData);
        }

        Destroy(gameObject);
        yield return true;
    }
}
