using HarmonyLib;
using Mirror;
using MoreSettings.Runtime;
using UnityEngine;

namespace MoreSettings.Patches;

[HarmonyPatch(typeof(LocalSaveManager), nameof(LocalSaveManager.CreateNewSave))]
internal static class QuotaSaveInitializationPatches
{
    private static readonly AccessTools.FieldRef<SaveManager, SaveData> CurrentSaveDataRef =
        AccessTools.FieldRefAccess<SaveManager, SaveData>("currentSaveData");

    private static void Prefix()
    {
        if (!NetworkServer.active) return;
        TimingCoordinator.TryApplyFromResources("LocalSaveManager.CreateNewSave");
    }

    private static void Postfix()
    {
        if (!NetworkServer.active) return;

        var saveManager = Object.FindFirstObjectByType<SaveManager>();
        if (saveManager == null) return;

        var saveData = CurrentSaveDataRef(saveManager);
        if (saveData == null) return;

        TimingCoordinator.TryApplyInitialQuotaToSaveData(saveData, "LocalSaveManager.CreateNewSave");
    }
}

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.ResetCurrentSaveToDefaults))]
internal static class QuotaSaveResetPatches
{
    private static readonly AccessTools.FieldRef<SaveManager, SaveData> CurrentSaveDataRef =
        AccessTools.FieldRefAccess<SaveManager, SaveData>("currentSaveData");

    private static void Prefix()
    {
        if (!NetworkServer.active) return;
        TimingCoordinator.TryApplyFromResources("SaveManager.ResetCurrentSaveToDefaults");
    }

    private static void Postfix(SaveManager __instance)
    {
        if (!NetworkServer.active || __instance == null) return;

        var saveData = CurrentSaveDataRef(__instance);
        if (saveData == null) return;

        TimingCoordinator.TryApplyInitialQuotaToSaveData(saveData, "SaveManager.ResetCurrentSaveToDefaults");
    }
}
