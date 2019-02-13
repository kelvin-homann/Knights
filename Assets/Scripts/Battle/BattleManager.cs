using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum EOpponentSelectionMethod
{
    MostSignificantOpponent,
    MostVulnerableOpponent,
    WeakestOpponent,
    ClosestOpponent
}

/// <summary>
/// A singleton class that manages all battle instances, battle groups and all participating and not participating battle controllers.
/// </summary>
public class BattleManager : MonoBehaviour
{
    private const float battleGroupAssemblyDistance = 10f;
    private const float battleGroupDisassemblyDistance = 12f;
    private const float battleCombatAllocationDistance = 15f;
    private const float battleGroupUpdateInterval = 0.5f;

    [ReadOnly]
    public int battleGroupUpdateCount = 0;
    private float nextBattleGroupUpdateRealtime = 0f;
    private static bool destroyed = false;

    private static BattleManager instance = null;
    private List<BattleGroup> battleGroups = new List<BattleGroup>();
    private List<CharacterBattleController> battleControllers = new List<CharacterBattleController>();
    private Dictionary<int, BattleGroup> battleGroupsDictionary = new Dictionary<int, BattleGroup>();
    private Dictionary<CharacterBattleController, BattleGroup> battleGroupMemberAssignments = new Dictionary<CharacterBattleController, BattleGroup>();
    private Dictionary<BattleGroup, BattleGroup> battleGroupPairings = new Dictionary<BattleGroup, BattleGroup>();
    private Dictionary<CharacterBattleController, CharacterBattleController> battleControllerPairings = new Dictionary<CharacterBattleController, CharacterBattleController>();

    private static Transform battleGroupsParent;

    public static BattleManager Instance { get; protected set; }

    private void Awake()
    {
        if(instance == null)
            instance = this;
        else if(instance != this)
            Destroy(gameObject);

        // create parent object for battle groups
        if(battleGroupsParent == null)
        {
            GameObject battleGroupsParentGameObject = new GameObject("Battle Groups");
            battleGroupsParent = battleGroupsParentGameObject.transform;
        }
    }

    private void OnDestroy()
    {
        destroyed = true;
    }

    private void FixedUpdate()
    {
        // only update battle groups in the specified interval
        if(Time.realtimeSinceStartup >= nextBattleGroupUpdateRealtime)
        {
            UpdateBattleGroups();
            AssignBattleGroupPairings();
            AssignBattleControllerPairings();
            nextBattleGroupUpdateRealtime = Time.realtimeSinceStartup + battleGroupUpdateInterval;
        }

        // update center of mass of each battle group every frame
        //foreach(BattleGroup battleGroup in battleGroups)
        //{
        //    battleGroup.UpdateCenterPoint();
        //}
    }

    /// <summary>
    /// Gets the singleton instance of the only BattleManager
    /// </summary>
    /// <returns></returns>
    public static BattleManager GetInstance()
    {
        if(instance == null && !destroyed)
            throw new Exception("BattleManager.GetInstance(): Fatal Error: The Battle Manager has not yet been initialized. " +
                "It needs to be a component of a GameObject. Maybe you need to make sure that the BattleManager script is above " +
                "Default Time in Project Settings/Script Execution Order");
        return instance;
    }

    /// <summary>
    /// Registers a specific CharacterBattleController to the BattleManager's list of considered CharacterBattleControllers
    /// </summary>
    /// <param name="battleController">The CharacterBattleController to register</param>
    public void RegisterBattleController(CharacterBattleController battleController)
    {
        if(battleController != null && !battleControllers.Contains(battleController))
        {
            battleControllers.Add(battleController);
            LogSystem.Log(ELogMessageType.BattleManagerControllerRegistering, "registered battle controller <color=white>{0}</color>",
                battleController.name);
        }
    }

    /// <summary>
    /// Unregisters a specific CharacterBattleController from the BattleManager's list of considered CharacterBattleControllers
    /// </summary>
    /// <param name="battleController">The CharacterBattleController to unregister</param>
    public void UnregisterBattleController(CharacterBattleController battleController)
    {
        if(battleController == null)
            return;

        // remove battle controller from the battle group assignments
        BattleGroup battleGroup = null;
        if(battleGroupMemberAssignments.ContainsKey(battleController))
            battleGroup = battleGroupMemberAssignments[battleController];
        if(battleGroup != null)
        {
            //int battleGroupId = battleGroup.BattleGroupId;

            // remove battle controller from its battle group
            battleGroup.RemoveCharacter(battleController);
            //List<CharacterBattleController> battleGroupBattleControllers = battleGroup.GetCharacters();
            //
            // if the battle group is now empty remove the battle group and all remaining references
            //if(battleGroupBattleControllers.Count == 0)
            //{
            //    battleGroupAssignments.Remove(battleController);
            //    battleGroupsDictionary.Remove(battleGroup.battleGroupId);
            //    battleGroups.Remove(battleGroup);
            //    Destroy(battleGroup);

            //    LogSystem.Log(ELogMessageType.BattleGroupDestroying, "destroyed battle group id {0}", battleGroupId);
            //}
        }

        // remove the battle controller from the list of managed battle controllers
        if(battleControllers.Contains(battleController))
            battleControllers.Remove(battleController);

        // remove the battle controller from battle group member assignments
        if(battleGroupMemberAssignments.ContainsKey(battleController))
            battleGroupMemberAssignments.Remove(battleController);

        // remove the battle controller from battle controller pairings (being instigator/key)
        if(battleControllerPairings.ContainsKey(battleController))
            battleControllerPairings.Remove(battleController);

        // run through all battle controller pairings and mark controllers in the role of receiver (value) for removal
        List<CharacterBattleController> pairingsToRemove = new List<CharacterBattleController>();
        foreach(KeyValuePair<CharacterBattleController, CharacterBattleController> pairing in battleControllerPairings)
        {
            if(pairing.Value.Equals(battleController) && !pairingsToRemove.Contains(pairing.Key))
                pairingsToRemove.Add(pairing.Key);
        }

        // remove the battle controller from all battle controller pairings (being receiver/value)
        foreach(CharacterBattleController pairing in pairingsToRemove)
            battleControllerPairings.Remove(pairing);

        // clear temporary list of pairings marked for removal
        pairingsToRemove.Clear();

        LogSystem.Log(ELogMessageType.BattleManagerControllerUnregistering, "unregistered battle controller <color=white>{0}</color>", 
            battleController.name);
    }

    /// <summary>
    /// Registers a specific BattleGroup to the BattleManager's list of considered BattleGroups
    /// </summary>
    /// <param name="battleGroup">The BattleGroup to register</param>
    private void RegisterBattleGroup(BattleGroup battleGroup)
    {
        if(battleGroup != null && !battleGroups.Contains(battleGroup))
        {
            battleGroups.Add(battleGroup);
            LogSystem.Log(ELogMessageType.BattleManagerGroupRegistering, "registered battle group <color=white>{0}</color>",
                battleGroup.name);
        }
    }

    /// <summary>
    /// Unregisters a specific BattleGroup from the BattleManager's list of considered BattleGroups
    /// </summary>
    /// <param name="battleGroup">The BattleGroup to unregister</param>
    public void UnregisterBattleGroup(BattleGroup battleGroup)
    {
        if(battleGroup == null || !battleGroups.Contains(battleGroup))
            return;

        // remove from battle group assignments
        if(battleGroupMemberAssignments.ContainsValue(battleGroup))
        {
        }

        // remove from battle group dictionary by id
        battleGroupsDictionary.Remove(battleGroup.battleGroupId);

        // remove from considered battle groups
        battleGroups.Remove(battleGroup);

        LogSystem.Log(ELogMessageType.BattleManagerGroupUnregistering, "unregistered battle group <color=white>{0}</color>",
            battleGroup.name);
    }

    /// <summary>
    /// Gets the corresponding BattleGroup to a given CharacterBattleController
    /// </summary>
    /// <param name="battleController">The CharacterBattleController to find the BattleGroup of</param>
    /// <param name="battleGroup">The BattleGroup to be returned</param>
    /// <returns>True if the corresponding BattleGroup could be found and is not null</returns>
    private bool GetBattleGroup(CharacterBattleController battleController, out BattleGroup battleGroup)
    {
        if(battleController == null)
        {
            battleGroup = null;
            return false;
        }

        battleGroup = battleGroupMemberAssignments.ContainsKey(battleController) ? battleGroupMemberAssignments[battleController] : null;
        return battleGroup != null;
    }

    /// <summary>
    /// Requests a new attack target for the attacking CharacterBattleController that is in the same group as its last opponent 
    /// CharacterBattleController and gives it back (out) as the new opponent CharacterBattleController. 
    /// Determination of new opponent can be influenced by the given opponent selection method.
    /// </summary>
    /// <param name="attackingBattleController">The attacking CharacterBattleController</param>
    /// <param name="lastOpponentBattleController">The last opponent CharacterBattleController</param>
    /// <param name="newOpponentBattleController">The new opponent CharacterBattleController (out)</param>
    /// <param name="opponentSelectionMethod">The method for selecting the new opponent</param>
    /// <returns>True if a new opponent CharacterBattleController could be found and given back</returns>
    public bool RequestNewAttackTarget(CharacterBattleController attackingBattleController, CharacterBattleController lastOpponentBattleController, 
        out CharacterBattleController newOpponentBattleController, EOpponentSelectionMethod opponentSelectionMethod)
    {
        if(attackingBattleController == null || lastOpponentBattleController == null || attackingBattleController.automaticallyAttackNextTarget)
        {
            newOpponentBattleController = null;
            return false;
        }

        BattleGroup battleGroup = null;
        if(battleGroupMemberAssignments.ContainsKey(lastOpponentBattleController))
            battleGroup = battleGroupMemberAssignments[lastOpponentBattleController];
        if(battleGroup != null)
        {
            // remove battle controller from its battle group
            List<CharacterBattleController> battleGroupBattleControllers = battleGroup.GetCharacters();
            battleGroupBattleControllers.Remove(lastOpponentBattleController);

            if(battleGroupBattleControllers.Count > 0)
            {
                CharacterBattleController attacker = attackingBattleController;
                AttackDefinition attackDefinition = attacker.attackDefinition;
                CharacterBattleController mostSignificantOpponentBattleController = null;
                float maxPossibleDamagePoints = float.MinValue;
                float minPossibleTargetDistance = float.MaxValue;
                //string consideredOpponentsString = "";

                // iterate through all opponent battle controllers and rate their significance
                foreach(CharacterBattleController possibleOpponentBattleController in battleGroupBattleControllers)
                {
                    Attack possibleAttack = null;
                    EAttackResult possibleAttackResult;
                    float possibleDamagePoints = attackDefinition.GenerateAttack(attacker, possibleOpponentBattleController.gameObject,
                        ref possibleAttack, out possibleAttackResult, false);

                    // don't consider attacks whose result failed
                    if(possibleAttack != null && possibleAttackResult == EAttackResult.Pending)
                    {
                        float possibleTargetDistance = possibleAttack.AttackDistance;
                        //consideredOpponentsString += string.Format("\n   {0}: dist = {1}, edp = {2}", possibleOpponentBattleController.name,
                        //    possibleTargetDistance, possibleDamagePoints);

                        if(possibleDamagePoints > maxPossibleDamagePoints)
                        {
                            maxPossibleDamagePoints = possibleDamagePoints;
                            minPossibleTargetDistance = possibleTargetDistance;
                            mostSignificantOpponentBattleController = possibleOpponentBattleController;
                        }
                        else if(possibleDamagePoints == maxPossibleDamagePoints && possibleTargetDistance < minPossibleTargetDistance)
                        {
                            minPossibleTargetDistance = possibleTargetDistance;
                            mostSignificantOpponentBattleController = possibleOpponentBattleController;
                        }
                    }
                }

                //Debug.LogFormat("RequestNewAttackTarget(): considered new opponents: {0}", consideredOpponentsString);

                // set most significant opponent battle controller as the target of the attacker
                if(mostSignificantOpponentBattleController != null)
                {
                    newOpponentBattleController = mostSignificantOpponentBattleController;
                    return true;
                }
            }
        }

        newOpponentBattleController = null;
        return false;
    }

    /// <summary>
    /// Updates all battle groups and groups all CharacterBattleControllers of the same affiliation and with same deployment together when they are within grouping distance
    /// </summary>
    private void UpdateBattleGroups()
    {
        // get all battle controllers
        //CharacterBattleController[] battleControllers = FindObjectsOfType<CharacterBattleController>();
        Dictionary<int, BattleGroup> newBattleGroups = new Dictionary<int, BattleGroup>();

        // run through fixed set of affiliations
        for(int i = 0; i <= 3; i++)
        {
            Dictionary<int, BattleGroup> battleGroups = new Dictionary<int, BattleGroup>();
            List<CharacterBattleController> affiliatedBattleControllers = battleControllers.Where(x => x.affiliation == i).ToList();
            if(affiliatedBattleControllers.Count > 0)
            {
                UpdateBattleGroups(affiliatedBattleControllers, i, out battleGroups);
                battleGroups.ToList().ForEach(x => newBattleGroups.Add(x.Key, x.Value));
            }
        }

        battleGroupsDictionary = newBattleGroups;
        battleGroupUpdateCount++;
    }

    /// <summary>
    /// Updates all battle groups and groups all CharacterBattleControllers of the given affiliation and with same deployment together when they are within grouping distance
    /// </summary>
    /// <param name="affiliation">The affiliation of the CharacterBattleControllers to be grouped</param>
    private void UpdateBattleGroups(int affiliation)
    {
        // get all battle controllers
        CharacterBattleController[] battleControllers = FindObjectsOfType<CharacterBattleController>();
        Dictionary<int, BattleGroup> newBattleGroups = new Dictionary<int, BattleGroup>();

        List<CharacterBattleController> affiliatedBattleControllers = battleControllers.Where(x => x.affiliation == affiliation).ToList();
        if(affiliatedBattleControllers.Count > 0)
            UpdateBattleGroups(affiliatedBattleControllers, affiliation, out newBattleGroups);
    }

    /// <summary>
    /// Updates all battle groups and groups CharacterBattleControllers within the given list with same deployment together when they are within grouping distance
    /// </summary>
    /// <param name="battleControllers">List of CharacterBattleControllers to be considered</param>
    /// <param name="affiliation">The affiliation of the CharacterBattleControllers to be grouped</param>
    /// <param name="battleGroups">The resulting assembled battle groups</param>
    private void UpdateBattleGroups(List<CharacterBattleController> battleControllers, int affiliation, out Dictionary<int, BattleGroup> battleGroups)
    {
        if(battleControllers == null || battleControllers.Count == 0)
        {
            battleGroups = null;
            return;
        }

        // get all character battle controllers deployed in attack
        Dictionary<int, BattleGroup> attackingBattleGroups = new Dictionary<int, BattleGroup>();
        List<CharacterBattleController> attackingBattleControllers = battleControllers.Where(x => x.currentDeployment == EDeploymentType.Attack).ToList();
        if(attackingBattleControllers.Count > 0)
            AssembleBattleGroups(attackingBattleControllers, affiliation, EDeploymentType.Attack, out attackingBattleGroups);

        // get all character battle controllers deployed in defense
        Dictionary<int, BattleGroup> defendingBattleGroups = new Dictionary<int, BattleGroup>();
        List<CharacterBattleController> defendingBattleControllers = battleControllers.Where(x => x.currentDeployment == EDeploymentType.Defense).ToList();
        if(defendingBattleControllers.Count > 0)
            AssembleBattleGroups(defendingBattleControllers, affiliation, EDeploymentType.Defense, out defendingBattleGroups);

        Dictionary<int, BattleGroup> outBattleGroups = new Dictionary<int, BattleGroup>();
        attackingBattleGroups.ToList().ForEach(x => outBattleGroups.Add(x.Key, x.Value));
        defendingBattleGroups.ToList().ForEach(x => outBattleGroups.Add(x.Key, x.Value));

        battleGroups = outBattleGroups;
    }

    /// <summary>
    /// Assembles battle groups and groups CharacterBattleControllers within the given list when they are within grouping distance.
    /// This method assumes that the given CharacterBattleControllers are of the same affiliation and in the same deployment!
    /// </summary>
    /// <param name="battleControllers">List of CharacterBattleControllers to be considered</param>
    /// <param name="affiliation">The affiliation of the CharacterBattleControllers to be grouped</param>
    /// <param name="deployment">The deployment type (i.e. attack or defense) of the CharacterBattleControllers to be grouped</param>
    /// <param name="battleGroups">The dictionary of resulting assembled battle groups</param>
    private void AssembleBattleGroups(List<CharacterBattleController> battleControllers, int affiliation, EDeploymentType deployment, 
        out Dictionary<int, BattleGroup> battleGroups)
    {
        if(battleControllers == null || battleControllers.Count == 0)
        {
            battleGroups = null;
            return;
        }

        Dictionary<int, BattleGroup> newBattleGroups = new Dictionary<int, BattleGroup>();

        // iterate through all battle controllers pair by pair
        for(int i = 0; i < battleControllers.Count; i++)
        {
            for(int j = 0; j < battleControllers.Count; j++)
            {
                if(i == j) continue;
                CharacterBattleController a = battleControllers[i];
                CharacterBattleController b = battleControllers[j];

                // determine distance between battle controllers
                float distance = Vector3.Distance(a.transform.position, b.transform.position);

                bool containsA = battleGroupMemberAssignments.ContainsKey(a);
                bool containsB = battleGroupMemberAssignments.ContainsKey(b);

                // if both battle controllers are within grouping distance
                if(distance <= battleGroupAssemblyDistance)
                {
                    // merge two existing groups if necessary
                    if(containsA && containsB)
                    {
                        BattleGroup groupA = battleGroupMemberAssignments[a];
                        BattleGroup groupB = battleGroupMemberAssignments[b];

                        // if groups are different
                        if(!groupA.Equals(groupB))
                        {
                            BattleGroup receiver = groupA;
                            BattleGroup issuer = groupB;
                            List<CharacterBattleController> issuedCharacters;

                            int added = 0;

                            // merge smaller into bigger group
                            if(groupA.GetCharacterCount() > groupB.GetCharacterCount())
                            {
                                issuedCharacters = groupB.GetCharacters();
                                added = groupA.Merge(groupB, true);
                            }
                            else if(groupA.GetCharacterCount() < groupB.GetCharacterCount())
                            {
                                issuedCharacters = groupA.GetCharacters();
                                added = groupB.Merge(groupA, true);
                                receiver = groupB;
                                issuer = groupA;
                            }

                            // merge younger into older group
                            else if(groupA.CreationDateTime < groupB.CreationDateTime)
                            {
                                issuedCharacters = groupB.GetCharacters();
                                added = groupA.Merge(groupB, true);
                            }
                            else
                            {
                                issuedCharacters = groupA.GetCharacters();
                                added = groupB.Merge(groupA, true);
                                receiver = groupB;
                                issuer = groupA;
                            }

                            // update battle group assignments for issued characters
                            foreach(CharacterBattleController character in issuedCharacters)
                                battleGroupMemberAssignments[character] = receiver;

                            LogSystem.Log(ELogMessageType.BattleGroupMerging, "merged battle groups <color=white>{0}</color> and <color=white>{1}</color>\nadded {2} characters, now {3} total, to {1}",
                                issuer.name, receiver.name, added, receiver.GetCharacterCount());
                        }

                        // if groups are the same continue
                        else
                            continue;
                    }

                    // add B to existing battle group of A
                    else if(containsA && !containsB)
                    {
                        BattleGroup battleGroup = battleGroupMemberAssignments[a];
                        battleGroup.AddCharacter(b);

                        battleGroupMemberAssignments.Add(b, battleGroup);

                        LogSystem.Log(ELogMessageType.BattleGroupReinforcing, "added <color=white>{0}</color> to existing battle group <color=white>{1}</color>",
                            b.name, battleGroup.name);
                    }

                    // add A to existing battle group of B
                    else if(containsB && !containsA)
                    {
                        BattleGroup battleGroup = battleGroupMemberAssignments[b];
                        battleGroup.AddCharacter(a);

                        battleGroupMemberAssignments.Add(a, battleGroup);

                        LogSystem.Log(ELogMessageType.BattleGroupReinforcing, "added <color=white>{0}</color> to existing battle group <color=white>{1}</color>",
                            a.name, battleGroup.name);
                    }

                    // create new battle group and add both A and B
                    else
                    {
                        GameObject newBattleGroupGameObject = new GameObject();
                        newBattleGroupGameObject.transform.parent = battleGroupsParent;

                        BattleGroup newBattleGroup = newBattleGroupGameObject.AddComponent<BattleGroup>();
                        newBattleGroup.affiliation = affiliation;
                        newBattleGroup.deployment = deployment;
                        newBattleGroup.AddCharacter(a);
                        newBattleGroup.AddCharacter(b);

                        newBattleGroups.Add(newBattleGroup.BattleGroupId, newBattleGroup);

                        //if(!this.battleGroups.Contains(newBattleGroup))
                        //    this.battleGroups.Add(newBattleGroup);
                        RegisterBattleGroup(newBattleGroup);

                        battleGroupMemberAssignments.Add(a, newBattleGroup);
                        battleGroupMemberAssignments.Add(b, newBattleGroup);

                        LogSystem.Log(ELogMessageType.BattleGroupReinforcing, "added <color=white>{0}</color> and <color=white>{1}</color> to battle group <color=white>{2}</color>", 
                            a.name, b.name, newBattleGroup.name);
                    }
                }

                // if both are outside battle group assembly distance
                else
                {
                    // add both battle controllers to their own pseudo solo battle groups
                }
            }
        }

        List<CharacterBattleController> battleControllersToUnassign = new List<CharacterBattleController>();

        // iterate through all battle group assignments and check if any battle controller went outside battle disassembly distance
        foreach(KeyValuePair<CharacterBattleController, BattleGroup> entry in battleGroupMemberAssignments)
        {
            CharacterBattleController battleController = entry.Key;
            BattleGroup battleGroup = entry.Value;

            float distanceToGroupCenter = Vector3.Distance(battleController.transform.position, battleGroup.centerPoint);

            // if battle controller went outside battle disassembly distance
            if(distanceToGroupCenter >= battleGroupDisassemblyDistance)
            {
                // mark battle controller for removal from battle group assignments
                battleControllersToUnassign.Add(battleController);

                // remove battle controller from battle group
                battleGroup.RemoveCharacter(battleController);

                // add battle controller to its own pseudo solo battle group

                LogSystem.Log(ELogMessageType.BattleGroupWeakening, "removed <color=white>{0}</color> from battle group <color=white>{1}</color>",
                    battleController.name, battleGroup.name);
            }
        }

        foreach(CharacterBattleController battleController in battleControllersToUnassign)
            battleGroupMemberAssignments.Remove(battleController);

        // create one man battle groups for all battle controllers that were not in assembly distance to any other battle controller
        // iterate through all battle controllers pair by pair
        for(int i = 0; i < battleControllers.Count; i++)
        {
            CharacterBattleController battleController = battleControllers[i];

            // if battle controller has not yet been assigned to a battle group
            if(!battleGroupMemberAssignments.ContainsKey(battleController))
            {
                // create a new one man battle group for it
                GameObject newBattleGroupGameObject = new GameObject();
                newBattleGroupGameObject.transform.parent = battleGroupsParent;

                BattleGroup newBattleGroup = newBattleGroupGameObject.AddComponent<BattleGroup>();
                newBattleGroup.affiliation = affiliation;
                newBattleGroup.deployment = deployment;
                newBattleGroup.AddCharacter(battleController);

                newBattleGroups.Add(newBattleGroup.BattleGroupId, newBattleGroup);

                RegisterBattleGroup(newBattleGroup);

                battleGroupMemberAssignments.Add(battleController, newBattleGroup);

                LogSystem.Log(ELogMessageType.BattleGroupReinforcing, "added <color=white>{0}</color> to one man battle group <color=white>{1}</color>",
                    battleController.name, newBattleGroup.name);
            }
        }

        // update assigned battle group in all battle controllers
        foreach(KeyValuePair<CharacterBattleController, BattleGroup> entry in battleGroupMemberAssignments)
            entry.Key.assignedBattleGroup = entry.Value;

        battleGroups = newBattleGroups;
    }

    /// <summary>
    /// Assigns battle pairings between battle groups that are in range to each other and not already in a pairing.
    /// Subsequent steps will make sure that the groups continue moving towards each other and start the battle.
    /// </summary>
    private void AssignBattleGroupPairings()
    {
        // run through all registered battle groups and find unengaged combat instigators
        for(int i = 0; i < battleGroups.Count; i++)
        {
            BattleGroup instigator = battleGroups[i];

            // check if the instigator is already engaged
            if(battleGroupPairings.ContainsKey(instigator))
            {
                // skip this group as an instigator, but not as a receiver
                continue;
            }

            // run through all possible combat receivers (passives)
            for(int j = 0; j < battleGroups.Count; j++)
            {
                // skip this group if it is the same or if its from the same affiliation
                if(i == j || battleGroups[i].affiliation == battleGroups[j].affiliation)
                    continue;

                BattleGroup receiver = battleGroups[j];

                float maximumVisibilityRadius = instigator.GetMaximumVisibilityRadius();
                float distance = Vector3.Distance(instigator.centerPoint, receiver.centerPoint);

                // check if the instigator group spotted the receiver group (i.e. receiver group is within maximum visibility radius of instigator group)
                if(distance <= maximumVisibilityRadius)
                {
                    battleGroupPairings.Add(instigator, receiver);
                }
            }
        }
    }

    /// <summary>
    /// Assigns single combat pairings between battle controllers of assigned battle group pairings.
    /// This method will also set the corresponding attack targets of the battle controllers which in turn will start the single combats.
    /// </summary>
    private void AssignBattleControllerPairings()
    {
        // run through all assigned battle group pairings
        foreach(KeyValuePair<BattleGroup, BattleGroup> battleGroupPairing in battleGroupPairings)
        {
            BattleGroup instigator = battleGroupPairing.Key;
            BattleGroup receiver = battleGroupPairing.Value;

            // get list of battle controllers of both participating battle groups
            List<CharacterBattleController> instigatorBattleControllers = instigator.GetCharacters();
            List<CharacterBattleController> receiverBattleControllers = receiver.GetCharacters();

            // this should practically not happen since the battle group should have been removed by now (precaution)
            if(instigatorBattleControllers.Count == 0 || receiverBattleControllers.Count == 0)
                continue;

            // create spread single combat positions
            //int minCount = Math.Min(instigatorBattleControllers.Count, receiverBattleControllers.Count);

            // run through all instigator battle controllers
            for(int i = 0; i < instigatorBattleControllers.Count; i++)
            {
                CharacterBattleController instigatorBattleController = instigatorBattleControllers[i];
                CharacterBattleController mostFittingReceiverBattleController = null;
                float minDistanceSquared = float.MaxValue;

                // if the current instigator battle controller is already paired continue
                if(battleControllerPairings.ContainsKey(instigatorBattleController) || battleControllerPairings.ContainsValue(instigatorBattleController))
                    continue;

                // run through all receiver battle controllers that are unpaired to find the most fitting
                for(int j = 0; j < receiverBattleControllers.Count; j++)
                {
                    CharacterBattleController receiverBattleController = receiverBattleControllers[j];

                    // if the current receiver battle controller is already paired continue
                    if(battleControllerPairings.ContainsKey(receiverBattleController) || battleControllerPairings.ContainsValue(receiverBattleController))
                        continue;

                    // calculate squared distance between possible pairing's controllers
                    Vector3 offset = instigatorBattleController.transform.position - receiverBattleController.transform.position;
                    float distanceSquared = offset.sqrMagnitude;

                    // todo: calculate pairing score based on distance, current health of and effectiveness against opponent

                    // for now only consider the distance between the two battle controllers (easiest approach)
                    if(distanceSquared < minDistanceSquared)
                    {
                        mostFittingReceiverBattleController = receiverBattleController;
                        minDistanceSquared = distanceSquared;
                    }
                }

                // if we have not yet found a fitting receiver battle controller then probably because all are already paired
                if(mostFittingReceiverBattleController == null)
                {
                    // run through all receiver battle controllers that are paired to find the most suitable
                    for(int j = 0; j < receiverBattleControllers.Count; j++)
                    {
                        CharacterBattleController receiverBattleController = receiverBattleControllers[j];

                        // if the current receiver battle controller is already paired continue
                        if(!battleControllerPairings.ContainsValue(receiverBattleController))
                            continue;

                        // calculate squared distance between possible pairing's controllers
                        Vector3 offset = instigatorBattleController.transform.position - receiverBattleController.transform.position;
                        float distanceSquared = offset.sqrMagnitude;

                        // todo: calculate pairing score based on distance, current health of and effectiveness against opponent

                        // for now only consider the distance between the two battle controllers (easiest approach)
                        if(distanceSquared < minDistanceSquared)
                        {
                            mostFittingReceiverBattleController = receiverBattleController;
                            minDistanceSquared = distanceSquared;
                        }
                    }
                }

                // only proceed if we have found a fitting receiver battle controller
                if(mostFittingReceiverBattleController != null)
                {
                    // assign the most fitting receiver battle controller to the instigator battle controller
                    battleControllerPairings.Add(instigatorBattleController, mostFittingReceiverBattleController);

                    // assign each other as their attack targets
                    instigatorBattleController.AssignAttackTarget(mostFittingReceiverBattleController.gameObject);
                    if(mostFittingReceiverBattleController.attackTarget == null)
                        mostFittingReceiverBattleController.AssignAttackTarget(instigatorBattleController.gameObject);

                    LogSystem.Log(ELogMessageType.BattleControllerTargetAssigning,
                        "assigned battle controller pairing between <color=white>{0}</color> and <color=white>{1}</color>",
                        instigatorBattleController.name, mostFittingReceiverBattleController.name);
                }
            }
        }
    }
}
