using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using MoreSettings.Configuration;
using MoreSettings.Models;
using MoreSettings.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MoreSettings.Runtime;

public static class NativeLobbySettingsMenu
{
    private const int MaxEditableQuotaMultipliers = 6;
    private const float DefaultCurrencySliderMax = 100000f;
    private const float CurrencySliderStep = 5000f;
    private const float SliderValueFieldWidth = 120f;
    private static readonly List<string> QuotaScalingModeOptions = new() { "Vanilla scaling", "Custom pattern" };
    private static readonly List<SliderRuntimeBinding> SliderRuntimeBindings = new();

    public const string SectionKey = "moresettings.section";
    public const string TimeSectionKey = "moresettings.time.section";
    public const string QuotaSectionKey = "moresettings.quota.section";
    public const string DayDurationMinutesKey = "moresettings.day-duration-minutes";
    public const string StartingMoneyKey = "moresettings.starting-money";
    public const string StartingQuotaKey = "moresettings.starting-quota";
    public const string CatchUpFactorKey = "moresettings.catch-up-factor";
    public const string QuotaScalingModeKey = "moresettings.quota-scaling-mode";
    public const string QuotaPatternLengthKey = "moresettings.quota-pattern-length";

    private static SettingsLayout? _lobbySettingsLayout;
    private static SettingsLayout? _runtimeLobbySettingsLayout;
    private static bool _isSynchronizing;

    public static void RegisterLobbyLayout(SettingsLayout layout, string source)
    {
        if (layout == null)
        {
            return;
        }

        _lobbySettingsLayout = layout;
        PluginMain.Log.LogDebug($"[NativeLobbySettingsMenu] Registered lobby settings layout from {source}.");
    }

    public static void RegisterRuntimeLobbyLayout(SettingsLayout layout, string source)
    {
        if (layout == null)
        {
            return;
        }

        _runtimeLobbySettingsLayout = layout;
        PluginMain.Log.LogDebug($"[NativeLobbySettingsMenu] Registered runtime lobby settings layout from {source}.");
    }

    public static bool EnsureInjected(SettingsLayout layout, string source)
    {
        if (layout == null)
        {
            return false;
        }

        if (!IsLobbySettingsLayout(layout))
        {
            return false;
        }

        if (layout.tabs == null || layout.tabs.Count == 0)
        {
            PluginMain.Log.LogDebug($"[NativeLobbySettingsMenu] Skipped injection from {source} because the layout had no tabs.");
            return false;
        }

        var tab = FindTargetTab(layout);
        if (tab == null)
        {
            PluginMain.Log.LogDebug($"[NativeLobbySettingsMenu] Skipped injection from {source} because no lobby settings tab was found.");
            return false;
        }

        tab.entries ??= new List<SettingItemBase>();

        var addedEntries = 0;
        addedEntries += EnsureTitleEntry(tab.entries, SectionKey, "MoreSettings");
        addedEntries += EnsureTitleEntry(tab.entries, TimeSectionKey, "Time");
        addedEntries += EnsureSliderEntry(
            tab.entries,
            DayDurationMinutesKey,
            "Day duration (minutes)",
            1f,
            1440f,
            wholeNumbers: true);
        addedEntries += EnsureTitleEntry(tab.entries, QuotaSectionKey, "Quota");
        addedEntries += EnsureSliderEntry(
            tab.entries,
            StartingMoneyKey,
            "Starting money",
            0f,
            100000f,
            wholeNumbers: true);
        addedEntries += EnsureSliderEntry(
            tab.entries,
            StartingQuotaKey,
            "Starting quota",
            0f,
            100000f,
            wholeNumbers: true);
        addedEntries += EnsureSliderEntry(
            tab.entries,
            CatchUpFactorKey,
            "Catch-up factor",
            0f,
            5f,
            wholeNumbers: false);
        addedEntries += EnsureDropdownEntry(
            tab.entries,
            QuotaScalingModeKey,
            "Quota scaling",
            QuotaScalingModeOptions,
            defaultIndex: (int)QuotaScalingMode.Vanilla);
        addedEntries += EnsureSliderEntry(
            tab.entries,
            QuotaPatternLengthKey,
            "Custom pattern length",
            1f,
            MaxEditableQuotaMultipliers,
            wholeNumbers: true);

        for (var index = 0; index < MaxEditableQuotaMultipliers; index++)
        {
            addedEntries += EnsureSliderEntry(
                tab.entries,
                GetQuotaMultiplierKey(index),
                $"Pattern multiplier {index + 1}",
                TimingProfileValidator.MinQuotaMultiplier,
                TimingProfileValidator.MaxQuotaMultiplier,
                wholeNumbers: false);
        }

        SyncFromCurrentState();

        PluginMain.Log.LogInfo(
            $"[NativeLobbySettingsMenu] {(addedEntries > 0 ? "Injected" : "Reused")} native lobby settings entries from {source}. Tab='{tab.tabName}', added={addedEntries}.");

        return true;
    }

    public static void RefreshFromLobbyButton(SettingsLayout layout)
    {
        EnsureInjected(layout, "LobbyModeDropdownButton.OnClick");
        SyncFromCurrentState();
        RefreshRuntimeSliderBindings();
    }

    public static void HandleSettingChanged(SettingItemBase entry)
    {
        var activeLayout = GetActiveLobbySettingsLayout();
        if (_isSynchronizing || entry == null || !IsMoreSettingsKey(entry.key) || activeLayout == null)
        {
            return;
        }

        if (string.Equals(entry.key, StartingMoneyKey, StringComparison.OrdinalIgnoreCase) && !IsStartingMoneyEditable())
        {
            SyncFromCurrentState();
            return;
        }

        if (!TryGetCurrentProfileForEditing(out var currentProfile) || currentProfile == null)
        {
            return;
        }

        if (!TryGetEditableEntries(
            activeLayout,
                out var dayDurationEntry,
                out var startingMoneyEntry,
                out var startingQuotaEntry,
                out var catchUpFactorEntry,
                out var quotaScalingModeEntry,
                out var quotaPatternLengthEntry,
                out var quotaMultiplierEntries))
        {
            return;
        }

        var quotaScalingMode = ResolveQuotaScalingMode(quotaScalingModeEntry!);
        var customPattern = QuotaPatternEditor.BuildPattern(
            quotaMultiplierEntries!
                .Select(multiplierEntry => multiplierEntry.value)
                .ToArray(),
            Mathf.RoundToInt(quotaPatternLengthEntry!.value));

        var startingMoney = (long)Mathf.Round(startingMoneyEntry!.value);
        var startingQuota = EnsureLinkedStartingQuota(
            startingMoney,
            (long)Mathf.Round(startingQuotaEntry!.value));

        var updatedProfile = new TimingProfile(
            "ActiveConfig",
            false,
            dayDurationEntry!.value * 60f,
            currentProfile.DaysBeforeQuota,
            startingQuota,
            startingMoney,
            catchUpFactorEntry!.value,
            quotaScalingMode,
            quotaScalingMode == QuotaScalingMode.CustomPattern ? customPattern : currentProfile.QuotaMultipliers);

        if (!TimingCoordinator.TryApplyManualOverrides(
                currentProfile,
                updatedProfile,
                "NativeLobbySettingsMenu.NotifyChanged",
                out _))
        {
            SyncFromCurrentState();
            return;
        }

        LobbyVisibility.BroadcastCurrentState();
        SyncFromCurrentState();
    }

    private static void SyncFromCurrentState()
    {
        var activeLayout = GetActiveLobbySettingsLayout();
        if (activeLayout == null)
        {
            return;
        }

        if (!TryGetCurrentProfileForEditing(out var profile) || profile == null)
        {
            return;
        }

        if (!TryGetEditableEntries(
                activeLayout,
                out var dayDurationEntry,
                out var startingMoneyEntry,
                out var startingQuotaEntry,
                out var catchUpFactorEntry,
                out var quotaScalingModeEntry,
                out var quotaPatternLengthEntry,
                out var quotaMultiplierEntries))
        {
            return;
        }

        var editablePattern = QuotaPatternEditor.BuildEditableValues(profile.QuotaMultipliers, MaxEditableQuotaMultipliers);

        _isSynchronizing = true;
        try
        {
            dayDurationEntry!.value = Mathf.Clamp(profile.DayDurationSeconds / 60f, dayDurationEntry.min, dayDurationEntry.max);
            dayDurationEntry.defaultValue = dayDurationEntry.value;

            var linkedStartingMoney = profile.StartingMoney;
            var linkedStartingQuota = EnsureLinkedStartingQuota(profile.StartingMoney, profile.StartingQuota);

            ApplyCurrencyBounds(startingMoneyEntry!, startingQuotaEntry!, linkedStartingMoney, linkedStartingQuota);

            startingMoneyEntry!.value = Mathf.Clamp((float)linkedStartingMoney, startingMoneyEntry.min, startingMoneyEntry.max);
            startingMoneyEntry.defaultValue = startingMoneyEntry.value;

            startingQuotaEntry!.value = Mathf.Clamp((float)linkedStartingQuota, startingQuotaEntry.min, startingQuotaEntry.max);
            startingQuotaEntry.defaultValue = startingQuotaEntry.value;

            catchUpFactorEntry!.value = Mathf.Clamp(profile.CatchUpFactor, catchUpFactorEntry.min, catchUpFactorEntry.max);
            catchUpFactorEntry.defaultValue = catchUpFactorEntry.value;

            quotaScalingModeEntry!.index = Mathf.Clamp((int)profile.QuotaScalingMode, 0, QuotaScalingModeOptions.Count - 1);

            quotaPatternLengthEntry!.value = Mathf.Clamp(
                Mathf.Max(1, Mathf.Min(profile.QuotaMultipliers.Count, MaxEditableQuotaMultipliers)),
                quotaPatternLengthEntry.min,
                quotaPatternLengthEntry.max);
            quotaPatternLengthEntry.defaultValue = quotaPatternLengthEntry.value;

            for (var index = 0; index < quotaMultiplierEntries!.Length; index++)
            {
                var multiplierEntry = quotaMultiplierEntries[index];
                multiplierEntry.max = Mathf.Max(
                    TimingProfileValidator.MaxQuotaMultiplier,
                    Mathf.Ceil(editablePattern[index]));
                multiplierEntry.value = Mathf.Clamp(editablePattern[index], multiplierEntry.min, multiplierEntry.max);
                multiplierEntry.defaultValue = multiplierEntry.value;
            }
        }
        finally
        {
            _isSynchronizing = false;
        }

        RefreshRuntimeSliderBindings();
    }

    private static bool TryGetCurrentProfileForEditing(out TimingProfile? profile)
    {
        if (!TimingCoordinator.TryGetResolvedProfile("NativeLobbySettingsMenu.Sync", out profile) || profile == null)
        {
            return false;
        }

        return true;
    }

    private static bool IsMoreSettingsKey(string? key) =>
        key == DayDurationMinutesKey ||
        key == StartingMoneyKey ||
        key == StartingQuotaKey ||
        key == CatchUpFactorKey ||
        key == QuotaScalingModeKey ||
        key == QuotaPatternLengthKey ||
        IsQuotaMultiplierKey(key);

    public static bool IsManagedSliderKey(string? key) =>
        key == DayDurationMinutesKey ||
        key == StartingMoneyKey ||
        key == StartingQuotaKey ||
        key == CatchUpFactorKey ||
        key == QuotaPatternLengthKey ||
        IsQuotaMultiplierKey(key);

    public static bool IsStartingMoneyEditable() =>
        !NetworkServer.active && !NetworkClient.active;

    public static void RegisterSliderRuntimeBinding(string? key, Transform? root)
    {
        if (string.IsNullOrWhiteSpace(key) || root == null || !IsManagedSliderKey(key))
        {
            return;
        }

        SliderRuntimeBindings.RemoveAll(binding => binding.Root == null);

        if (!SliderRuntimeBindings.Any(binding => binding.Root == root))
        {
            SliderRuntimeBindings.Add(new SliderRuntimeBinding(key, root));
        }

        ApplyRuntimeSliderPresentation(key, root);
    }

    public static void RefreshRuntimeSliderBindings()
    {
        SliderRuntimeBindings.RemoveAll(binding => binding.Root == null);

        foreach (var binding in SliderRuntimeBindings)
        {
            ApplyRuntimeSliderPresentation(binding.Key, binding.Root);
        }
    }

    private static SettingsLayout? GetActiveLobbySettingsLayout() =>
        _runtimeLobbySettingsLayout ?? _lobbySettingsLayout;

    private static bool IsLobbySettingsLayout(SettingsLayout layout) =>
        FindTargetTab(layout) != null;

    private static SettingsLayout.Tab? FindTargetTab(SettingsLayout layout)
    {
        foreach (var tab in layout.tabs)
        {
            if (tab.entries == null || tab.entries.Count == 0)
            {
                continue;
            }

            if (tab.entries.Any(IsLobbyModeEntry))
            {
                return tab;
            }

            if (!string.IsNullOrWhiteSpace(tab.tabName) &&
                string.Equals(tab.tabName.Trim(), "Settings", StringComparison.OrdinalIgnoreCase))
            {
                return tab;
            }
        }

        return null;
    }

    private static bool IsLobbyModeEntry(SettingItemBase entry)
    {
        if (entry is not DropdownSettingItem)
        {
            return false;
        }

        if (string.Equals(entry.key, SectionKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(entry.label?.Trim(), "Lobby Mode", StringComparison.OrdinalIgnoreCase);
    }

    private static int EnsureTitleEntry(ICollection<SettingItemBase> entries, string key, string label)
    {
        if (entries.Any(entry => entry.key == key))
        {
            return 0;
        }

        var titleEntry = ScriptableObject.CreateInstance<TitleSettingItem>();
        titleEntry.hideFlags = HideFlags.HideAndDontSave;
        titleEntry.key = key;
        titleEntry.label = label;
        entries.Add(titleEntry);
        return 1;
    }

    private static int EnsureDropdownEntry(
        ICollection<SettingItemBase> entries,
        string key,
        string label,
        IReadOnlyCollection<string> options,
        int defaultIndex)
    {
        if (entries.Any(entry => entry.key == key))
        {
            return 0;
        }

        var dropdownEntry = ScriptableObject.CreateInstance<DropdownSettingItem>();
        dropdownEntry.hideFlags = HideFlags.HideAndDontSave;
        dropdownEntry.key = key;
        dropdownEntry.label = label;
        dropdownEntry.options = options.ToList();
        dropdownEntry.index = Mathf.Clamp(defaultIndex, 0, dropdownEntry.options.Count - 1);
        dropdownEntry.loadOnSceneStart = false;
        entries.Add(dropdownEntry);
        return 1;
    }

    private static int EnsureSliderEntry(
        ICollection<SettingItemBase> entries,
        string key,
        string label,
        float min,
        float max,
        bool wholeNumbers)
    {
        if (entries.Any(entry => entry.key == key))
        {
            return 0;
        }

        var sliderEntry = ScriptableObject.CreateInstance<SliderSettingItem>();
        sliderEntry.hideFlags = HideFlags.HideAndDontSave;
        sliderEntry.key = key;
        sliderEntry.label = label;
        sliderEntry.min = min;
        sliderEntry.max = max;
        sliderEntry.wholeNumbers = wholeNumbers;
        sliderEntry.value = min;
        sliderEntry.defaultValue = min;
        sliderEntry.loadOnSceneStart = false;
        entries.Add(sliderEntry);
        return 1;
    }

    private static T? FindEntry<T>(SettingsLayout layout, string key)
        where T : SettingItemBase
    {
        foreach (var tab in layout.tabs)
        {
            if (tab.entries == null)
            {
                continue;
            }

            var match = tab.entries.OfType<T>().FirstOrDefault(entry => entry.key == key);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static bool TryGetEditableEntries(
        SettingsLayout layout,
        out SliderSettingItem? dayDurationEntry,
        out SliderSettingItem? startingMoneyEntry,
        out SliderSettingItem? startingQuotaEntry,
        out SliderSettingItem? catchUpFactorEntry,
        out DropdownSettingItem? quotaScalingModeEntry,
        out SliderSettingItem? quotaPatternLengthEntry,
        out SliderSettingItem[]? quotaMultiplierEntries)
    {
        dayDurationEntry = FindEntry<SliderSettingItem>(layout, DayDurationMinutesKey);
        startingMoneyEntry = FindEntry<SliderSettingItem>(layout, StartingMoneyKey);
        startingQuotaEntry = FindEntry<SliderSettingItem>(layout, StartingQuotaKey);
        catchUpFactorEntry = FindEntry<SliderSettingItem>(layout, CatchUpFactorKey);
        quotaScalingModeEntry = FindEntry<DropdownSettingItem>(layout, QuotaScalingModeKey);
        quotaPatternLengthEntry = FindEntry<SliderSettingItem>(layout, QuotaPatternLengthKey);
        quotaMultiplierEntries = GetQuotaMultiplierEntries(layout);

        return dayDurationEntry != null
            && startingMoneyEntry != null
            && startingQuotaEntry != null
            && catchUpFactorEntry != null
            && quotaScalingModeEntry != null
            && quotaPatternLengthEntry != null
            && quotaMultiplierEntries.Length == MaxEditableQuotaMultipliers;
    }

    private static SliderSettingItem[] GetQuotaMultiplierEntries(SettingsLayout layout)
    {
        var entries = new SliderSettingItem[MaxEditableQuotaMultipliers];
        for (var index = 0; index < MaxEditableQuotaMultipliers; index++)
        {
            var entry = FindEntry<SliderSettingItem>(layout, GetQuotaMultiplierKey(index));
            if (entry == null)
            {
                return Array.Empty<SliderSettingItem>();
            }

            entries[index] = entry;
        }

        return entries;
    }

    private static QuotaScalingMode ResolveQuotaScalingMode(DropdownSettingItem entry) =>
        entry.index == (int)QuotaScalingMode.CustomPattern
            ? QuotaScalingMode.CustomPattern
            : QuotaScalingMode.Vanilla;

    private static float ComputeLinkedCurrencySliderMax(long startingMoney, long startingQuota)
    {
        var highestValue = Mathf.Max(DefaultCurrencySliderMax, (float)startingMoney, (float)startingQuota);
        return Mathf.Max(DefaultCurrencySliderMax, Mathf.Ceil(highestValue / CurrencySliderStep) * CurrencySliderStep);
    }

    private static long EnsureLinkedStartingQuota(long startingMoney, long startingQuota) =>
        Math.Max(startingMoney, startingQuota);

    private static void ApplyCurrencyBounds(
        SliderSettingItem startingMoneyEntry,
        SliderSettingItem startingQuotaEntry,
        long startingMoney,
        long startingQuota)
    {
        var linkedCurrencyMax = ComputeLinkedCurrencySliderMax(startingMoney, startingQuota);
        startingMoneyEntry.min = 0f;
        startingMoneyEntry.max = linkedCurrencyMax;
        startingQuotaEntry.min = startingMoney;
        startingQuotaEntry.max = linkedCurrencyMax;
    }

    private static void ApplyRuntimeSliderPresentation(string key, Transform root)
    {
        var activeLayout = GetActiveLobbySettingsLayout();
        var runtimeEntry = activeLayout != null
            ? FindEntry<SliderSettingItem>(activeLayout, key)
            : null;

        if (runtimeEntry != null)
        {
            SyncRuntimeSlider(root, runtimeEntry);
        }

        var valueInput = FindRuntimeValueInput(root);
        if (valueInput != null)
        {
            ApplyFixedWidth(valueInput.gameObject);
        }

        var valueText = root.Find("ValueText");
        if (valueText != null)
        {
            ApplyFixedWidth(valueText.gameObject);
        }

        if (string.Equals(key, StartingMoneyKey, StringComparison.OrdinalIgnoreCase))
        {
            SetInteractable(root, IsStartingMoneyEditable());
        }
    }

    private static void SyncRuntimeSlider(Transform root, SliderSettingItem entry)
    {
        var slider = root.GetComponentInChildren<Slider>(includeInactive: true);
        if (slider != null)
        {
            slider.minValue = entry.min;
            slider.maxValue = entry.max;
            slider.wholeNumbers = entry.wholeNumbers;
            slider.SetValueWithoutNotify(Mathf.Clamp(entry.value, slider.minValue, slider.maxValue));
        }

        var displayValue = entry.wholeNumbers
            ? Mathf.Round(entry.value).ToString()
            : entry.value.ToString("F1");

        var valueInput = FindRuntimeValueInput(root);
        if (valueInput != null)
        {
            valueInput.SetTextWithoutNotify(displayValue);
        }
    }

    private static TMP_InputField? FindRuntimeValueInput(Transform root)
    {
        var valueInput = root.GetComponentInChildren<TMP_InputField>(includeInactive: true);
        if (valueInput != null)
        {
            return valueInput;
        }

        var valueText = root.Find("ValueText");
        return valueText != null ? valueText.GetComponent<TMP_InputField>() : null;
    }

    private static void ApplyFixedWidth(GameObject gameObject)
    {
        var layoutElement = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        layoutElement.minWidth = SliderValueFieldWidth;
        layoutElement.preferredWidth = SliderValueFieldWidth;
        layoutElement.flexibleWidth = 0f;

        var rectTransform = gameObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, SliderValueFieldWidth);
        }
    }

    private static void SetInteractable(Transform root, bool isInteractable)
    {
        foreach (var selectable in root.GetComponentsInChildren<Selectable>(includeInactive: true))
        {
            selectable.interactable = isInteractable;
        }
    }

    private static string GetQuotaMultiplierKey(int index) =>
        $"moresettings.quota-multiplier-{index + 1}";

    private static bool IsQuotaMultiplierKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) &&
        key.StartsWith("moresettings.quota-multiplier-", StringComparison.OrdinalIgnoreCase);

    private sealed class SliderRuntimeBinding
    {
        public SliderRuntimeBinding(string key, Transform root)
        {
            Key = key;
            Root = root;
        }

        public string Key { get; }

        public Transform Root { get; }
    }
}