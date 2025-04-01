using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace ReOpenShock.Patches;

public static class PlayerPatches
{
    private static int _lastHealth;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.PlayerDeathDone))]
    public static void PlayerDeathDone_PostFix(PlayerAvatar __instance)
    {
        ReOpenShock.Instance.Logger.LogInfo($"{__instance.playerName} died");
        if (__instance.isLocal)
        {
            ReOpenShock.Instance.Logger.LogInfo("Shocking!");
            Task.Run(() =>
                ReOpenShock.Instance.ActionShockers(
                    ModConfig.ShockOnDeath.Value ? ControlType.Shock : ControlType.Vibrate,
                    ModConfig.ShockStrength.Value));
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
    public static void PlayerHurt_Prefix(PlayerHealth __instance)
    {
        if (!__instance.GetComponent<PlayerAvatar>().isLocal) return;
        if (ModConfig.ShockOnDamage.Value)
        {
            if (_lastHealth < 0)
                _lastHealth = __instance.health; // Initialize last health

            if (__instance.health < _lastHealth) // Health is decreasing
            {
                var intensity = (1 - __instance.health / (float)__instance.maxHealth) * 100;
                ReOpenShock.Instance.Logger.LogInfo(intensity);
                var shockValue = Mathf.RoundToInt(intensity);
                ReOpenShock.Instance.Logger.LogInfo(
                    $"Health decreased: {_lastHealth} -> {__instance.health}, sending shock {shockValue}");
                Task.Run(() => ReOpenShock.Instance.ActionShockers(ControlType.Shock, shockValue)); // Vibration task
            }

            _lastHealth = __instance.health;
        }
    }
}