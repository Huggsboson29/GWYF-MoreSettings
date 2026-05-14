using HarmonyLib;
using MoreSettings.Runtime;

namespace MoreSettings.Patches;

[HarmonyPatch]
internal static class NativeLobbySettingsRuntimeUIPatches
{
    private static readonly AccessTools.FieldRef<SettingsLayoutRuntimeUI, SettingsLayout> LayoutRef =
        AccessTools.FieldRefAccess<SettingsLayoutRuntimeUI, SettingsLayout>("layout");

    [HarmonyPrefix, HarmonyPatch(typeof(SettingsLayoutRuntimeUI), "Awake")]
    private static void SettingsLayoutRuntimeUI_Awake_Prefix(SettingsLayoutRuntimeUI __instance)
    {
        if (__instance == null) return;

        var layout = LayoutRef(__instance);
        if (layout == null) return;

        NativeLobbySettingsMenu.EnsureInjected(layout, "SettingsLayoutRuntimeUI.Awake");
    }
}

[HarmonyPatch]
internal static class NativeLobbySettingsButtonPatches
{
    private static readonly AccessTools.FieldRef<LobbyModeDropdownButton, SettingsLayout> LayoutRef =
        AccessTools.FieldRefAccess<LobbyModeDropdownButton, SettingsLayout>("settingsLayout");

    [HarmonyPostfix, HarmonyPatch(typeof(LobbyModeDropdownButton), "Awake")]
    private static void LobbyModeDropdownButton_Awake_Postfix(LobbyModeDropdownButton __instance)
    {
        if (__instance == null) return;

        var layout = LayoutRef(__instance);
        if (layout == null) return;

        NativeLobbySettingsMenu.RegisterLobbyLayout(layout, "LobbyModeDropdownButton.Awake");
        NativeLobbySettingsMenu.EnsureInjected(layout, "LobbyModeDropdownButton.Awake");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(LobbyModeDropdownButton), "OnClick")]
    private static void LobbyModeDropdownButton_OnClick_Prefix(LobbyModeDropdownButton __instance)
    {
        if (__instance == null) return;

        var layout = LayoutRef(__instance);
        if (layout == null) return;

        NativeLobbySettingsMenu.RefreshFromLobbyButton(layout);
    }
}

[HarmonyPatch(typeof(SettingItemBase), nameof(SettingItemBase.NotifyChanged))]
internal static class NativeLobbySettingChangePatches
{
    private static void Postfix(SettingItemBase __instance)
    {
        NativeLobbySettingsMenu.HandleSettingChanged(__instance);
    }
}