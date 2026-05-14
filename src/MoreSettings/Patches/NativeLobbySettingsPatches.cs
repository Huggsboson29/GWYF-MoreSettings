using HarmonyLib;
using MoreSettings.Runtime;
using UnityEngine;

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

[HarmonyPatch]
internal static class NativeLobbySliderPresentationPatches
{
    [HarmonyPrefix, HarmonyPatch(typeof(SettingsLayoutRuntimeUI), "CreateSliderEntry")]
    private static void CreateSliderEntry_Prefix(RectTransform parent, out int __state)
    {
        __state = parent != null ? parent.childCount : 0;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(SettingsLayoutRuntimeUI), "CreateSliderEntry")]
    private static void CreateSliderEntry_Postfix(RectTransform parent, SliderSettingItem entry, int __state)
    {
        if (parent == null || entry == null || !NativeLobbySettingsMenu.IsManagedSliderKey(entry.key))
        {
            return;
        }

        if (parent.childCount <= __state)
        {
            return;
        }

        var createdRoot = parent.GetChild(parent.childCount - 1);
        var contentRoot = createdRoot.childCount > 0 ? createdRoot.GetChild(0) : createdRoot;
        NativeLobbySettingsMenu.RegisterSliderRuntimeBinding(entry.key, contentRoot);
    }
}