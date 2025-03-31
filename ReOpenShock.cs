using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ReOpenShock
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class ReOpenShock : BaseUnityPlugin
    {
        public const string PluginGuid = "gay.lilyy.ReOpenShock";
        public const string PluginName = "ReOpenShock";
        public const string PluginVersion = "1.0.1";

        private static new ManualLogSource Logger;

        private static ConfigEntry<string> domain;
        private static ConfigEntry<string> apiKey;
        private static ConfigEntry<bool> shockOnDeath;
        private static ConfigEntry<byte> shockStrength;
        private static ConfigEntry<bool> vibrateOnDamage;

        private static OpenShockApi client;

        private async Task MakeClient()
        {
            client = new OpenShockApi(apiKey.Value, new Uri(domain.Value));
            await client.GetDevices();
        }

        public async void Awake()
        {
            domain = Config.Bind(
                "Connection",
                "OpenShock API",
                "https://api.openshock.app",
                "The OpenShock instance's API");
            apiKey = Config.Bind(
                "Connection",
                "OpenShock API Key",
                "GO TO YOUR CONFIG FILE TO EDIT", "Go to your config file to edit!");

            shockOnDeath = Config.Bind(
                "General",
                "Shock On Death",
                true,
                "Do you want to be shocked on death?");
            shockStrength = Config.Bind<byte>(
                "General",
                "Shock Strength",
                20,
                new ConfigDescription("", new AcceptableValueRange<byte>(0, 100)));
            vibrateOnDamage = Config.Bind(
                "General",
                "Vibrate On Damage",
                true,
                "Vibrate on damage proportionate to how low you are");
            Logger = base.Logger;
            new Harmony(PluginGuid).PatchAll();
            Logger.LogInfo("ReOpenShock patched!");
            await MakeClient();
            apiKey.SettingChanged += async (_, _) =>
            {
                await MakeClient();
            };
            domain.SettingChanged += async (_, _) =>
            {
                await MakeClient();
            };
        }

        static async void ActionShockers(ControlType action, int strength)
        {
            List<Control> actions = new();
            foreach (var device in client.devices)
            {
                actions.Add(new Control
                {
                    Id = device,
                    Duration = 300,
                    Intensity = shockStrength.Value,
                    Type = action
                });
            }

            await client.Control(actions);
        }


        [HarmonyPatch(typeof(PlayerAvatar), "PlayerDeathDone")]
        public static class DeathPatch
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerAvatar __instance)
            {
                Logger.LogInfo($"{__instance.playerName} died");
                if (__instance.isLocal)
                {
                    Logger.LogInfo("Shocking!");
                    Task.Run(() => ActionShockers(shockOnDeath.Value ? ControlType.Shock : ControlType.Vibrate, shockStrength.Value));
                }
            }

            
        }
        [HarmonyPatch(typeof(PlayerHealth), "Hurt")]
        public static class HealthPatch
        {
            private static float _lastHealth = -1;
        
            [HarmonyPrefix]
            public static void PostFix(PlayerHealth __instance)
            {
                if (vibrateOnDamage.Value)
                {
                    if (_lastHealth < 0)
                        _lastHealth = __instance.health; // Initialize last health

                    if (__instance.health < _lastHealth) // Health is decreasing
                    {
                        float intensity = (1 - (__instance.health / (float)__instance.maxHealth)) * 100;
                        Logger.LogInfo(intensity);
                        int vibrateValue = Mathf.RoundToInt(intensity);
                        Logger.LogInfo(
                            $"Health decreased: {_lastHealth} -> {__instance.health}, sending vibrate {vibrateValue}");
                        Task.Run(() => ActionShockers(ControlType.Vibrate, vibrateValue)); // Vibration task
                    }

                    _lastHealth = __instance.health;
                }
            }
        }

    }
}