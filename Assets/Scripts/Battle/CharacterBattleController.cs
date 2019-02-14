using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The type of character deployment in commanding fashion. At the moment used for attack and defense deployment only.
/// </summary>
public enum EDeploymentType
{
    None, 
    Attack, 
    Defense, 
    Other
}

/// <summary>
/// A class representing the character component responsible for battle. Implements IAttackable and IDestructable and can attack 
/// objects that implement the IAttackable interface.
/// </summary>
public class CharacterBattleController : MonoBehaviour, IAttackable, IDestructable
{
    // constant attributes
    private const float hitIndicatorMaterialTime = 0.2f;

    // static attributes
    //private static Material hitIndicatorMaterial;

    // character attributes
    [Header("Character Attributes")]
    public EAttackableType attackableType;
    public EDeploymentType currentDeployment = EDeploymentType.None;
    public CharacterDefinition characterDefinition;
    public AttackDefinition attackDefinition;
    public GameObject attackTarget;
    public Material hitIndicatorMaterial;
    public Renderer healthRenderer;
    private Animator animator;
    private MaterialPropertyBlock healthRendererMaterialPropertyBlock;
    private int healthRendererMaterialColorId;
    public Canvas healthCanvas;
    public Slider healthSlider;
    public Image healthFillImage;
    public Gradient healthColorGradient;
    public Transform projectileSpawnOrigin;
    public GameObject projectilePrefab;
    public bool executeAttacks = true;
    [Tooltip("Whether to attack the next available target within the same battle group until the battle group is diminished")]
    public bool automaticallyAttackNextTarget = false;
    public float attackCadence = 1f;
    public float initialHealthPoints = 100f;
    public float currentHealthPoints = 100f;
    public float maxHealthPoints = 100f;
    [ReadOnly] public bool hit = false;
    [ReadOnly] public bool destroyed = false;

    // battle group attributes
    [Header("Group Attributes")]
    public int affiliation = 0;
    public BattleGroup assignedBattleGroup;

    // statistical attributes
    [Header("Statistical Attributes")]
    [ReadOnly] public int mishitsInARow = 0;
    [ReadOnly] public int mishitsTotal = 0;
    [ReadOnly] public int attacksTotal = 0;
    [ReadOnly] public float hitProbability = 0f;
    [ReadOnly] public int hitsObtained = 0;
    [ReadOnly] public float damagePointsObtained = 0f;
    [ReadOnly] public float averageDamagePointsObtained = 0f;

    private float timeUntilNextAttack = 0f;
    private float timeSinceLastAttack = 0f;

    private int projectilesLaunched = 0;

    private MeshRenderer[] meshRenderers;
    private Dictionary<MeshRenderer, Material> originalMaterials = new Dictionary<MeshRenderer, Material>();
    private float hitIndicatorUntilTime = 0f;

    private GameObject audioSourceGameObject;
    private AudioSource audioSource;
    private float voicePitch = 1f;

    /* MonoBehaviour methods */

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponentInChildren<AudioSource>();
        audioSourceGameObject = audioSource.gameObject;
        
        if(attackableType == EAttackableType.CharacterHeavy)
            voicePitch = Random.Range(0.5f, 0.65f);
        else
            voicePitch = Random.Range(0.8f, 1.1f);

        audioSource.pitch = voicePitch;

        InitializeHealthIndicator();
    }

    private void Awake()
    {
        maxHealthPoints = characterDefinition.maxHealthPoints;
        currentHealthPoints = initialHealthPoints;
        attackCadence = attackDefinition.cadence;
        //timeSinceLastAttack = characterUnitDefinition.reactionTime + Random.Range(-characterUnitDefinition.reactionTime / 2f, characterUnitDefinition.reactionTime / 2f);
        timeUntilNextAttack = 1f / attackCadence * Random.Range(0f, attackDefinition.cadenceVariation);

        BattleManager.GetInstance().RegisterBattleController(this);

        UpdateHealthIndicator();
    }

    private void OnDestroy()
    {
        BattleManager.GetInstance().UnregisterBattleController(this);
    }

    private void Update()
    {
        // debug
        if(Input.GetKeyDown(KeyCode.A))
        {
            ExecuteAttack(attackTarget);
        }

        // debug
        if(Input.GetKeyDown(KeyCode.R))
        {
            // reset health
            if(currentHealthPoints < maxHealthPoints)
            {
                currentHealthPoints = maxHealthPoints;
                float newScale = Mathf.Max(0.1f, currentHealthPoints / maxHealthPoints);
                transform.localScale = new Vector3(newScale, newScale, newScale);
            }

            // also reset some stats
            if(Input.GetKey(KeyCode.LeftShift))
            {
                mishitsTotal = 0;
                attacksTotal = 0;
                hitProbability = 1f;
            }
        }
    }

    private void FixedUpdate()
    {
        if(!destroyed && attackTarget != null && executeAttacks)
        {
            float distanceToTarget = Vector3.Distance(transform.position, attackTarget.transform.position);

            //if(distanceToTarget > attackDefinition.actionRadius)
            //{
            //    DebugCharacterMovementController movementController = GetComponent<DebugCharacterMovementController>();
            //    if(movementController != null && !movementController.activatePatrol)
            //        movementController.SetTarget(attackTarget);
            //}

            if(distanceToTarget <= attackDefinition.actionRadius)
            {
                timeSinceLastAttack += Time.deltaTime;
                if(timeSinceLastAttack >= timeUntilNextAttack && !hit)
                {
                    ExecuteAttack(attackTarget);

                    timeUntilNextAttack = 1f / attackCadence;
                    timeUntilNextAttack += timeUntilNextAttack * Random.Range(-attackDefinition.cadenceVariation / 2f, attackDefinition.cadenceVariation / 2f);
                    timeSinceLastAttack = 0f;
                }
            }

            // draw debug line between this battle controller and the attack target

        }

        if(Time.time > hitIndicatorUntilTime && hit)
            UnsetHitMaterial();

        // called in FixedUpdate() for debugging purposes
        UpdateHealthIndicator();
    }

    private void OnDrawGizmos()
    {
        if(attackTarget != null)
        {
            Color lineColor = Color.cyan;
            Gizmos.color = lineColor;
            Vector3 fromOffset = new Vector3(0f, 1f, 0f);

            Gizmos.DrawLine(transform.position + fromOffset, attackTarget.transform.position);
        }
    }

    /* IAttackable methods */

    public void OnAttack(Attack attackData)
    {
        if(attackData == null)
            return;

        /* don't exert damage if the attacker was destroyed; an attacker may be destroyed after it transmitted an attack (delayed) 
           but before the attack was received here */
        if(attackData.Attacker != null && attackData.Attacker.destroyed)
            return;

        if(attackData.AttackResult == EAttackResult.Succeeded_Damaged || attackData.AttackResult == EAttackResult.Succeeded_Destroyed || 
            attackData.AttackResult == EAttackResult.Pending)
        {
            float newHealthPoints = Mathf.Max(currentHealthPoints - attackData.DamagePoints, 0f);
            bool isLethal = newHealthPoints == 0f;

            string attackedWithText = attackData.AttackType == EAttackType.Melee ? "hand weapon" : "projectile";
            if(attackData.AttackDefinition.attackedWithText.Length > 0)
                attackedWithText = attackData.AttackDefinition.attackedWithText;

            SetHitMaterial();
            PlayWoodHitSound();
            PlayYelpSound();

            if(isLethal)
            {
                OnDestruct(attackData);
            }
            else
            {
                currentHealthPoints = newHealthPoints;

#if DEBUG
                LogSystem.Log(ELogMessageType.BattleControllerDamaging, "{0} was attacked by <color=white>{1}</color> with <color=yellow>{2}</color> and received {3:0.00} damage points",
                    name, attackData.Attacker.name, attackedWithText, attackData.DamagePoints);
#endif

                attackData.AttackResult = EAttackResult.Succeeded_Damaged;
                if(attackData.AttackExecutedCallback != null)
                    attackData.AttackExecutedCallback(attackData);
            }

            UpdateHealthIndicator();
        }
    }

    /* IDestructable methods */

    public void OnDestruct(Attack attackData)
    {
        // only do this if there is a valid attack and if the attacker still exists
        if(attackData != null && attackData.Attacker != null && attackData.Attacker.gameObject != null)
        {
            CharacterBattleController attacker = attackData.Attacker;

            string attackedWithText = attackData.AttackType == EAttackType.Melee ? "hand weapon" : "projectile";
            if(attackData.AttackDefinition.attackedWithText.Length > 0)
                attackedWithText = attackData.AttackDefinition.attackedWithText;

#if DEBUG
            LogSystem.Log(ELogMessageType.BattleControllerDestroying, "{0} was destroyed by <color=white>{1}</color> with <color=yellow>{2}</color>",
                name, attacker.name, attackedWithText);
#endif

            attackData.AttackResult = EAttackResult.Succeeded_Destroyed;
            if(attackData.AttackExecutedCallback != null)
                attackData.AttackExecutedCallback(attackData);
        }
        else
        {
#if DEBUG
            LogSystem.Log(ELogMessageType.BattleControllerDestroying, "{0} was destroyed", name);
#endif
        }

        currentHealthPoints = 0f;
        // destruction handling
        //Destroy(gameObject, 3f);
        DestroyGuided();
        destroyed = true;
    }

    private void DestroyGuided()
    {
        // outsource the audio source if its still playing a sound
        if(audioSourceGameObject != null)
        {
            audioSourceGameObject.transform.parent = null;
            Destroy(audioSourceGameObject, 2f);
        }

        Destroy(gameObject, hitIndicatorMaterialTime);
    }

    /* CharacterUnitBattleController methods */

    /// <summary>
    /// Assigns a new attack target and moves the battle controller to it.
    /// </summary>
    /// <param name="attackTarget">The target to move to and to attack</param>
    public void AssignAttackTarget(GameObject attackTarget)
    {
        this.attackTarget = attackTarget;
    }

    private void ExecuteAttack(GameObject attackTarget)
    {
        if(attackTarget == null || destroyed)
            return;

        IAttackable attackable = (IAttackable)attackTarget.GetComponent(typeof(IAttackable));
        if(attackable == null)
        {
#if DEBUG
            LogSystem.Log(ELogMessageType.BattleControllerAttackExecuting, "game object <color=white>{0}</color> is not of type IAttackable!", attackTarget.name);
#endif
            return;
        }

        // don't attack fellow kingdom characters
        CharacterBattleController attackTargetBattleController = attackTarget.GetComponent<CharacterBattleController>();
        if(attackTargetBattleController != null && attackTargetBattleController.affiliation == affiliation)
            return;

        // don't attack fellow kingdom structures
        //StructureController attackTargetStructureController = attackTarget.GetComponent<StructureController>();
        //if(attackTargetStructureController != null && attackTargetStructureController.affiliation == affiliation)
        //    return;

        // create an attack from this character unit towards the attack target
        Attack attackData = null;
        EAttackResult attackResult;
        attackDefinition.GenerateAttack(this, attackTarget, ref attackData, out attackResult);
        if(attackData == null)
        {
            LogSystem.Log(ELogMessageType.BattleControllerAttackExecuting, "attack failed because no attack instance could be created");
            return;
        }

        attackData.SetAttackFinishedCallback(OnAttackExecuted);
        this.attackTarget = attackTarget;

        if(attackResult == EAttackResult.Succeeded_Damaged || attackResult == EAttackResult.Succeeded_Destroyed || attackResult == EAttackResult.Pending)
        {
            // switch attack animation on
            animator.SetBool("PerformAttack", true);
            Invoke("PlayStrikeOutSounds", 0.35f);
            StartCoroutine(TransmitAttackDelayed(attackable, attackData, 0.55f));

            attacksTotal++;
        }
        else
        {
            OnAttackExecuted(attackData);
        }
    }

    IEnumerator TransmitAttackDelayed(IAttackable attackable, Attack attackData, float delay)
    {
        if(attackable == null || attackData == null)
            yield break;
        yield return new WaitForSeconds(delay);

        // if the attackable was destroyed while waiting
        if(attackable == null)
            yield break;

        switch(attackData.AttackType)
        {
            case EAttackType.Melee:
                // immediately exert attack on target
                attackable.OnAttack(attackData);
                break;

            case EAttackType.Projectile:
                // launch projectile and let it exert the attack on target or any other IAttackable
                LaunchProjectile(attackData);
                break;
        }

        animator.SetBool("PerformAttack", false);

        yield return true;
    }

    private void OnAttackExecuted(Attack attackData)
    {
        if(attackData == null)
            return;

        // statistics keeping
        if(attackData.AttackResult == EAttackResult.Failed_Mishit)
        {
            mishitsInARow++;
            mishitsTotal++;
        }
        else
            mishitsInARow = 0;

        hitProbability = 1f - ((float)mishitsTotal / attacksTotal);

        if(attackData.AttackResult == EAttackResult.Succeeded_Damaged || attackData.AttackResult == EAttackResult.Succeeded_Destroyed)
        {
            hitsObtained++;
            damagePointsObtained += attackData.DamagePoints;
            averageDamagePointsObtained = damagePointsObtained / hitsObtained;
        }

        // if actual attack target was destroyed request a new target from the battle manager
//        if(attackData.AttackResult == EAttackResult.Succeeded_Destroyed && attackTarget.Equals(attackData.AttackTarget))
//        {
//            CharacterBattleController lastOpponentBattleController = attackData.AttackTarget.GetComponent<CharacterBattleController>();
//            CharacterBattleController newOpponentBattleController = null;
//            if(BattleManager.GetInstance().RequestNewAttackTarget(this, lastOpponentBattleController, out newOpponentBattleController,
//                EOpponentSelectionMethod.MostSignificantOpponent))
//            {
//                attackTarget = newOpponentBattleController.gameObject;
//#if DEBUG
//                LogSystem.Log(ELogMessageType.BattleControllerTargetAssigning, "{0} received <color=white>{1}</color> as its new target from the battle manager", name, attackTarget.name);
//#endif
//            }
//            else
//            {
//#if DEBUG
//                LogSystem.Log(ELogMessageType.BattleControllerTargetAssigning, "{0} did not receive a new target. Opponent battle group is either destroyed or no opponent battle controller is in reach", name);
//#endif
//            }
//        }

        switch(attackData.AttackResult)
        {
            case EAttackResult.Failed_NullReferenceArgument:
                LogSystem.Log(ELogMessageType.BattleControllerAttackExecuting, "attack failed due to null reference error");
                return;

            case EAttackResult.Failed_TargetNotAttackable:
                LogSystem.Log(ELogMessageType.BattleControllerAttackExecuting, "attack failed because target is not attackable");
                return;

            case EAttackResult.Failed_BeyondActionRadius:
                LogSystem.Log(ELogMessageType.BattleControllerAttackExecuting, "attack failed because target is beyond action radius");
                return;

            case EAttackResult.Failed_VirtuallyIneffective:
                LogSystem.Log(ELogMessageType.BattleControllerAttackExecuting, "attack failed because it was totally ineffective");
                return;
        }

        // remove callback reference to this function to avoid double calls
        attackData.SetAttackFinishedCallback(null);
    }

    private void LaunchProjectile(Attack attackData)
    {
        if(projectilePrefab != null && executeAttacks && attackTarget != null)
        {
            GameObject newProjectileGameObject = Instantiate(projectilePrefab);
            Projectile newProjectile = newProjectileGameObject.GetComponent<Projectile>();
            if(newProjectile != null)
            {
                //newProjectile.SetSpawnerCollider(GetComponent<Collider>());
                newProjectile.SetIgnoreCollisionForAffiliation(affiliation);

                if(projectileSpawnOrigin != null)
                    newProjectile.transform.SetPositionAndRotation(projectileSpawnOrigin.position, projectileSpawnOrigin.rotation);
                else
                    newProjectile.transform.SetPositionAndRotation(transform.position, transform.rotation);

                newProjectile.target = attackTarget.transform;
                newProjectile.attackData = attackData;
                newProjectile.Launch();

                // statistics keeping
                projectilesLaunched++;
            }
        }
    }

    private void SetHitMaterial()
    {
        if(!DisplaySettings.renderHitIndicator || hitIndicatorMaterial == null || destroyed)
            return;

        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        originalMaterials.Clear();

        // get and store original materials of all submesh renderers and then set the hit indicator material
        foreach(MeshRenderer meshRenderer in meshRenderers)
        {
            originalMaterials.Add(meshRenderer, meshRenderer.material);
            meshRenderer.material = hitIndicatorMaterial;
        }

        hit = true;
        hitIndicatorUntilTime = Time.time + hitIndicatorMaterialTime;
    }

    private void UnsetHitMaterial()
    {
        if(destroyed)
            return;

        // get and store original materials of all submesh renderers and then set the hit indicator material
        foreach(MeshRenderer meshRenderer in meshRenderers)
        {
            if(meshRenderer == null)
                continue;
            meshRenderer.material = originalMaterials[meshRenderer];
        }

        hit = false;
    }

    public void AssignBattleGroup(BattleGroup battleGroup)
    {
        assignedBattleGroup = battleGroup;
    }

    private void InitializeHealthIndicator()
    {
        if(healthRenderer == null)
        {
            Transform healthRendererTransform = transform.FindChildWithTag("Health Renderer");
            if(healthRendererTransform != null)
                healthRenderer = healthRendererTransform.GetComponent<Renderer>();
        }

        if(healthRenderer != null && healthRendererMaterialPropertyBlock == null)
        {
            healthRendererMaterialPropertyBlock = new MaterialPropertyBlock();
            healthRendererMaterialColorId = Shader.PropertyToID("_Color");
        }
    }

    private void UpdateHealthIndicator()
    {
        if(healthCanvas == null && healthRenderer == null)
            return;

        // enable or disable health canvas and/or renderer based on current display settings
        if(healthCanvas != null && healthCanvas.enabled != DisplaySettings.renderHealthBars)
            healthCanvas.enabled = DisplaySettings.renderHealthBars;
        if(healthRenderer != null && healthRenderer.enabled != DisplaySettings.renderHealthBars)
            healthRenderer.enabled = DisplaySettings.renderHealthBars;

        Color healthColor;
        float currentHealthRelative = currentHealthPoints / characterDefinition.maxHealthPoints;

        // update health slider position
        if(healthSlider != null)
        {
            if(healthFillImage.fillMethod == Image.FillMethod.Radial360)
                healthSlider.value = 100f;
            else
                healthSlider.value = currentHealthRelative * 100f;
        }

        // use color gradient and evaluate momentary color value
        if(healthColorGradient != null)
        {
            healthColor = healthColorGradient.Evaluate(currentHealthRelative);
        }
        // fallback method
        else
        {
            if(currentHealthRelative < 0.5f)
                healthColor = Color.Lerp(Color.red, Color.yellow, currentHealthRelative * 2f);
            else
                healthColor = Color.Lerp(Color.yellow, Color.green, (currentHealthRelative - 0.5f) * 2f);
        }

        // set new health color to health renderer mesh material
        if(healthRenderer != null && healthRendererMaterialPropertyBlock != null)
        {
            healthRendererMaterialPropertyBlock.SetColor(healthRendererMaterialColorId, healthColor);
            healthRenderer.SetPropertyBlock(healthRendererMaterialPropertyBlock);
        }

        // set new health color to UI slider fill image
        if(healthCanvas != null && healthCanvas.enabled && healthFillImage != null)
            healthFillImage.color = healthColor;
    }

    private void PlayStrikeOutSounds()
    {
        if(audioSource == null)
            return;
        audioSource.PlayOneShot(AudioManager.GetRandomGruntSound(), 0.14f);
        audioSource.PlayOneShot(AudioManager.GetRandomWooshSound(), 0.5f);
    }

    private void PlayYelpSound()
    {
        if(audioSource == null)
            return;
        audioSource.PlayOneShot(AudioManager.GetRandomYelpSound(), 0.1f);
    }

    private void PlayWoodHitSound()
    {
        if(audioSource == null)
            return;
        audioSource.PlayOneShot(AudioManager.GetRandomWoodHitSound(), 0.2f);
    }
}
