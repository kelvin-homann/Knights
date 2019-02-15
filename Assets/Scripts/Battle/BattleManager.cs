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
    private const float battleGroupAssemblyDistance = 7f;
    private const float battleGroupDisassemblyDistance = 10f;
    private const float battleGroupUpdateInterval = 0.5f;

    [ReadOnly]
    public int battleGroupUpdateCount = 0;
    private float nextBattleGroupUpdateRealtime = 0f;
    private static bool destroyed = false;

    private static BattleManager instance = null;
    private List<BattleGroup> battleGroups = new List<BattleGroup>();
    private List<CharacterBattleController> characterBattleControllers = new List<CharacterBattleController>();
    private List<StructureBattleController> structureBattleControllers = new List<StructureBattleController>();
    private Dictionary<int, BattleGroup> battleGroupsDictionary = new Dictionary<int, BattleGroup>();
    private Dictionary<CharacterBattleController, BattleGroup> battleGroupMemberAssignments = new Dictionary<CharacterBattleController, BattleGroup>();
    private Dictionary<BattleGroup, BattleGroup> battleGroupPairings = new Dictionary<BattleGroup, BattleGroup>();
    private Dictionary<CharacterBattleController, BattleController> battleControllerTargets = new Dictionary<CharacterBattleController, BattleController>();

    private static Transform battleGroupsParent;
    private static Transform charactersParent;
    private static int nextCharacterId = 1;

    // character prefabs used for spawning
    [Header("Character Prefabs")]
    public GameObject knightGameObjectPrefab;
    public GameObject archerGameObjectPrefab;
    public GameObject heavyGameObjectPrefab;

    // points used for spawning and navigating
    [Header("Navigation Points")]
    public GameObject[] spawnPoints;
    public GameObject[] attackPoints;
    public GameObject[] defensePoints;

    // materials used for spawning and affiliation recognition
    [Header("Common Materials")]
    public Material[] affiliationColorsMaterials;

    public static BattleManager Instance { get { return instance; } protected set {} }

    private void Awake()
    {
        if(instance == null)
            instance = this;
        else if(instance != this) {
            Destroy(gameObject);
            return;
        }

        // create parent object for battle groups
        if(battleGroupsParent == null)
        {
            GameObject battleGroupsParentGameObject = new GameObject("Battle Groups");
            battleGroupsParent = battleGroupsParentGameObject.transform;
        }

        // create parent object for character battle controllers
        if(charactersParent == null)
        {
            GameObject charactersParentGameObject = new GameObject("Characters");
            charactersParent = charactersParentGameObject.transform;
        }

        SpawnInitialCharacters();
    }

    private void OnDestroy()
    {
        destroyed = true;
    }

    private void Update()
    {
        //CheckDebugInput();
    }

    private void CheckDebugInput()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
            SpawnCharacter(EAttackableType.CharacterMedium, 0, EDeploymentType.Attack);

        else if(Input.GetKeyDown(KeyCode.Alpha2))
            SpawnCharacter(EAttackableType.CharacterLight, 0, EDeploymentType.Attack);

        else if(Input.GetKeyDown(KeyCode.Alpha3))
            SpawnCharacter(EAttackableType.CharacterHeavy, 0, EDeploymentType.Attack);

        else if(Input.GetKeyDown(KeyCode.Alpha4))
            SpawnCharacter(EAttackableType.CharacterMedium, 1, EDeploymentType.Attack);

        else if(Input.GetKeyDown(KeyCode.Alpha5))
            SpawnCharacter(EAttackableType.CharacterLight, 1, EDeploymentType.Attack);

        else if(Input.GetKeyDown(KeyCode.Alpha6))
            SpawnCharacter(EAttackableType.CharacterHeavy, 1, EDeploymentType.Attack);

        else if(Input.GetKeyDown(KeyCode.A))
            RedeployCharacters(1, EDeploymentType.Attack);

        else if(Input.GetKeyDown(KeyCode.D))
            RedeployCharacters(1, EDeploymentType.Defense);

        else if(Input.GetKeyDown(KeyCode.H))
        {
            float totalStructureHealthPointsRelative = GetTotalStructureHealthPointsRelative(0);
            LogSystem.Log("total structural health points of affiliation 0: {0:0}%",
                totalStructureHealthPointsRelative * 100f);
        }

        else if(Input.GetKeyDown(KeyCode.S))
        {
            int numAttackCharacters = GetCharactersCount(1, EDeploymentType.Attack);
            int numDefenseCharacters = GetCharactersCount(1, EDeploymentType.Defense);
            LogSystem.Log("{0} characters in attack deployment, {1} in defense deployment",
                numAttackCharacters, numDefenseCharacters);
        }
    }

    private void FixedUpdate()
    {
        // only update battle groups in the specified interval
        if(Time.realtimeSinceStartup >= nextBattleGroupUpdateRealtime)
        {
            //RemoveDestroyedBattleControllers();
            UpdateBattleGroups();
            //AssignBattleGroupPairings();
            AssignBattleControllerTargets();
            nextBattleGroupUpdateRealtime = Time.realtimeSinceStartup + battleGroupUpdateInterval;
        }
    }

    /// <summary>
    /// Gets the singleton instance of the only BattleManager
    /// </summary>
    /// <returns></returns>
    public static BattleManager GetInstance()
    {
        if(instance == null && !destroyed)
            throw new Exception("BattleManager.GetInstance(): Fatal Error: BattleManager has not yet been initialized. " +
                "It needs to be a component of a GameObject. Maybe you need to make sure that the BattleManager script is above " +
                "Default Time in Project Settings/Script Execution Order");
        return instance;
    }

    /// <summary>
    /// Registers a specific CharacterBattleController to the BattleManager's list of considered CharacterBattleControllers
    /// </summary>
    /// <param name="battleController">The CharacterBattleController to register</param>
    public void RegisterCharacterBattleController(CharacterBattleController battleController)
    {
        if(battleController != null && !characterBattleControllers.Contains(battleController))
        {
            characterBattleControllers.Add(battleController);
            LogSystem.Log(ELogMessageType.CharacterBattleControllerRegistering, "registered character battle controller <color=white>{0}</color>",
                battleController.name);
        }
    }

    /// <summary>
    /// Unregisters a specific CharacterBattleController from the BattleManager's list of considered CharacterBattleControllers
    /// </summary>
    /// <param name="battleController">The CharacterBattleController to unregister</param>
    public void UnregisterCharacterBattleController(CharacterBattleController battleController)
    {
        if(battleController == null)
            return;

        // remove battle controller from the battle group assignments
        BattleGroup battleGroup = null;
        if(battleGroupMemberAssignments.ContainsKey(battleController))
            battleGroup = battleGroupMemberAssignments[battleController];

        // first of all remove the character battle controller from its battle group
        if(battleGroup != null)
            battleGroup.RemoveCharacter(battleController);

        // remove the battle controller from the list of managed battle controllers
        if(characterBattleControllers.Contains(battleController))
            characterBattleControllers.Remove(battleController);

        // remove the battle controller from battle group member assignments
        if(battleGroupMemberAssignments.ContainsKey(battleController))
            battleGroupMemberAssignments.Remove(battleController);

        // remove the battle controller from battle controller pairings (being instigator/key)
        if(battleControllerTargets.ContainsKey(battleController))
            battleControllerTargets.Remove(battleController);

        // run through all battle controller targets and mark controllers in the role of receiver (value) for removal
        List<CharacterBattleController> targetKeysToRemove = new List<CharacterBattleController>();
        foreach(KeyValuePair<CharacterBattleController, BattleController> target in battleControllerTargets)
        {
            if(target.Value.Equals(battleController) && !targetKeysToRemove.Contains(target.Key))
                targetKeysToRemove.Add(target.Key);
        }

        // remove the battle controller from all battle controller targets (being receiver/value)
        foreach(CharacterBattleController pairing in targetKeysToRemove)
            battleControllerTargets.Remove(pairing);

        // clear temporary list of pairings marked for removal
        targetKeysToRemove.Clear();

        LogSystem.Log(ELogMessageType.CharacterBattleControllerUnregistering, "unregistered character battle controller <color=white>{0}</color>", 
            battleController.name);
    }

    /// <summary>
    /// Registers a specific StructureBattleController to the BattleManager's list of considered StructureBattleControllers
    /// </summary>
    /// <param name="battleController">The StructureBattleController to register</param>
    public void RegisterStructureBattleController(StructureBattleController battleController)
    {
        if(battleController != null && !structureBattleControllers.Contains(battleController))
        {
            structureBattleControllers.Add(battleController);
            LogSystem.Log(ELogMessageType.StructureBattleControllerRegistering, "registered structure battle controller <color=white>{0}</color>",
                battleController.name);
        }
    }

    /// <summary>
    /// Unregisters a specific StructureBattleController to the BattleManager's list of considered StructureBattleControllers
    /// </summary>
    /// <param name="battleController">The StructureBattleController to register</param>
    public void UnregisterStructureBattleController(StructureBattleController battleController)
    {
        // remove the battle controller from the list of managed battle controllers
        if(structureBattleControllers.Contains(battleController))
            structureBattleControllers.Remove(battleController);

        // run through all battle controller targets and mark controllers in the role of receiver (value) for removal
        List<CharacterBattleController> targetKeysToRemove = new List<CharacterBattleController>();
        foreach(KeyValuePair<CharacterBattleController, BattleController> target in battleControllerTargets)
        {
            if(target.Value.Equals(battleController) && !targetKeysToRemove.Contains(target.Key))
                targetKeysToRemove.Add(target.Key);
        }

        // remove the battle controller from all battle controller targets (being receiver/value)
        foreach(CharacterBattleController pairing in targetKeysToRemove)
            battleControllerTargets.Remove(pairing);

        // clear temporary list of pairings marked for removal
        targetKeysToRemove.Clear();
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
            LogSystem.Log(ELogMessageType.BattleGroupRegistering, "registered battle group <color=white>{0}</color>",
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

        LogSystem.Log(ELogMessageType.BattleGroupUnregistering, "unregistered battle group <color=white>{0}</color>",
            battleGroup.name);
    }

    /// <summary>
    /// Gets the predefined (attribute) spawn point of the given affiliation.
    /// </summary>
    /// <param name="affiliation">the affiliation in question</param>
    /// <returns>the actual spawn point as a gameobject</returns>
    public GameObject GetSpawnPoint(int affiliation)
    {
        return affiliation >= 0 && affiliation < spawnPoints.Length ? spawnPoints[affiliation] : null;
    }

    /// <summary>
    /// Gets the predefined (attribute) attack point of the given affiliation.
    /// </summary>
    /// <param name="affiliation">the affiliation in question</param>
    /// <returns>the actual attack point as a gameobject</returns>
    public GameObject GetAttackPoint(int affiliation)
    {
        return affiliation >= 0 && affiliation < attackPoints.Length ? attackPoints[affiliation] : null;
    }

    public void SetAttackPoint(int affiliation, Vector3 pos)
    {
        attackPoints[affiliation].transform.position = pos;
    }

    public void SetDefendPoint(int affiliation, Vector3 pos)
    {
        defensePoints[affiliation].transform.position = pos;
    }

    /// <summary>
    /// Gets the predefined (attribute) defense point of the given affiliation.
    /// </summary>
    /// <param name="affiliation">the affiliation in question</param>
    /// <returns>the actual defense point as a gameobject</returns>
    public GameObject GetDefensePoint(int affiliation)
    {
        return affiliation >= 0 && affiliation < defensePoints.Length ? defensePoints[affiliation] : null;
    }

    /// <summary>
    /// Returns the number of character battle controllers in the given affiliation.
    /// </summary>
    /// <param name="affiliation">affiliation to consider</param>
    /// <returns></returns>
    public int GetCharactersCount(int affiliation)
    {
        CharacterBattleController[] battleControllers = FindObjectsOfType<CharacterBattleController>();
        int numCharacters = 0;

        foreach(CharacterBattleController battleController in battleControllers)
        {
            if(battleController.affiliation == affiliation)
                numCharacters++;
        }

        return numCharacters;
    }

    /// <summary>
    /// Returns the number of character battle controllers in the given affiliation and with the given deployment type.
    /// </summary>
    /// <param name="affiliation">affiliation to consider</param>
    /// <param name="deploymentType">deployment type to consider</param>
    /// <returns></returns>
    public int GetCharactersCount(int affiliation, EDeploymentType deploymentType)
    {
        CharacterBattleController[] battleControllers = FindObjectsOfType<CharacterBattleController>();
        int numCharacters = 0;

        foreach(CharacterBattleController battleController in battleControllers)
        {
            if(battleController.affiliation == affiliation && battleController.currentDeployment == deploymentType)
                numCharacters++;
        }

        return numCharacters;
    }

    /// <summary>
    /// Returns the number of character battle controllers of the given type, in the given affiliation and with
    /// the given deployment type.
    /// </summary>
    /// <param name="characterType">character type to consider</param>
    /// <param name="affiliation">affiliation to consider</param>
    /// <param name="deploymentType">deployment type to consider</param>
    /// <returns></returns>
    public int GetCharactersCount(EAttackableType characterType, int affiliation, EDeploymentType deploymentType)
    {
        CharacterBattleController[] battleControllers = FindObjectsOfType<CharacterBattleController>();
        int numCharacters = 0;

        foreach(CharacterBattleController battleController in battleControllers)
        {
            if(battleController.attackableType == characterType && battleController.affiliation == affiliation && 
                battleController.currentDeployment == deploymentType)
                numCharacters++;
        }

        return numCharacters;
    }

    /// <summary>
    /// Returns the relative health points in a range [0,1] of all structures belonging to the given affiliation
    /// </summary>
    /// <param name="affiliation">the affiliation that the structures belong to</param>
    /// <returns>the relative health points</returns>
    public float GetTotalStructureHealthPointsRelative(int affiliation)
    {
        float currentHealthPoints = 0f;
        float maximumHealthPoints = 0f;

        foreach(StructureBattleController structureBattleController in structureBattleControllers)
        {
            if(structureBattleController.affiliation == affiliation)
            {
                currentHealthPoints += structureBattleController.currentHealthPoints;
                maximumHealthPoints += structureBattleController.maxHealthPoints;
            }
        }

        float healthPointsRelative = currentHealthPoints / maximumHealthPoints;

        return healthPointsRelative;
    }

    /// <summary>
    /// Spawns a character of specified type into the given affiliation and deployment type and at the 
    /// specified position.
    /// </summary>
    /// <param name="characterType">the type of character to spawn (only character considered!)</param>
    /// <param name="affiliation">the affiliation to associate the new character with</param>
    /// <param name="deploymentType">the deployment that the new character shall occupy</param>
    public void SpawnCharacter(EAttackableType characterType, int affiliation, EDeploymentType deploymentType)
    {
        GameObject spawnPointGameObject = GetSpawnPoint(affiliation);

        if(spawnPointGameObject == null)
        {
            LogSystem.Log(ELogMessageType.CharacterBattleControllerSpawning, 
                "failed spawning new character battle controller for affiliation {0}. " + 
                "There is no spawn point defined for this affiliation.", affiliation);
            return;
        }

        Transform spawnPoint = spawnPointGameObject.transform;
        SpawnCharacter(characterType, affiliation, deploymentType, spawnPoint.position);
    }

    /// <summary>
    /// Spawns a character of specified type into the given affiliation and deployment type and at the 
    /// specified position.
    /// </summary>
    /// <param name="characterType">the type of character to spawn (only character considered!)</param>
    /// <param name="affiliation">the affiliation to associate the new character with</param>
    /// <param name="deploymentType">the deployment that the new character shall occupy</param>
    /// <param name="position">the world position at which to put the new character</param>
    public void SpawnCharacter(EAttackableType characterType, int affiliation, EDeploymentType deploymentType,
        Vector3 position)
    {
        GameObject newCharacter = null;
        
        // look what character type we need to spawn
        switch(characterType)
        {
        case EAttackableType.CharacterMedium:
            if(knightGameObjectPrefab != null)
            {
                // instantiate new character gameobject from prefab
                newCharacter = Instantiate(knightGameObjectPrefab, charactersParent);
                newCharacter.name = string.Format("Knight {0}", nextCharacterId);
            }
            break;

        case EAttackableType.CharacterLight:
            if(archerGameObjectPrefab != null)
            {
                // instantiate new character gameobject from prefab
                newCharacter = Instantiate(archerGameObjectPrefab, charactersParent);
                newCharacter.name = string.Format("Archer {0}", nextCharacterId);
            }
            break;

        case EAttackableType.CharacterHeavy:
            if(heavyGameObjectPrefab != null)
            {
                // instantiate new character gameobject from prefab
                newCharacter = Instantiate(heavyGameObjectPrefab, charactersParent);
                newCharacter.name = string.Format("Heavy {0}", nextCharacterId);
            }
            break;
        }

        if(newCharacter == null)
            return;

        newCharacter.transform.position = position;
        newCharacter.transform.LookAt(Vector3.zero);

        // get character battle controller of new character and assign base properties
        CharacterBattleController battleController = newCharacter.GetComponent<CharacterBattleController>();
        battleController.currentDeployment = deploymentType;
        battleController.affiliation = affiliation;

        nextCharacterId++;

        ChangeAffiliationColor(newCharacter, 0, affiliation);
    }

    /// <summary>
    /// Changes the deployment of a certain number of characters of a given affiliation.
    /// </summary>
    /// <param name="affiliation"></param>
    /// <param name="newDeploymentType">the new deployment</param>
    /// <param name="count">the number of characters to change the deployment of</param>
    /// <returns>the actual number of character deployments changed</returns>
    public int RedeployCharacters(int affiliation, EDeploymentType newDeploymentType, int count = 1)
    {
        if(affiliation < 0 || affiliation > 3 || count <= 0)
            return 0;

        int numCharacterDeploymentsChanged = 0;
        GameObject deploymentPoint = null;

        /* step 1: list all battle controllers in question and order them by their distance to the target deployment 
         * point and prefer battle controllers that do not currently have an attack target assigned to them */

        /* receive a list of all character battle controllers of the given affiliation and that are in other 
           deployments than the target deployment */
        List<CharacterBattleController> battleControllersFiltered = characterBattleControllers.Where(battleController => 
            battleController != null && battleController.affiliation == affiliation && 
            battleController.currentDeployment != newDeploymentType).ToList();

        // get target deployment point if set
        if(newDeploymentType == EDeploymentType.Attack)
            deploymentPoint = GetAttackPoint(affiliation);
        else if(newDeploymentType == EDeploymentType.Defense)
            deploymentPoint = GetDefensePoint(affiliation);

        // sort battle controllers using a custom sorting order
        battleControllersFiltered.Sort(delegate(CharacterBattleController a, CharacterBattleController b)
        {
            float scoreA = 0f;
            float scoreB = 0f;

            /* if the target deployment point is set, get the distances (squared) of both battle controllers 
             * to that point and assign as score */
            if(deploymentPoint != null)
            {
                scoreA = MathUtilities.VectorDistanceSquared(deploymentPoint.transform.position, a.transform.position);
                scoreB = MathUtilities.VectorDistanceSquared(deploymentPoint.transform.position, b.transform.position);
            }

            /* reduce the score drastically if the battle controller does currently has no attack target
             * and thus does not have to pulled out of a fight */
            if(a.attackTarget == null)
                scoreA /= 10f;
            if(b.attackTarget == null)
                scoreB /= 10f;

            return scoreA.CompareTo(scoreB);
        });

        // truncate the list if neccessary
        if(count < battleControllersFiltered.Count)
            battleControllersFiltered.RemoveRange(count, battleControllersFiltered.Count - count);

        foreach(CharacterBattleController battleController in battleControllersFiltered)
        {
            // step 2: change deployment and unset attack target of chosen battle controllers

            battleController.currentDeployment = newDeploymentType;
            battleController.attackTarget = null;
            numCharacterDeploymentsChanged++;

            // step 3: unassign chosen battle controllers from their corresponding battle groups

            if(battleGroupMemberAssignments.ContainsKey(battleController))
            {
                BattleGroup battleGroup = battleGroupMemberAssignments[battleController];
                battleGroup.RemoveCharacter(battleController);
                battleGroupMemberAssignments.Remove(battleController);
            }

            // step 4: remove chosen battle controllers from battle controller pairings where they are an instigator

            if(battleControllerTargets.ContainsKey(battleController))
            {
                battleControllerTargets.Remove(battleController);
            }
        }

        LogSystem.Log(ELogMessageType.CharacterBattleControllerRedeploying, "{0} characters of affiliation {1} redeployed into {1}; " +
            "{2} requested", numCharacterDeploymentsChanged, affiliation, 
            newDeploymentType == EDeploymentType.Attack ? "attack" : "defense", count);

        return numCharacterDeploymentsChanged;
    }

    /// <summary>
    /// Changes the affiliation color of any game object that has renderers and uses predefined affiliation 
    /// based materials.
    /// </summary>
    /// <param name="affiliatedObject">the gameobject that used an affiliation material</param>
    /// <param name="fromAffiliation">the affiliation the object currently is associated with</param>
    /// <param name="toAffiliation">the affiliation the object shall be associated with</param>
    private void ChangeAffiliationColor(GameObject affiliatedObject, int fromAffiliation, int toAffiliation)
    {
        // abort if invalid character, no affiliation colors materials specified, affiliation is less than 0 or greater than 3
        if(affiliatedObject == null || affiliationColorsMaterials.Length < 4 || fromAffiliation < 0 || fromAffiliation > 3 || 
            toAffiliation < 0 || toAffiliation > 3 || fromAffiliation == toAffiliation)
            return;

        Material presentMaterial = affiliationColorsMaterials[fromAffiliation];
        int numRenderers = 0;
        int numMaterials = 0;
        int numMaterialsReplaced = 0;

        SkinnedMeshRenderer[] renderers = affiliatedObject.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach(SkinnedMeshRenderer renderer in renderers)
        {
            numRenderers++;

            Material[] sharedMaterialsCopy = renderer.sharedMaterials;
            for(int i = 0; i < sharedMaterialsCopy.Length; i++)
            {
                numMaterials++;
                if(sharedMaterialsCopy[i] == presentMaterial)
                {
                    sharedMaterialsCopy[i] = affiliationColorsMaterials[toAffiliation];
                    numMaterialsReplaced++;
                }
            }

            renderer.sharedMaterials = sharedMaterialsCopy;
        }

        MeshRenderer[] renderers2 = affiliatedObject.GetComponentsInChildren<MeshRenderer>();
        foreach(MeshRenderer renderer in renderers2)
        {
            numRenderers++;

            Material[] sharedMaterialsCopy = renderer.sharedMaterials;
            for(int i = 0; i < sharedMaterialsCopy.Length; i++)
            {
                numMaterials++;
                if(sharedMaterialsCopy[i] == presentMaterial)
                {
                    sharedMaterialsCopy[i] = affiliationColorsMaterials[toAffiliation];
                    numMaterialsReplaced++;
                }
            }

            renderer.sharedMaterials = sharedMaterialsCopy;
        }

        //LogSystem.Log("{0} materials in {1} renderers found; {2} materials replaced", numMaterials, 
        //    numRenderers, numMaterialsReplaced);
    }

    /// <summary>
    /// Spawns initial characters from placed SpawnPoints on the map.
    /// </summary>
    private void SpawnInitialCharacters()
    {
        // find all spawn points
        SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();
        foreach(SpawnPoint spawnPoint in spawnPoints)
        {
            if(spawnPoint != null && spawnPoint.affiliation >= 0 && spawnPoint.affiliation <= 3)
                SpawnCharacter(spawnPoint.characterType, spawnPoint.affiliation, spawnPoint.deploymentType, 
                    spawnPoint.transform.position);
            Destroy(spawnPoint.gameObject);
        }
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
            List<CharacterBattleController> affiliatedBattleControllers = characterBattleControllers.Where(x => x.affiliation == i).ToList();
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

            float distanceToGroupCenterSquared = MathUtilities.VectorDistanceSquared(battleController.transform.position, 
                battleGroup.centerPoint);

            // if battle controller went outside battle disassembly distance
            if(distanceToGroupCenterSquared >= battleGroupDisassemblyDistance * battleGroupDisassemblyDistance)
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

        // do the actual unassignment of marked battle controllers by removing them from the member assignments list
        foreach(CharacterBattleController battleController in battleControllersToUnassign)
            battleGroupMemberAssignments.Remove(battleController);

        // create one man battle groups for all battle controllers that were not in assembly distance to any other battle controller or group
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
        ClearEmptyBattleGroupPairings();

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
                float distanceSquared = MathUtilities.VectorDistanceSquared(instigator.centerPoint, receiver.centerPoint);

                // check if the instigator group spotted the receiver group (i.e. receiver group is within maximum visibility radius of instigator group)
                if(distanceSquared <= maximumVisibilityRadius * maximumVisibilityRadius)
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
                BattleController mostFittingTarget = null;
                float minDistanceSquared = float.MaxValue;

                // if the current instigator battle controller already has a target continue
                if(battleControllerTargets.ContainsKey(instigatorBattleController) || battleControllerTargets.ContainsValue(instigatorBattleController))
                    continue;

                // run through all receiver battle controllers that are unpaired to find the most fitting
                for(int j = 0; j < receiverBattleControllers.Count; j++)
                {
                    CharacterBattleController receiverBattleController = receiverBattleControllers[j];

                    // if the current receiver battle controller is already paired continue
                    if(battleControllerTargets.ContainsKey(receiverBattleController) || battleControllerTargets.ContainsValue(receiverBattleController))
                        continue;

                    // calculate squared distance between possible pairing's controllers
                    //Vector3 offset = instigatorBattleController.transform.position - receiverBattleController.transform.position;
                    float distanceSquared = MathUtilities.VectorDistanceSquared(instigatorBattleController.transform.position, 
                        receiverBattleController.transform.position);

                    // todo: calculate pairing score based on distance, current health of and effectiveness against opponent

                    // for now only consider the distance between the two battle controllers (easiest approach)
                    if(distanceSquared < minDistanceSquared)
                    {
                        mostFittingTarget = receiverBattleController;
                        minDistanceSquared = distanceSquared;
                    }
                }

                // if we have not yet found a fitting receiver battle controller then probably because all are already paired
                if(mostFittingTarget == null)
                {
                    // run through all receiver battle controllers that are paired to find the most fitting
                    for(int j = 0; j < receiverBattleControllers.Count; j++)
                    {
                        CharacterBattleController receiverBattleController = receiverBattleControllers[j];

                        // if the current receiver battle controller is already an instigator itself continue
                        if(!battleControllerTargets.ContainsValue(receiverBattleController))
                            continue;

                        // calculate squared distance between possible pairing's controllers
                        //Vector3 offset = instigatorBattleController.transform.position - receiverBattleController.transform.position;
                        float distanceSquared = MathUtilities.VectorDistanceSquared(instigatorBattleController.transform.position,
                            receiverBattleController.transform.position);

                        // todo: calculate pairing score based on distance, current health of and effectiveness against opponent

                        // for now only consider the distance between the two battle controllers (easiest approach)
                        if(distanceSquared < minDistanceSquared)
                        {
                            mostFittingTarget = receiverBattleController;
                            minDistanceSquared = distanceSquared;
                        }
                    }
                }

                // only proceed if we have found a fitting receiver battle controller
                if(mostFittingTarget != null)
                {
                    // assign the most fitting receiver battle controller to the instigator battle controller
                    battleControllerTargets.Add(instigatorBattleController, mostFittingTarget);

                    // assign each other as their attack targets
                    instigatorBattleController.AssignAttackTarget(mostFittingTarget.gameObject);

                    /* avoid overwrite and only assign a new attack target to the receiver battle controller if it currently 
                     * has none and if it is in attack deployment */
                    CharacterBattleController mostFittingTargetCharacterBattleController = mostFittingTarget.GetComponent<CharacterBattleController>();
                    if(mostFittingTargetCharacterBattleController != null && mostFittingTargetCharacterBattleController.attackTarget == null &&
                        mostFittingTargetCharacterBattleController.currentDeployment == EDeploymentType.Attack)
                        mostFittingTargetCharacterBattleController.AssignAttackTarget(instigatorBattleController.gameObject);

                    LogSystem.Log(ELogMessageType.CharacterBattleControllerTargetAssigning,
                        "assigned battle controller pairing between <color=white>{0}</color> and <color=white>{1}</color>",
                        instigatorBattleController.name, mostFittingTarget.name);
                }
            }
        }
    }

    private void AssignBattleControllerTargets()
    {
        List<BattleController> possibleTargets = new List<BattleController>();
        for(int i = 0; i < characterBattleControllers.Count; i++)
            possibleTargets.Add(characterBattleControllers[i]);
        for(int i = 0; i < structureBattleControllers.Count; i++)
            possibleTargets.Add(structureBattleControllers[i]);

        // run through all character battle controllers
        for(int i = 0; i < characterBattleControllers.Count; i++)
        {
            CharacterBattleController attackerBattleController = characterBattleControllers[i];
            BattleController mostFittingTarget = null;
            float attackerVisionRadiusSquared = attackerBattleController.characterDefinition.visionRadius * attackerBattleController.characterDefinition.visionRadius;
            float minDistanceSquared = float.MaxValue;

            // if the current attacker battle controller already has a target continue
            if(battleControllerTargets.ContainsKey(attackerBattleController) /*|| battleControllerTargets.ContainsValue(attackerBattleController)*/)
                continue;

            // run through all possible targets that are not yet a target of any character to find the most fitting
            for(int j = 0; j < possibleTargets.Count; j++)
            {
                BattleController targetBattleController = possibleTargets[j];

                // skip if same battle controller or same affiliation
                if(attackerBattleController == targetBattleController || attackerBattleController.affiliation == targetBattleController.affiliation)
                    continue;

                //CharacterBattleController targetCharacterBattleController = targetBattleController.GetComponent<CharacterBattleController>();

                //// if the current target battle controller is already a target continue
                //if((targetCharacterBattleController != null && battleControllerTargets.ContainsKey(targetCharacterBattleController)) ||
                //    battleControllerTargets.ContainsValue(targetCharacterBattleController))
                //    continue;

                // calculate squared distance between the two
                float distanceSquared = MathUtilities.VectorDistanceSquared(attackerBattleController.transform.position,
                    targetBattleController.transform.position);

                // todo: calculate pairing score based on distance, current health of and effectiveness against opponent

                // for now only consider the distance between the two battle controllers (easiest approach)
                if(distanceSquared < minDistanceSquared && distanceSquared <= attackerVisionRadiusSquared)
                {
                    mostFittingTarget = targetBattleController;
                    minDistanceSquared = distanceSquared;
                }
            }

            // if we have not yet found a fitting receiver battle controller then probably because all are already paired
            if(mostFittingTarget == null)
            {
                // run through all receiver battle controllers that are paired to find the most fitting
                for(int j = 0; j < possibleTargets.Count; j++)
                {
                    BattleController targetBattleController = possibleTargets[j];

                    // skip if same battle controller or same affiliation
                    if(attackerBattleController == targetBattleController || attackerBattleController.affiliation == targetBattleController.affiliation)
                        continue;

                    // if the current target battle controller is already an attacker itself continue
                    if(!battleControllerTargets.ContainsValue(targetBattleController))
                        continue;

                    // calculate squared distance between the two
                    float distanceSquared = MathUtilities.VectorDistanceSquared(attackerBattleController.transform.position,
                        targetBattleController.transform.position);

                    // todo: calculate pairing score based on distance, current health of and effectiveness against opponent

                    // for now only consider the distance between the two battle controllers (easiest approach)
                    if(distanceSquared < minDistanceSquared && distanceSquared <= attackerVisionRadiusSquared)
                    {
                        mostFittingTarget = targetBattleController;
                        minDistanceSquared = distanceSquared;
                    }
                }
            }

            // only proceed if we have found a fitting receiver battle controller
            if(mostFittingTarget != null)
            {
                // assign the most fitting receiver battle controller to the instigator battle controller
                battleControllerTargets.Add(attackerBattleController, mostFittingTarget);

                // assign each other as their attack targets
                attackerBattleController.AssignAttackTarget(mostFittingTarget.gameObject);

                /* avoid overwrite and only assign a new attack target to the receiver battle controller if it currently 
                    * has none and if it is in attack deployment */
                //CharacterBattleController mostFittingTargetCharacterBattleController = mostFittingTarget.GetComponent<CharacterBattleController>();
                //if(mostFittingTargetCharacterBattleController != null && mostFittingTargetCharacterBattleController.attackTarget == null &&
                //    mostFittingTargetCharacterBattleController.currentDeployment == EDeploymentType.Attack)
                //    mostFittingTargetCharacterBattleController.AssignAttackTarget(attackerBattleController.gameObject);

                LogSystem.Log(ELogMessageType.CharacterBattleControllerTargetAssigning,
                    "assigned battle controller pairing between <color=white>{0}</color> and <color=white>{1}</color>",
                    attackerBattleController.name, mostFittingTarget.name);
            }
        }
    }

    private void ClearEmptyBattleGroupPairings()
    {
        List<BattleGroup> battleGroupKeysToRemove = new List<BattleGroup>();

        foreach(KeyValuePair<BattleGroup, BattleGroup> pairing in battleGroupPairings)
        {
            if(pairing.Value == null)
                battleGroupKeysToRemove.Add(pairing.Key);
        }

        foreach(BattleGroup battleGroupKey in battleGroupKeysToRemove)
            battleGroupPairings.Remove(battleGroupKey);
    }

    private void RemoveDestroyedBattleControllers()
    {
        List<StructureBattleController> structureBattleControllersToRemove = new List<StructureBattleController>();

        foreach(StructureBattleController structureBattleController in structureBattleControllers)
        {
            if(structureBattleController.destroyed)
                structureBattleControllersToRemove.Add(structureBattleController);
        }

        //foreach(KeyValuePair<CharacterBattleController, BattleController> entry in battleControllerTargets)
        //{
        //    if(entry.Value == )
        //        structureBattleControllersToRemove.Add(structureBattleController);
        //}

        foreach(StructureBattleController structureBattleController in structureBattleControllersToRemove)
        {
            structureBattleControllers.Remove(structureBattleController);
        }
    }
}
