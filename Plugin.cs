using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace AnarchyMode;

[BepInPlugin(Guid, "Anarchy Mode", "0.1.1")]
public class Plugin : BasePlugin
{
    public const string Guid = "com.avs.anarchymode";

    public static ManualLogSource Logger;

    public override void Load()
    {
        Logger = Log;
        Log.LogInfo("Anarchy Mode: loading");

        ModConfig.Init(Config);

        ClassInjector.RegisterTypeInIl2Cpp<AnarchyBehaviour>();

        var go = new GameObject("AnarchyMode");
        go.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(go);
        go.AddComponent<AnarchyBehaviour>();

        var harmony = new Harmony(Guid);
        harmony.PatchAll();

        Log.LogInfo("Anarchy Mode: loaded");
    }
}

