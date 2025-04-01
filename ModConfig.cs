using BepInEx.Configuration;
using Sirenix.Serialization.Utilities;

namespace ReOpenShock;

public static class ModConfig
{
    public static ConfigEntry<string> OpenShockAPIDomain;
    public static ConfigEntry<string> OpenShockAPIKey;


    public static ConfigEntry<bool> DeveloperMode;
    
    public static ConfigEntry<int> ShockStrength;
    public static ConfigEntry<float> ShockDuration;

    public static ConfigEntry<bool> ShockOnDeath;
    public static ConfigEntry<bool> ShockOnDamage;
    public static ConfigEntry<bool> ShockOnItemBreak;

    private static readonly ImmutableList<ConfigEntry<string>> RegenOsClientOnChange =
        new([OpenShockAPIDomain, OpenShockAPIKey]);

    public static void Init(ConfigFile config)
    {
        OpenShockAPIDomain = config.Bind(
            ConfigGroups.Connection,
            "OpenShock API",
            "https://api.openshock.app",
            "The OpenShock instance's API");
        OpenShockAPIKey = config.Bind(
            ConfigGroups.Connection,
            "OpenShock API Key",
            "GO TO YOUR CONFIG FILE TO EDIT", "Go to your config file to edit!");

        DeveloperMode = config.Bind(
            ConfigGroups.Misc,
            "Developer Mode",
            false,
            new ConfigDescription("Beeps Only", null, "HideFromREPOConfig")
        );

        ShockStrength = config.Bind(
            ConfigGroups.General,
            "Shock Strength",
            20,
            new ConfigDescription("", new AcceptableValueRange<int>(0, 100)));
        ShockDuration = config.Bind(
            ConfigGroups.General,
            "Shock Duration",
            0.3f,
            new ConfigDescription("", new AcceptableValueRange<float>(0.3f, 30)));

        ShockOnDeath = config.Bind(
            ConfigGroups.ShockEvents,
            "Shock On Death",
            true,
            "Do you want to be shocked on death?");
        ShockOnDamage = config.Bind(
            ConfigGroups.ShockEvents,
            "Shock On Damage",
            true,
            "Shock on damage proportionate to how low you are");
        ShockOnItemBreak = config.Bind(
            ConfigGroups.ShockEvents,
            "Shock On Item Break",
            true,
            "Shocks you when an item breaks, proportionate to how much health the item had");

        // foreach (var configEntry in RegenOsClientOnChange)
        //     configEntry.SettingChanged += async (_, _) => { await ReOpenShock.Instance.MakeClient(); };
    }

    private static class ConfigGroups
    {
        public static readonly string Connection = "Connection";
        public static readonly string General = "General";
        public static readonly string ShockEvents = "Shock Events";
        public static readonly string Misc = "Misc";
    }
}