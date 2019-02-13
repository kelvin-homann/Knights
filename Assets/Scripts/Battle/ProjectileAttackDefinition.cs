using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EProjectileAimMode
{
    Straight, 
    Predictive, 
    Inhuman
}

public enum EProjectileAccuracyMode
{
    ConstantAccuracy,
    DynamicAccuracy
}

public enum EProjectileAccuracyVariationMode
{
    HeadingOnly,
    TrajectoryOnly, 
    HeadingAndTrajectory
}

/// <summary>
/// An object describing all aspects of a projectile attack including aiming method, accuracy and further clout factors.
/// </summary>
[CreateAssetMenu(fileName = "ProjectileAttackDefinition.asset", menuName = "Knights/Projectile Attack Definition")]
public class ProjectileAttackDefinition : AttackDefinition
{
    public float startSpeed = 10f;
    public EProjectileAimMode aimMode;
    public EProjectileAccuracyMode accuracyMode;
    public EProjectileAccuracyVariationMode accuracyVariationMode;
    public float constantAccuracy = 1f;
    public AnimationCurve dynamicAccuracy;
    public bool adjustCloutByHitImpulse;
}
