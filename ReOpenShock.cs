using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ReOpenShock.Patches;

namespace ReOpenShock;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class ReOpenShock : BaseUnityPlugin
{
    public const string PluginGuid = "gay.lilyy.ReOpenShock";
    public const string PluginName = "ReOpenShock";
    public const string PluginVersion = "1.0.4";
    public static ReOpenShock Instance;


    private Harmony _harmony;

    internal OpenShockApi client;

    public new ManualLogSource Logger;

    public async void Awake()
    {
        Instance = this;

        Logger = base.Logger;
        ModConfig.Init(Config);
        Patch();
        Logger.LogInfo("ReOpenShock patched!");
        await MakeClient();
    }

    public async Task MakeClient()
    {
        client = new OpenShockApi(ModConfig.OpenShockAPIKey.Value, new Uri(ModConfig.OpenShockAPIDomain.Value));
        await client.GetDevices();
    }

    private void Patch()
    {
        _harmony ??= new Harmony(PluginGuid);
        _harmony.PatchAll(typeof(PlayerPatches));
        _harmony.PatchAll(typeof(ValuablePatches));
        _harmony.PatchAll(typeof(ValuablePatches.PhysGrabObjectImpactDetectorPatch));
    }

    public async void ActionShockers(ControlType action, int strength)
    {
        List<Control> actions = new();
        foreach (var device in Instance.client.devices)
            actions.Add(new Control
            {
                Id = device,
                Duration = (int)(ModConfig.ShockDuration.Value * 1000),
                Intensity = strength,
                Type = ModConfig.DeveloperMode.Value ? ControlType.Sound : action
            });
        Logger.LogInfo($"Actioning shockers at mode {action} at {strength}% for {ModConfig.ShockDuration}s");
        await Instance.client.Control(actions);
    }
}