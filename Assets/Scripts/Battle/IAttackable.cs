using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EAttackableType
{
    CharacterLight,
    CharacterMedium,
    CharacterHeavy,
    StructureLight,
    StructureMedium,
    StructureHeavy
}

public interface IAttackable
{
    void OnAttack(Attack attackData);
}
