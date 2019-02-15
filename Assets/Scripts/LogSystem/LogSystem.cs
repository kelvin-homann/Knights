using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public enum ELogMessageType
{
    Default, 
    ResourceLoading, 

    // object message types
    ObjectColliding, 
    ObjectCreating, 
    ObjectDestroying, 

    // game manager message types
    GameStarting, 
    GamePausing, 
    GameWinning, 
    GameLosing, 
    GameEnding, 

    // kingdom message types
    KingdomCreating, 
    KingdomReinforcing, 
    KingdomWinning, 
    KingdomLosing, 
    KingdomDestroying, 

    // movement controller message types
    MovementControllerCreating,
    MovementControllerDestroying,
    MovementControllerDodgingAside, 

    // character battle controller message types
    CharacterBattleControllerCreating, 
    CharacterBattleControllerSpawning, 
    CharacterBattleControllerRegistering,
    CharacterBattleControllerUnregistering,
    CharacterBattleControllerAttackExecuting, 
    CharacterBattleControllerAttackReceiving, 
    CharacterBattleControllerDamaging, 
    CharacterBattleControllerDestroying, 
    CharacterBattleControllerTargetAssigning, 
    CharacterBattleControllerTargetChoosing,
    CharacterBattleControllerRedeploying,

    // structure battle controller message types
    StructureBattleControllerCreating,
    StructureBattleControllerSpawning,
    StructureBattleControllerRegistering,
    StructureBattleControllerUnregistering,
    StructureBattleControllerAttackReceiving,
    StructureBattleControllerDamaging,
    StructureBattleControllerDestroying,

    // battle group message types
    BattleGroupCreating, 
    BattleGroupRegistering, 
    BattleGroupUnregistering, 
    BattleGroupReinforcing, 
    BattleGroupWeakening, 
    BattleGroupMerging, 
    BattleGroupDestroying, 

    // battle instance message types
    BattleInstanceCreating, 
    BattleInstanceGrowing, 
    BattleInstanceDestroying, 

    // projectile message types
    ProjectileCreating,
    ProjectileColliding, 
    ProjectileMishitting, 
    ProjectileHitting, 

    // audio manager types
    AudioManagerAudioClipLoading,
    AudioManagerAudioClipDistributing
}

/// <summary>
/// A system for comfortably logging message to the console and/or files and hiding unwanted messages either by type or due to the kind of its caller.
/// </summary>
public static class LogSystem
{
    private static List<ELogMessageType> disallowedMessageTypes = new List<ELogMessageType>();
    private static List<System.Type> disallowedCallerTypes = new List<System.Type>();
    //private static List<Object> allowedCallerObjects = new List<Object>();

    private static bool enableLogMessage = true;
    private static bool useCallerMethodPrefix = true;
    private static string callerMethodPrefixColor = "#ddddaa";

    static LogSystem()
    {
        DisallowMessageType(ELogMessageType.CharacterBattleControllerRegistering);
        //DisallowMessageType(ELogMessageType.CharacterBattleControllerUnregistering);
        DisallowMessageType(ELogMessageType.StructureBattleControllerRegistering);
        //DisallowMessageType(ELogMessageType.StructureBattleControllerUnregistering);
        DisallowMessageType(ELogMessageType.BattleGroupCreating);
        DisallowMessageType(ELogMessageType.BattleGroupRegistering);
        DisallowMessageType(ELogMessageType.BattleGroupUnregistering);

        //DisallowCallerType(typeof(Projectile));
        //DisallowCallerType(typeof(BattleManager));
    }

    /// <summary>
    /// Logs a message specified by format with placeholders and provided arguments (like string.format)
    /// </summary>
    /// <param name="format">The format that the log message should have (like string.format)</param>
    /// <param name="args">Optional enumeration of arguments that replace previously introduced placeholders in the format string</param>
    public static void Log(string format, params System.Object[] args)
    {
        if(!enableLogMessage)
            return;

        StackFrame stackFrame = new StackTrace().GetFrame(1);
        System.Type callerType = stackFrame.GetMethod().DeclaringType;

        if(!disallowedCallerTypes.Contains(callerType))
        {
            if(useCallerMethodPrefix)
            {
                string callerMethodPrefix = string.Format("<color={2}>{0}.{1}():</color> ", callerType.Name, stackFrame.GetMethod().Name, callerMethodPrefixColor);
                UnityEngine.Debug.Log(string.Format(callerMethodPrefix + format, args));
            }
            else
                UnityEngine.Debug.Log(string.Format(format, args));
        }
    }

    /// <summary>
    /// Logs a message specified by format with placeholders and provided arguments (like string.format)
    /// </summary>
    /// <param name="logtype">The type of log message that will be logged. Used for easy filtering of message types.</param>
    /// <param name="format">The format that the log message should have (like string.format)</param>
    /// <param name="args">Optional enumeration of arguments that replace previously introduced placeholders in the format string</param>
    public static void Log(ELogMessageType logType, string format, params System.Object[] args)
    {
        if(!enableLogMessage)
            return;

        StackFrame stackFrame = new StackTrace().GetFrame(1);
        System.Type callerType = stackFrame.GetMethod().DeclaringType;

        if(!disallowedMessageTypes.Contains(logType) && !disallowedCallerTypes.Contains(callerType))
        {
            if(useCallerMethodPrefix)
            {
                string callerMethodPrefix = string.Format("<color={2}>{0}.{1}():</color> ", callerType.Name, stackFrame.GetMethod().Name, callerMethodPrefixColor);
                UnityEngine.Debug.Log(string.Format(callerMethodPrefix + format, args));
            }
            else
                UnityEngine.Debug.Log(string.Format(format, args));
        }
    }

    /// <summary>
    /// Allows the specified log message type to be displayed in the console
    /// </summary>
    /// <param name="logType">The log message type to be allowed</param>
    public static void AllowMessageType(ELogMessageType logType)
    {
        if(disallowedMessageTypes.Contains(logType))
            disallowedMessageTypes.Remove(logType);
    }

    /// <summary>
    /// Disallows the specified log message type and hides it in the console
    /// </summary>
    /// <param name="logType">The log message type to be disallowed</param>
    public static void DisallowMessageType(ELogMessageType logType)
    {
        if(!disallowedMessageTypes.Contains(logType))
            disallowedMessageTypes.Add(logType);
    }

    /// <summary>
    /// Allows the specified log caller type such that messages from that type of class 
    /// will be displayed in the console.
    /// </summary>
    /// <param name="callerType">The log caller type to allow messages from</param>
    public static void AllowCallerType(System.Type callerType)
    {
        if(disallowedCallerTypes.Contains(callerType))
            disallowedCallerTypes.Remove(callerType);
    }

    /// <summary>
    /// Disallows the specified log caller type such that messages from that type of class 
    /// will be hidden from the console.
    /// </summary>
    /// <param name="callerType">The log caller type to disallow messages from</param>
    public static void DisallowCallerType(System.Type callerType)
    {
        if(!disallowedCallerTypes.Contains(callerType))
            disallowedCallerTypes.Add(callerType);
    }
}
