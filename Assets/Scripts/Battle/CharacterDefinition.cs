using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct AttackableDamagePointsEntry
{
    public EAttackableType attackableType;
    public float damagePoints;
}

[CreateAssetMenu(fileName = "CharacterDefinition.asset", menuName = "Knights/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    public float maxHealthPoints;
    public float visionRadius;
    public float reactionTime;
    public float walkingSpeed;
    public float attackingSpeed;
    public float turningSpeed;
    public AudioClipArray effortSounds;
    public float effortSoundsDelay = 0f;
}
