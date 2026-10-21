using System;
using BepInEx.Configuration;

namespace AnarchyMode;

internal static class ModConfig
{
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<int> StreetWeaponCount;
    internal static ConfigEntry<bool> ArmNpcs;
    internal static ConfigEntry<float> ArmChance;
    internal static ConfigEntry<int> MaxNpcsToArm;
    internal static ConfigEntry<float> AggressionInterval;
    internal static ConfigEntry<float> ScanRadius;
    internal static ConfigEntry<float> AttackRadius;
    internal static ConfigEntry<int> MaxAttackersPerTick;
    internal static ConfigEntry<float> BraveryMin;
    internal static ConfigEntry<float> BraveryMax;
    internal static ConfigEntry<float> DoorUnlockDelay;
    internal static ConfigEntry<bool> ShowHud;
    internal static ConfigEntry<float> KillFeedSeconds;
    internal static ConfigEntry<bool> DisableRoutineGoals;
    internal static ConfigEntry<bool> AutoStartTestCity;
    internal static ConfigEntry<int> CityWidth;
    internal static ConfigEntry<int> CityHeight;
    internal static ConfigEntry<string> RespawnKey;
    internal static ConfigEntry<string> RespawnHereKey;
    internal static ConfigEntry<string> HealKey;
    internal static ConfigEntry<string> FreeCamKey;
    internal static ConfigEntry<string> WallHackKey;
    internal static ConfigEntry<string> WeaponMenuKey;

    internal static void Init(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true, "Turn the anarchy simulation on or off.");
        ShowHud = config.Bind("General", "ShowHud", true, "Show the anarchy HUD.");
        KillFeedSeconds = config.Bind("General", "KillFeedSeconds", 12f, "How long kill notifications stay on screen.");
        DisableRoutineGoals = config.Bind("General", "DisableRoutineGoals", false, "Stop NPCs from generating normal work routines.");
        AutoStartTestCity = config.Bind("Debug", "AutoStartTestCity", false, "Generate a new city automatically after the main menu loads. For testing only.");
        CityWidth = config.Bind("City", "Width", 2, "City width in blocks. 2 means a 2x2 city. Requires DensityMod for sizes below vanilla limits.");
        CityHeight = config.Bind("City", "Height", 2, "City height in blocks. 2 means a 2x2 city. Requires DensityMod for sizes below vanilla limits.");
        RespawnKey = config.Bind("Respawn", "RespawnKey", "F5", "Key to respawn on the surface.");
        RespawnHereKey = config.Bind("Respawn", "RespawnHereKey", "F", "Key to respawn at the spectator camera position.");
        HealKey = config.Bind("Controls", "HealKey", "H", "Key to fully heal the player.");
        FreeCamKey = config.Bind("Controls", "FreeCamKey", "V", "Key to toggle free camera.");
        WallHackKey = config.Bind("Controls", "WallHackKey", "F7", "Key to toggle NPC wallhack markers.");
        WeaponMenuKey = config.Bind("Controls", "WeaponMenuKey", "B", "Key to open the weapon selection menu.");

        if (string.Equals(WallHackKey.Value, "X", StringComparison.OrdinalIgnoreCase))
        {
            WallHackKey.Value = "F7";
        }

        StreetWeaponCount = config.Bind("Weapons", "StreetWeaponCount", 300, "How many weapons to scatter on the streets when a city loads.");
        ArmNpcs = config.Bind("Weapons", "ArmNpcs", true, "Try to give every NPC a weapon when the city loads.");
        ArmChance = config.Bind("Weapons", "ArmChance", 1f, "Chance for an NPC to be armed (0..1).");
        MaxNpcsToArm = config.Bind("Weapons", "MaxNpcsToArm", 2000, "Safety cap for how many NPCs to arm at city load.");

        AggressionInterval = config.Bind("Combat", "AggressionInterval", 0.5f, "Seconds between aggression ticks.");
        ScanRadius = config.Bind("Combat", "ScanRadius", 120f, "Only NPCs within this distance of the player are managed.");
        AttackRadius = config.Bind("Combat", "AttackRadius", 55f, "How far an NPC will look for a victim.");
        MaxAttackersPerTick = config.Bind("Combat", "MaxAttackersPerTick", 30, "Maximum number of new attacks started per tick.");
        BraveryMin = config.Bind("Combat", "BraveryMin", 0.15f, "Minimum deterministic bravery.");
        BraveryMax = config.Bind("Combat", "BraveryMax", 0.95f, "Maximum deterministic bravery.");
        DoorUnlockDelay = config.Bind("Combat", "DoorUnlockDelay", 1.5f, "Delay before an NPC forces a locked door.");
    }
}
