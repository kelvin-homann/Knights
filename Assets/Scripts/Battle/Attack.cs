using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The type of an attack.
/// </summary>
public enum EAttackType
{
    Melee, 
    Projectile
}

/// <summary>
/// The specific result of an attack creation method telling how it failed or if not, the result of a completed attack telling how successful 
/// the attack was.
/// </summary>
public enum EAttackResult
{
    Succeeded_Damaged,
    Succeeded_Destroyed,
    Pending,
    Failed_NullReferenceArgument,
    Failed_TargetNotAttackable,
    Failed_BeyondActionRadius,
    Failed_Mishit,
    Failed_HitWrongAffiliation, 
    Failed_VirtuallyIneffective
}

/// <summary>
/// A class representing an individual attack like a blow, stroke or projectile shot; containing information such as type and result of attack, 
/// attacking battle controller, attacked gameobject, transmitted damage points and so on.
/// </summary>
[System.Serializable]
public class Attack
{
    private readonly EAttackType attackType;
    private EAttackResult attackResult;
    private readonly CharacterBattleController attacker;
    private readonly GameObject attackTarget;
    private readonly float attackDistance;
    private readonly float damagePoints;
    private readonly AttackDefinition attackDefinition;
    private System.Action<Attack> attackFinishedCallback;

    public EAttackType AttackType { get { return attackType; } }
    public EAttackResult AttackResult { get { return attackResult; } set { attackResult = value; } }
    public CharacterBattleController Attacker { get { return attacker; } }
    public GameObject AttackTarget { get { return attackTarget; } }
    public float AttackDistance { get { return attackDistance; } }
    public float DamagePoints { get { return damagePoints; } }
    public AttackDefinition AttackDefinition { get { return attackDefinition; } }
    public System.Action<Attack> AttackExecutedCallback { get { return attackFinishedCallback; } set { attackFinishedCallback = value; } }

    public Attack(EAttackType attackType, EAttackResult attackResult, CharacterBattleController attacker, GameObject attackTarget, 
        float attackDistance, float damagePoints, AttackDefinition attackDefinition)
    {
        this.attackType = attackType;
        this.attackResult = attackResult;
        this.attacker = attacker;
        this.attackTarget = attackTarget;
        this.attackDistance = attackDistance;
        this.damagePoints = damagePoints;
        this.attackDefinition = attackDefinition;
    }

    public void SetAttackFinishedCallback(System.Action<Attack> attackFinishedCallback)
    {
        this.attackFinishedCallback = attackFinishedCallback;
    }
}
