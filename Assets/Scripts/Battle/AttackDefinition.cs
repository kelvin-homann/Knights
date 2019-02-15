using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An object describing all aspects of an attack including generated damage points, cadence, clout factors and mishit consideration.
/// </summary>
[CreateAssetMenu(fileName = "AttackDefinition.asset", menuName = "Knights/Attack Definition")]
public class AttackDefinition : ScriptableObject
{
    [Tooltip("Describes the general type of attack")]
    public EAttackType attackType;

    [Tooltip("The generic damage points that this attack exerts on attackable types not covered in the list of specific damage points")]
    public float genericDamagePoints;

    [Tooltip("The specific damage points that this attack exerts on given attackable types")]
    public AttackableDamagePointsEntry[] damagePoints;

    [Tooltip("The frequency with that attacks are executed")]
    public float cadence;

    [Tooltip("Describes the maximum variation in percent of the cadence value. Will be multiplied by a random value in range [0-1]. Variegates positively or negatively around the cadence value.")]
    [Range(0f, 1f)]
    public float cadenceVariation;

    [Tooltip("The maximum distance in unit length that this attack has any effectiveness.")]
    public float actionRadius;

    [Tooltip("Whether or not to adjust clout based on the distance relative to action radius (max action radius equals 1.0 on the x-axis of the curve)")]
    public bool adjustCloutByDistance = true;

    [Tooltip("Describes the relative clout based on the distance relative to action radius (max action radius equals 1.0 on the x-axis of the curve). The evaluated value serves as a base clout multiplier.")]
    public AnimationCurve cloutByDistance;

    [Tooltip("Whether or not to adjust clout based on facing direction and angled stances (references cosine values)")]
    public bool adjustCloutByFacingDirection = true;

    [Tooltip("Describes the relative clout based on facing direction (references cosine values: 1 = facing target directly; 0 = standing in right angle to target; -1 = back to target). The evaluated value serves as a base clout multiplier.")]
    public AnimationCurve cloutByFacingDirection;

    [Tooltip("Whether or not to allow the occurence of mishits based on the distance relative to action radius (max action radius equals 1.0 on the x-axis of the curve)")]
    public bool allowMishits = false;

    [Tooltip("Describes how probabilistic a mishit is by distance relative to action radius (max action radius equals 1.0 on the x-axis of the curve). The evaluated probability value will be checked against a random value in range [0-1]")]
    public AnimationCurve mishitProbabilityByDistance;

    [Tooltip("Makes sure that there are not more than n mishits in a row and forces the n+1th attack to be a hit if the previous n were mishits. Mithits in a row have to be recorded by the attacking battle controller.")]
    public int maxMishitsInARow = 2;

    [Tooltip("Whether or not to allow variations to occur by chance (simulates quality of strike and similar factors)")]
    public bool allowCloutVariationByChance = true;

    [Tooltip("Describes the maximum variation in percent of the clout value. Will be multiplied by a random value in range [0-1]. Variegates positively or negatively around the clout value.")]
    [Range(0f, 1f)]
    public float cloutVariationByChance;

    [Tooltip("The text used in UI and debug outputs; following \"X was attacked with \" or \"X was destroyed with \"")]
    public string attackedWithText;

    [Tooltip("An array of audio clips from which one random clip gets played when this attack is executed")]
    public AudioClipArray executionSound;

    [Tooltip("The delay in seconds before to play a random execution sound. Counting starts when the actual attack starts.")]
    public float executionSoundDelay = 0f;

    [Tooltip("An array of audio clips from which one random clip gets played when a target is hit by this attack")]
    public AudioClipArray hitSound;

    /// <summary>
    /// Generates a specific attack data object for a given attacker <b>CharacterBattleController</b> attacking the given target <b>GameObject</b>.
    /// </summary>
    /// <param name="attacker">The attacking CharacterBattleController that shall execute the attack</param>
    /// <param name="attackTarget">The attack target GameObject that shall receive the attack</param>
    /// <param name="attackData">The Attack object that receives the attack data (out) and optionally brings old attack data (in)</param>
    /// <param name="attackResult">The result of the attack describing how exactly it succeeded or failed (out)</param>
    /// <param name="variegate">Whether or not to variegate the effectiveness of the attack (used for determining possible new attack targets)</param>
    /// <returns>The effective damage points that will be exerted onto the actual attack target if the attack succeeds</returns>
    public float GenerateAttack(CharacterBattleController attacker, GameObject attackTarget, ref Attack attackData, 
        out EAttackResult attackResult, bool variegate = true)
    {
        System.Action<Attack> oldAttackFinishedCallback = attackData != null ? attackData.AttackExecutedCallback : null;

        if(attacker == null || attacker.characterDefinition == null || attackTarget == null)
        {
            attackResult = EAttackResult.Failed_NullReferenceArgument;
            attackData = new Attack(attackType, attackResult, attacker, attackTarget, -1f, -1f, this);
            attackData.SetAttackFinishedCallback(oldAttackFinishedCallback);
            return -1f;
        }

        IAttackable attackable = (IAttackable)attackTarget.GetComponent(typeof(IAttackable));
        if(attackable == null)
        {
            attackResult = EAttackResult.Failed_TargetNotAttackable;
            attackData = new Attack(attackType, attackResult, attacker, attackTarget, -1f, -1f, this);
            attackData.SetAttackFinishedCallback(oldAttackFinishedCallback);
            return -1f;
        }

        // calculate distance between attacker and attack target
        float distanceToTarget = Vector3.Distance(attacker.transform.position, attackTarget.transform.position);
        // calculate relative attack distance (from 0.0 to 1.0)
        float relativeAttackDistance = distanceToTarget / actionRadius;

        //Debug.LogFormat("CreateAttack(): distance to target = {0}, relative attack distance = {1}", distanceToTarget, relativeAttackDistance);

        // if attack distance to target is greater than our character unit can attack, return
        if(relativeAttackDistance > 1f)
        {
            attackResult = EAttackResult.Failed_BeyondActionRadius;
            attackData = new Attack(attackType, attackResult, attacker, attackTarget, distanceToTarget, -1f, this);
            attackData.SetAttackFinishedCallback(oldAttackFinishedCallback);
            return -1f;
        }

        // check if mishits are allowed and that the attacker has fewer previous mishits in a row than allowed
        //if(allowMishits && previousMishitsInARow < maxMishitsInARow)
        //{
        //    // get mishit probability by relative attack distance
        //    float mishitProbability = Mathf.Clamp(mishitProbabilityByDistance.Evaluate(relativeAttackDistance), 0f, 1f);
        //    if(mishitProbability > 0f)
        //    {
        //        // roll the dice
        //        float random = Random.Range(0f, 1f);
        //        if(random < mishitProbability)
        //            return new Attack(attackType, EAttackResult.Failed_Mishit, attacker, attackTarget, distanceToTarget, 0f, this);
        //    }
        //    else if(mishitProbability == 1f)
        //        return new Attack(attackType, EAttackResult.Failed_Mishit, attacker, attackTarget, distanceToTarget, 0f, this);
        //}

        float baseClout = 1f;
        float baseDamagePoints = genericDamagePoints;

        CharacterBattleController attackTargetCharacterBattleController = attackTarget.GetComponent<CharacterBattleController>();
        StructureBattleController attackTargetStructureBattleController = attackTarget.GetComponent<StructureBattleController>();

        // get base clout value from damage amounts for this specific attackable type
        if(attackTargetCharacterBattleController != null)
        {
            float attackableDamageAmount = GetSpecificDamagePoints(attackTargetCharacterBattleController.attackableType);
            if(attackableDamageAmount != -1f)
                baseDamagePoints = attackableDamageAmount;
        }
        else if(attackTargetStructureBattleController != null)
        {
            float attackableDamageAmount = GetSpecificDamagePoints(attackTargetStructureBattleController.attackableType);
            if(attackableDamageAmount != -1f)
                baseDamagePoints = attackableDamageAmount;
        }

        // adjust clout by distance
        if(adjustCloutByDistance)
        {
            float cloutByDistanceValue = cloutByDistance.Evaluate(relativeAttackDistance);
            baseClout *= cloutByDistanceValue;
        }

        // adjust clout by facing direction
        if(adjustCloutByFacingDirection)
        {
            Vector3 targetToAttackerDirection = (attackTarget.transform.position - attacker.transform.position).normalized;
            float dotProductFacingDirection = Vector3.Dot(attacker.transform.forward, targetToAttackerDirection);
            float cloutByFacingDirectionValue = cloutByFacingDirection.Evaluate(dotProductFacingDirection);
            baseClout *= cloutByFacingDirectionValue;

            //float facingDirectionDeg = Mathf.Acos(dotProductFacingDirection) * Mathf.Rad2Deg;
            //Debug.LogFormat("CreateAttack(): attacker facing direction = {0:0.00}°", facingDirectionDeg);
        }

        // calculate random clout variation and subtract from base clout
        if(allowCloutVariationByChance && variegate)
        {
            float cloutVariation = baseClout * Random.Range(-cloutVariationByChance / 2f, cloutVariationByChance / 2f);
            baseClout -= cloutVariation;
        }

        // if clout is basically zero don't take it to target.OnAttack()
        if(baseClout < 0.1f)
        {
            attackResult = EAttackResult.Failed_VirtuallyIneffective;
            attackData = new Attack(attackType, attackResult, attacker, attackTarget, distanceToTarget, -1f, this);
            attackData.SetAttackFinishedCallback(oldAttackFinishedCallback);
            return -1f;
        }

        // calculate effective damage points
        float effectiveDamagePoints = baseDamagePoints * baseClout;

        attackResult = EAttackResult.Pending;
        attackData = new Attack(attackType, attackResult, attacker, attackTarget, distanceToTarget, effectiveDamagePoints, this);
        attackData.SetAttackFinishedCallback(oldAttackFinishedCallback);

        return effectiveDamagePoints;
    }

    /// <summary>
    /// Returns the gross damage points that this attack can exert on the given type of attackable.
    /// </summary>
    /// <param name="attackableType">The attackable type to be attacked with this attack</param>
    /// <returns>The actual number of gross damage points without any deduction</returns>
    public float GetSpecificDamagePoints(EAttackableType attackableType)
    {
        foreach(AttackableDamagePointsEntry entry in damagePoints)
        {
            if(entry.attackableType == attackableType)
                return entry.damagePoints;
        }
        return -1f;
    }
}
