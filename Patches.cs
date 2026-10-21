using HarmonyLib;

namespace AnarchyMode;

[HarmonyPatch(typeof(Player), nameof(Player.OnZeroHealthReached))]
internal static class PlayerDeathPatch
{
    private static bool Prefix(Player __instance)
    {
        var mod = AnarchyBehaviour.Instance;
        if (mod == null || !mod.Enabled)
        {
            return true;
        }

        mod.OnPlayerZeroHealth(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(Citizen), nameof(Citizen.RecieveDamage))]
internal static class CitizenDamagePatch
{
    private static void Postfix(Citizen __instance, Actor fromWho)
    {
        AnarchyBehaviour.Instance?.RecordDamage(__instance, fromWho);
    }
}

[HarmonyPatch(typeof(Human), nameof(Human.GenerateRoutineGoals))]
internal static class NoRoutineGoalsPatch
{
    private static bool Prefix()
    {
        var mod = AnarchyBehaviour.Instance;
        if (mod == null || !mod.Enabled)
        {
            return true;
        }

        return !(ModConfig.DisableRoutineGoals?.Value ?? false);
    }
}

[HarmonyPatch(typeof(StatusController), nameof(StatusController.Pursued))]
internal static class NoPursuedStatusPatch
{
    private static bool Prefix()
    {
        return !(AnarchyBehaviour.Instance?.Enabled ?? false);
    }
}

[HarmonyPatch(typeof(NewAIController), nameof(NewAIController.TriggerReactionIndicator))]
internal static class NoReactionIndicatorPatch
{
    private static bool Prefix()
    {
        return !(AnarchyBehaviour.Instance?.Enabled ?? false);
    }
}

[HarmonyPatch(typeof(MainMenuController), nameof(MainMenuController.SelectGenNewCity))]
internal static class CitySizePatch
{
    private static void Postfix()
    {
        AnarchyBehaviour.Instance?.ApplyCitySize();
    }
}

[HarmonyPatch(typeof(CityConstructor), nameof(CityConstructor.TriggerStartEvent))]
internal static class CityGenerationCompletePatch
{
    private static void Postfix()
    {
        AnarchyBehaviour.Instance?.OnCityGenerationComplete();
    }
}

[HarmonyPatch(typeof(NewAIController), nameof(NewAIController.SetKO))]
internal static class KillOnKnockoutPatch
{
    private static void Postfix(NewAIController __instance, bool val)
    {
        if (val)
        {
            AnarchyBehaviour.Instance?.KillKnockedOut(__instance.human);
        }
    }
}

[HarmonyPatch(typeof(NewAIController), nameof(NewAIController.SetStunned))]
internal static class KillOnStunPatch
{
    private static void Postfix(NewAIController __instance, bool val)
    {
        if (val)
        {
            AnarchyBehaviour.Instance?.KillKnockedOut(__instance.human);
        }
    }
}
[HarmonyPatch(typeof(GameplayController), "NewMurderCaseNotify")]
internal static class NoNewMurderCasePatch
{
    private static bool Prefix()
    {
        return !(AnarchyBehaviour.Instance?.Enabled ?? false);
    }
}

[HarmonyPatch(typeof(MurderController), nameof(MurderController.OnVictimDiscovery))]
internal static class NoVictimDiscoveryPatch
{
    private static bool Prefix()
    {
        return !(AnarchyBehaviour.Instance?.Enabled ?? false);
    }
}

[HarmonyPatch(typeof(MurderController), nameof(MurderController.AssignActiveCase))]
internal static class NoAssignActiveCasePatch
{
    private static bool Prefix()
    {
        return !(AnarchyBehaviour.Instance?.Enabled ?? false);
    }
}

[HarmonyPatch(typeof(CasePanelController), nameof(CasePanelController.SetActiveCase))]
internal static class NoSetActiveCasePatch
{
    private static bool Prefix()
    {
        return !(AnarchyBehaviour.Instance?.Enabled ?? false);
    }
}

[HarmonyPatch(typeof(InterfaceController), nameof(InterfaceController.NewMurderCaseDisplay))]
internal static class NoNewMurderCaseDisplayPatch
{
    private static bool Prefix()
    {
        return !(AnarchyBehaviour.Instance?.Enabled ?? false);
    }
}

[HarmonyPatch(typeof(NewGameLocation), nameof(NewGameLocation.SetAsCrimeScene))]
internal static class NoCrimeScenePatch
{
    private static bool Prefix(bool val)
    {
        return !(val && (AnarchyBehaviour.Instance?.Enabled ?? false));
    }
}

[HarmonyPatch(typeof(Toolbox), nameof(Toolbox.Shoot))]
internal static class NoBrassSpawnPatch
{
    private static void Prefix(ref bool ejectBrass, ref bool forcePhysicsEjectBrass)
    {
        if (AnarchyBehaviour.Instance?.Enabled ?? false)
        {
            ejectBrass = false;
            forcePhysicsEjectBrass = false;
        }
    }
}