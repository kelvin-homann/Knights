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

    // battle manager message types
    BattleManagerControllerRegistering,
    BattleManagerControllerUnregistering,
    BattleManagerGroupRegistering, 
    BattleManagerGroupUnregistering, 

    // battle controller message types
    BattleControllerCreating, 
    BattleControllerAttackExecuting, 
    BattleControllerAttackReceiving, 
    BattleControllerDamaging, 
    BattleControllerDestroying, 
    BattleControllerTargetAssigning, 
    BattleControllerTargetChoosing, 

    // battle group message types
    BattleGroupCreating, 
    BattleGroupReinforcing, 
    BattleGroupWeakening, 
    BattleGroupMerging, 
    BattleGroupDestroying, 

    // battle instance message types
    BattleInstanceCreating, 
    BattleInstanceGrowing, 
    BattleInstanceDestroying, 

    // projectile message types
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
    private static string callerMethodPrefixColor = "#000000";

    static LogSystem()
    {
        //DisallowMessageType(ELogMessageType.ProjectileCollided);
        //DisallowMessageType(ELogMessageType.ProjectileMishit);
        DisallowMessageType(ELogMessageType.BattleManagerControllerRegistering);
        DisallowMessageType(ELogMessageType.BattleManagerControllerUnregistering);
        DisallowMessageType(ELogMessageType.BattleGroupCreating);
        DisallowMessageType(ELogMessageType.BattleManagerGroupRegistering);

        DisallowCallerType(typeof(Projectile));
        //DisallowCallerType(typeof(BattleManager));
    }

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

    public static void AllowMessageType(ELogMessageType logType)
    {
        if(disallowedMessageTypes.Contains(logType))
            disallowedMessageTypes.Remove(logType);
    }

    public static void DisallowMessageType(ELogMessageType logType)
    {
        if(!disallowedMessageTypes.Contains(logType))
            disallowedMessageTypes.Add(logType);
    }

    public static void AllowCallerType(System.Type callerType)
    {
        if(disallowedCallerTypes.Contains(callerType))
            disallowedCallerTypes.Remove(callerType);
    }

    public static void DisallowCallerType(System.Type callerType)
    {
        if(!disallowedCallerTypes.Contains(callerType))
            disallowedCallerTypes.Add(callerType);
    }
}
