// Based on code from MxPuffin
// Original repository: https://github.com/MxPuffin/REPOShock
// Licensed under the MIT License


using HarmonyLib;
using UnityEngine;

namespace ReOpenShock.Patches;

public static class ValuablePatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PhysGrabObject), nameof(PhysGrabObject.GrabEnded))]
    private static void GrabEndedPostFix(PhysGrabObject __instance)
    {
        if (!__instance.isValuable)
            return;
        if (!__instance.heldByLocalPlayer)
            return;

        ModGlobals.LastHeldItemPickupTime = Time.time;
        ModGlobals.LastOffensiveGracePeriodTime = 0;

        if (!ModGlobals.RecentlyHeldObjects.ContainsKey(__instance.gameObject))
        {
            ModGlobals.RecentlyHeldObjects.Add(__instance.gameObject, Time.time);
            ReOpenShock.Instance.Logger.LogInfo($"Added {__instance.gameObject.name} to recently held");
        }
        else
        {
            ModGlobals.RecentlyHeldObjects[__instance.gameObject] = Time.time;
            ReOpenShock.Instance.Logger.LogInfo($"Updated {__instance.gameObject.name} in recently held");
        }
    }

    [HarmonyPatch(typeof(PhysGrabObjectImpactDetector))]
    public static class PhysGrabObjectImpactDetectorPatch
    {
        private static bool _lastHitEnemy;

        [HarmonyPrefix]
        [HarmonyPatch(nameof(PhysGrabObjectImpactDetector.OnCollisionStay))]
        private static void OnCollisionStay(ref Collision collision)
        {
            // really dodgy and maybe inefficient way of preventing
            // 'intentional' enemy hits from shocking

            // TODO
            // Create a custom class to store in recently held object
            // To keep track of

            if (collision.transform.CompareTag("Enemy"))
                _lastHitEnemy = true;
            else
                _lastHitEnemy = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(PhysGrabObjectImpactDetector.BreakRPC))]
        private static void ItemImpactBreakRPC(float valueLost, ref bool _loseValue, int breakLevel,
            PhysGrabObjectImpactDetector __instance)
        {
            // ReOpenShock.Instance.Logger.LogInfo($"iibr {valueLost}, {_loseValue}, {breakLevel}");
            if (!ModConfig.ShockOnItemBreak.Value)
                return;

            if (!_loseValue) return;

            ItemBreakPostfix(valueLost, __instance);
        }
        private static void ItemBreakPostfix(float valueLost, PhysGrabObjectImpactDetector __instance)
        {
            if (!ModGlobals.IsAlive)
                return;
            if (ModGlobals.CurrentLevel == "Arena")
                return;
            if (!__instance.isValuable)
                return;


            var isHeldByLocalPlayer = __instance.physGrabObject.heldByLocalPlayer;

            if (isHeldByLocalPlayer)
            {
                if (!ModGlobals.RecentlyHeldObjects.ContainsKey(__instance.gameObject))
                {
                    ModGlobals.RecentlyHeldObjects.Add(__instance.gameObject, Time.time);
                    ReOpenShock.Instance.Logger.LogInfo($"Object {__instance.gameObject.name} added to held objects.");
                }
                else
                {
                    ModGlobals.RecentlyHeldObjects[__instance.gameObject] = Time.time;
                }
            }

            var originalValue = __instance.valuableObject.dollarValueOriginal;

            ShockPlayerIfNecessary(__instance.gameObject, valueLost, originalValue, isHeldByLocalPlayer);
        }

        private static void ShockPlayerIfNecessary(GameObject damagedObject, float valueLost, float originalValue,
            bool isHeldByLocalPlayer)
        {
            if (!ModGlobals.RecentlyHeldObjects.ContainsKey(damagedObject))
                return;

            var lastHeldTime = ModGlobals.RecentlyHeldObjects[damagedObject];

            if (isHeldByLocalPlayer && _lastHitEnemy)
            {
                ModGlobals.LastOffensiveGracePeriodTime = Time.time;
                ModGlobals.LastOffensiveObject = damagedObject;
                ReOpenShock.Instance.Logger.LogInfo("[Damage Item Event] Enemy hit, aborting shock");
                return;
            }

            if (_lastHitEnemy && Time.time - lastHeldTime <= 2)
            {
                ModGlobals.LastOffensiveGracePeriodTime = Time.time;
                ModGlobals.LastOffensiveObject = damagedObject;
                ReOpenShock.Instance.Logger.LogInfo(
                    "[Damage Item Event] Enemy hit in thrown grace period, aborting shock");
                return;
            }

            if (ModGlobals.LastOffensiveObject == damagedObject
                && Time.time - ModGlobals.LastOffensiveGracePeriodTime <= 3)
            {
                ReOpenShock.Instance.Logger.LogInfo(
                    "[Damage Item Event] Recently used 'weapon' took damage, aborting shock");
                return;
            }

            if (Time.time - lastHeldTime <= 4) ShockPlayer(valueLost, originalValue, damagedObject.name);
        }

        private static void ShockPlayer(float valueLost, float originalValue, string objName)
        {
            var intensity = MapValue(valueLost, 0, originalValue, 0, ModConfig.ShockStrength.Value);


            ReOpenShock.Instance.Logger.LogInfo(
                $"[Damage Item Event] Played damaged {objName} for {valueLost} - Shocking for {intensity}%");
            ReOpenShock.Instance.ActionShockers(ControlType.Shock, intensity);
        }

        private static int MapValue(float value, float start1, float stop1, float start2, float stop2)
        {
            return (int)(start1 + (value - start1) * (stop2 - start2) / (stop1 - start1));
        }
    }
}