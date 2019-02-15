using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleController : MonoBehaviour
{
    // general attributes
    [Header("General Attributes")]
    public EAttackableType attackableType;
    public int affiliation = 0;
    public float initialHealthPoints = 100f;
    public float currentHealthPoints = 100f;
    public float maxHealthPoints = 100f;
    public bool battleManaged = true;
    protected bool hasRegisteredAtBattleManager = false;
    [ReadOnly] public bool hit = false;
    [ReadOnly] public bool destroyed = false;
}
