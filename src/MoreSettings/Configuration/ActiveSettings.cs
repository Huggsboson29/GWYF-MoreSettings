using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using MoreSettings.Models;
using UnityEngine;

namespace MoreSettings.Configuration;

public sealed class ActiveSettings
{
    private readonly ConfigFile _config;
    public const float PreserveFloat = -1f;
    public const int PreserveInt = -1;
    public const long PreserveLong = -1L;

    private readonly ConfigEntry<bool> _enableCustomTiming;
    private readonly ConfigEntry<float> _dayDurationSeconds;
    private readonly ConfigEntry<int> _daysBeforeQuota;
    private readonly ConfigEntry<long> _startingQuota;
    private readonly ConfigEntry<long> _startingMoney;
    private readonly ConfigEntry<float> _catchUpFactor;
    private readonly ConfigEntry<string> _quotaScalingMode;
    private readonly ConfigEntry<string> _quotaMultipliersCsv;
    private readonly ConfigEntry<string> _activeProfileName;
    private readonly ProfileStore _profileStore;

    private ActiveSettings(
        ConfigFile config,
        ConfigEntry<bool> enableCustomTiming,
        ConfigEntry<float> dayDurationSeconds,
        ConfigEntry<int> daysBeforeQuota,
        ConfigEntry<long> startingQuota,
        ConfigEntry<long> startingMoney,
        ConfigEntry<float> catchUpFactor,
        ConfigEntry<string> quotaScalingMode,
        ConfigEntry<string> quotaMultipliersCsv,
        ConfigEntry<string> activeProfileName,
        ProfileStore profileStore)
    {
        _config = config;
        _enableCustomTiming = enableCustomTiming;
        _dayDurationSeconds = dayDurationSeconds;
        _daysBeforeQuota = daysBeforeQuota;
        _startingQuota = startingQuota;
        _startingMoney = startingMoney;
        _catchUpFactor = catchUpFactor;
        _quotaScalingMode = quotaScalingMode;
        _quotaMultipliersCsv = quotaMultipliersCsv;
        _activeProfileName = activeProfileName;
        _profileStore = profileStore;
    }

    public bool IsEnabled => _enableCustomTiming.Value;

    /// <summary>
    /// The profile store used to save and load named timing profiles.
    /// Access this from other components to persist or enumerate profiles.
    /// </summary>
    public ProfileStore Profiles => _profileStore;

    public string ActiveProfileName => _activeProfileName.Value?.Trim() ?? string.Empty;

    public static ActiveSettings Bind(ConfigFile config) => Bind(config, LoadVanillaDefaults());

    public static ActiveSettings Bind(ConfigFile config, TimingProfile vanillaDefaults)
    {
        var enableCustomTiming = config.Bind(
            "General",
            "EnableCustomTiming",
            false,
            "Enable host-authoritative timing overrides for new sessions.");

        var dayDurationSeconds = config.Bind(
            "Timing",
            "DayDurationSeconds",
            vanillaDefaults.DayDurationSeconds,
            "Override the vanilla day duration in seconds. Defaults to the current vanilla value. Set -1 to preserve the loaded value.");

        var daysBeforeQuota = config.Bind(
            "Timing",
            "DaysBeforeQuota",
            vanillaDefaults.DaysBeforeQuota,
            "Deprecated. Multi-day quota overrides are ignored. Defaults to the current vanilla value; set -1 to preserve the loaded value.");

        var startingQuota = config.Bind(
            "Quota",
            "StartingQuota",
            vanillaDefaults.StartingQuota,
            "Override the starting quota for new sessions. Defaults to the current vanilla value. Set -1 to preserve the loaded value.");

        var startingMoney = config.Bind(
            "Quota",
            "StartingMoney",
            vanillaDefaults.StartingMoney,
            "Override the starting money for new sessions. Defaults to the current vanilla value. Set -1 to preserve the loaded value.");

        var catchUpFactor = config.Bind(
            "Quota",
            "CatchUpFactor",
            vanillaDefaults.CatchUpFactor,
            "Override the quota catch-up factor. Defaults to the current vanilla value. Set -1 to preserve the loaded value.");

        var quotaScalingMode = config.Bind(
            "Quota",
            "QuotaScalingMode",
            QuotaScalingMode.Vanilla.ToString(),
            "Set the quota scaling mode to Vanilla or CustomPattern. Defaults to Vanilla.");

        var quotaMultipliersCsv = config.Bind(
            "Quota",
            "QuotaMultipliersCsv",
            string.Empty,
            "Comma-separated quota multipliers. Leave blank to preserve the loaded values.");

        var activeProfileName = config.Bind(
            "Profiles",
            "ActiveProfileName",
            string.Empty,
            "Name of a saved profile to load on startup. Overrides all individual Timing/Quota entries " +
            "when non-empty. Use the ProfileStore API or edit profile files under " +
            "BepInEx/config/MoreSettings/profiles/.");

        var configDir = Path.GetDirectoryName(config.ConfigFilePath)
            ?? System.AppDomain.CurrentDomain.BaseDirectory;
        var profileStore = new ProfileStore(configDir);

        return new ActiveSettings(
            config,
            enableCustomTiming,
            dayDurationSeconds,
            daysBeforeQuota,
            startingQuota,
            startingMoney,
            catchUpFactor,
            quotaScalingMode,
            quotaMultipliersCsv,
            activeProfileName,
            profileStore);
    }

    private static TimingProfile LoadVanillaDefaults()
    {
        var gameSettings = Resources.Load<GameSettings>("GameSettings");
        if (gameSettings == null)
            throw new System.InvalidOperationException("Could not load the GameSettings resource.");

        return new TimingProfile(
            "Vanilla",
            true,
            gameSettings.dayDuration,
            gameSettings.daysBeforeQuota,
            gameSettings.startingQuota,
            gameSettings.startingMoney,
            gameSettings.catchUpFactor,
            QuotaScalingMode.Vanilla,
            (gameSettings.quotas ?? System.Array.Empty<float>()).ToArray());
    }

    public void SetManualOverrides(TimingProfile profile)
    {
        _enableCustomTiming.Value = true;
        _activeProfileName.Value = string.Empty;
        _dayDurationSeconds.Value = profile.DayDurationSeconds;
        _daysBeforeQuota.Value = PreserveInt;
        _startingQuota.Value = profile.StartingQuota;
        _startingMoney.Value = profile.StartingMoney;
        _catchUpFactor.Value = profile.CatchUpFactor;
        _quotaScalingMode.Value = profile.QuotaScalingMode.ToString();
        _quotaMultipliersCsv.Value = profile.QuotaScalingMode == QuotaScalingMode.CustomPattern
            ? string.Join(",",
                profile.QuotaMultipliers.Select(multiplier => multiplier.ToString(CultureInfo.InvariantCulture)))
            : string.Empty;

        _config.Save();
    }

    public bool TryCreateResolvedProfile(
        TimingProfile baseProfile,
        out TimingProfile profile,
        out IReadOnlyList<ValidationOutcome> outcomes)
    {
        var validation = new List<ValidationOutcome>();

        // Named profile takes precedence over individual config entries.
        StoredProfile? stored = null;
        var profileName = _activeProfileName.Value?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(profileName) && _profileStore.TryLoad(profileName, out StoredProfile? loaded))
            stored = loaded;

        var resolvedDayDuration = ResolveFloat(
            stored?.DayDurationSeconds ?? _dayDurationSeconds.Value,
            baseProfile.DayDurationSeconds);

        var resolvedDaysBeforeQuota = baseProfile.DaysBeforeQuota;

        var resolvedStartingQuota = ResolveLong(
            stored?.StartingQuota ?? _startingQuota.Value,
            baseProfile.StartingQuota);

        var resolvedStartingMoney = ResolveLong(
            stored?.StartingMoney ?? _startingMoney.Value,
            baseProfile.StartingMoney);

        var resolvedCatchUpFactor = ResolveFloat(
            stored?.CatchUpFactor ?? _catchUpFactor.Value,
            baseProfile.CatchUpFactor);

        var rawMultipliers = _quotaMultipliersCsv.Value?.Trim() ?? string.Empty;
        var resolvedQuotaScalingMode = ResolveQuotaScalingMode(
            stored?.QuotaScalingMode,
            _quotaScalingMode.Value,
            stored?.QuotaMultipliers,
            rawMultipliers);

        var resolvedQuotaMultipliers = baseProfile.QuotaMultipliers.ToArray();
        if (resolvedQuotaScalingMode == QuotaScalingMode.CustomPattern && stored?.QuotaMultipliers is { Length: > 0 } storedMultipliers)
        {
            resolvedQuotaMultipliers = storedMultipliers;
        }
        else if (resolvedQuotaScalingMode == QuotaScalingMode.CustomPattern)
        {
            if (!string.IsNullOrWhiteSpace(rawMultipliers))
            {
                if (TimingProfileValidator.TryParseQuotaMultipliers(rawMultipliers, out var parsed, out var parseError))
                    resolvedQuotaMultipliers = parsed;
                else if (parseError is not null)
                    validation.Add(parseError);
            }
            else
            {
                resolvedQuotaMultipliers = new float[0];
            }
        }

        var resolvedName = !string.IsNullOrEmpty(profileName) ? profileName : "ActiveConfig";
        profile = new TimingProfile(
            IsEnabled ? resolvedName : "Vanilla",
            !IsEnabled,
            resolvedDayDuration,
            resolvedDaysBeforeQuota,
            resolvedStartingQuota,
            resolvedStartingMoney,
            resolvedCatchUpFactor,
            resolvedQuotaScalingMode,
            resolvedQuotaMultipliers.ToArray());

        validation.AddRange(TimingProfileValidator.Validate(profile));
        outcomes = validation;

        return validation.All(outcome => outcome.Status != ValidationStatus.Error);
    }

    private static float ResolveFloat(float value, float vanilla) =>
        value > PreserveFloat ? value : vanilla;

    private static int ResolveInt(int value, int vanilla) =>
        value > PreserveInt ? value : vanilla;

    private static long ResolveLong(long value, long vanilla) =>
        value > PreserveLong ? value : vanilla;

    private static QuotaScalingMode ResolveQuotaScalingMode(
        QuotaScalingMode? storedMode,
        string configuredMode,
        float[]? storedMultipliers,
        string rawMultipliers)
    {
        if (storedMode.HasValue)
        {
            return storedMode.Value;
        }

        if (QuotaScalingModeParser.TryParse(configuredMode, out var parsedMode))
        {
            return parsedMode;
        }

        var hasCustomPattern = (storedMultipliers?.Length ?? 0) > 0 || !string.IsNullOrWhiteSpace(rawMultipliers);
        return hasCustomPattern ? QuotaScalingMode.CustomPattern : QuotaScalingMode.Vanilla;
    }
}
