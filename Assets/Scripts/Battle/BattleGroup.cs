using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#pragma warning disable 0414

/// <summary>
/// A group of combined characters of the same affiliation and with the same deployment.
/// </summary>
public class BattleGroup : MonoBehaviour
{
    public static int nextBattleGroupId = 1;

    private bool initialized = false;
    public int battleGroupId;
    public int affiliation;
    public EDeploymentType deployment;
    private List<CharacterBattleController> characters;
    public Vector3 centerPoint;
    private Vector3 positionMinimum;
    private Vector3 positionMaximum;
    private DateTime creationDateTime;
    private float maximumVisibilityRadius;

    public int BattleGroupId { get { return battleGroupId; } set {} }
    public DateTime CreationDateTime { get; protected set; }

    //public BattleGroup(int affiliation, EDeploymentType deployment)
    //{
    //    Initialize(affiliation, deployment);
    //}

    private void Awake()
    {
        Initialize(affiliation, deployment);
    }

    public void Initialize(int affiliation, EDeploymentType deployment)
    {
        if(initialized)
            return;

        this.battleGroupId = nextBattleGroupId++;
        this.affiliation = affiliation;
        this.deployment = deployment;

        name = string.Format("Battle Group {0}", battleGroupId);
        characters = new List<CharacterBattleController>();
        centerPoint = new Vector3();
        positionMaximum = new Vector3();
        positionMinimum = new Vector3();
        creationDateTime = DateTime.UtcNow;

        initialized = true;

        LogSystem.Log(ELogMessageType.BattleGroupCreating, "created battle group <color=white>{0}</color>", name);
    }

    private void FixedUpdate()
    {
        UpdateCenterPoint();
    }

    public void AddCharacter(CharacterBattleController character)
    {
        if(!characters.Contains(character))
        {
            characters.Add(character);
            UpdateCenterPoint();
            UpdateMaximumVisibilityRadius();
        }
    }

    public void RemoveCharacter(CharacterBattleController character)
    {
        if(characters.Contains(character))
        {
            characters.Remove(character);
            UpdateMaximumVisibilityRadius();
        }

        // immediately destroy this battle group once there are no more characters in it
        if(characters.Count == 0)
            DestroyGameObjectGuided();
    }

    public bool ContainsCharacter(CharacterBattleController character)
    {
        return character != null && characters != null && characters.Contains(character);
    }

    public List<CharacterBattleController> GetCharacters()
    {
        return characters.ToList<CharacterBattleController>();
    }

    public int GetCharacterCount()
    {
        return characters != null ? characters.Count : -1;
    }

    public float GetMaximumVisibilityRadius()
    {
        return maximumVisibilityRadius;
    }

    public int Merge(BattleGroup other, bool clearOther)
    {
        if(other == null || other.characters == null || other.characters.Count == 0)
            return 0;

        int added = 0;

        foreach(CharacterBattleController character in other.characters)
        {
            characters.Add(character);
            added++;
        }

        if(clearOther)
            other.Clear();

        UpdateCenterPoint();
        UpdateMaximumVisibilityRadius();

        return added;
    }

    private void Clear()
    {
        characters.Clear();
        UpdateCenterPoint();

        DestroyGameObjectGuided();
    }

    /// <summary>
    /// Update center point of battle group representing the center of mass of all containing characters
    /// </summary>
    public void UpdateCenterPoint()
    {
        if(characters.Count > 1)
        {
            positionMaximum.x = positionMaximum.y = positionMaximum.z = float.MinValue;
            positionMinimum.x = positionMinimum.y = positionMinimum.z = float.MaxValue;

            foreach(CharacterBattleController character in characters)
            {
                if(character.transform.position.x > positionMaximum.x)
                    positionMaximum.x = character.transform.position.x;
                if(character.transform.position.y > positionMaximum.y)
                    positionMaximum.y = character.transform.position.y;
                if(character.transform.position.z > positionMaximum.z)
                    positionMaximum.z = character.transform.position.z;

                if(character.transform.position.x < positionMinimum.x)
                    positionMinimum.x = character.transform.position.x;
                if(character.transform.position.y < positionMinimum.y)
                    positionMinimum.y = character.transform.position.y;
                if(character.transform.position.z < positionMinimum.z)
                    positionMinimum.z = character.transform.position.z;
            }

            centerPoint = Vector3.Lerp(positionMaximum, positionMinimum, 0.5f);
        }

        else if(characters.Count == 1)
        {
            centerPoint = positionMaximum = positionMinimum = characters[0].transform.position;
        }

        else
            centerPoint = Vector3.zero;

        transform.position = centerPoint;
    }

    /// <summary>
    /// Updates the maximum visibility radius (reconnaissance range) of the group based on the visibility radii of its members
    /// </summary>
    private void UpdateMaximumVisibilityRadius()
    {
        if(characters.Count > 0)
        {
            float max = -1f;

            foreach(CharacterBattleController character in characters)
            {
                // get character definition
                if(character.characterDefinition != null && character.characterDefinition.visionRadius > max)
                    max = character.characterDefinition.visionRadius;
            }

            maximumVisibilityRadius = max;
        }
        else
        {
            maximumVisibilityRadius = 0f;
        }
    }

    private void DestroyGameObjectGuided()
    {
        // unregister from Battle Manager
        BattleManager.GetInstance().UnregisterBattleGroup(this);

        string battleGroupName = name;
        Destroy(gameObject);

        LogSystem.Log(ELogMessageType.BattleGroupDestroying, "destroyed battle group <color=white>{0}</color>", battleGroupName);
    }

    private void OnDrawGizmos()
    {
        Color lineColor;

        if(deployment == EDeploymentType.Attack)
            lineColor = Color.red;
        else if(deployment == EDeploymentType.Defense)
            lineColor = Color.blue;
        else if(deployment == EDeploymentType.Other)
            lineColor = Color.green;
        else
            lineColor = Color.yellow;

        Gizmos.color = lineColor;

        foreach(CharacterBattleController character in characters)
        {
            //Debug.DrawLine(character.transform.position, centerPoint, lineColor, 0f, false);
            Gizmos.DrawLine(character.transform.position, centerPoint);
        }

        Gizmos.DrawSphere(centerPoint, 0.15f);
    }
}
