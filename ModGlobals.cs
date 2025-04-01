// Based on code from MxPuffin
// Original repository: https://github.com/MxPuffin/REPOShock
// Licensed under the MIT License


using System.Collections.Generic;
using UnityEngine;

namespace ReOpenShock;

#nullable enable
internal static class ModGlobals
{
    private static float _lastLevelMessage = Time.time;


    internal static GameObject? LastOffensiveObject = null;
    internal static float LastOffensiveGracePeriodTime = 0;

    internal static readonly Dictionary<GameObject, float> RecentlyHeldObjects = new();
    internal static bool IsAlive { get; set; } = true;
    internal static string CurrentLevel { get; set; } = "";
    internal static bool IsSafeLevel { get; set; } = true;
    internal static float LastHeldItemPickupTime { get; set; } = Time.time;


    internal static void UpdateLevel(string level)
    {
        CurrentLevel = level;
        EvaluateIsSafeLevel();

        if (_lastLevelMessage < 3) return;

        ReOpenShock.Instance.Logger.LogInfo($"Updating Level to {level} - Resetting Mod Global Variables");
        _lastLevelMessage = Time.time;
    }

    private static void EvaluateIsSafeLevel()
    {
        var arena = false;
        // Prevent the forced arena death from injuring player in single player
        if (!SemiFunc.IsMultiplayer()) arena = CurrentLevel == "Arena";
        IsSafeLevel = CurrentLevel == "Shop" || CurrentLevel == "Lobby" || CurrentLevel == "Lobby Menu" ||
                      CurrentLevel == "" || arena;
    }

    internal static void Revive()
    {
        IsAlive = true;
        ReOpenShock.Instance.Logger.LogInfo("Player has been revived.");
    }
}