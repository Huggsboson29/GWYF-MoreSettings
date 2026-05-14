using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MoreSettings.Configuration;
using MoreSettings.Models;
using UnityEngine;

namespace MoreSettings.Runtime;

public static class TimingCoordinator
{
    private static readonly AccessTools.FieldRef<GameManager, GameSettings> GameSettingsRef =
        AccessTools.FieldRefAccess<GameManager, GameSettings>("_gs");

    private static readonly AccessTools.FieldRef<SaveManager, SaveData> CurrentSaveDataRef =
        AccessTools.FieldRefAccess<SaveManager, SaveData>("currentSaveData");

    private static readonly System.Reflection.PropertyInfo? HasDayStartedProperty =
        AccessTools.Property(typeof(GameManager), nameof(GameManager.HasDayStarted));

    private static readonly System.Reflection.PropertyInfo? NetworkTimerProperty =
        AccessTools.Property(typeof(GameManager), "Network_timer");

    private static readonly System.Reflection.PropertyInfo? NetworkCurrentQuotaProperty =
        AccessTools.Property(typeof(GameManager), "NetworkcurrentQuota");

    private static readonly System.Reflection.PropertyInfo? NetworkRequiredQuotaProperty =
        AccessTools.Property(typeof(GameManager), "NetworkrequiredQuotaToNextFloor");

    private static readonly System.Reflection.PropertyInfo? NetworkDaysLeftProperty =
        AccessTools.Property(typeof(GameManager), "NetworkdaysLeft");

    private static readonly System.Reflection.PropertyInfo? NetworkDaysPassedProperty =
        AccessTools.Property(typeof(GameManager), "NetworkdaysPassed");

    private static readonly System.Reflection.PropertyInfo? NetworkSuccessfulQuotaProperty =
        AccessTools.Property(typeof(GameManager), "NetworksuccessfulQuota");

    private static ActiveSettings? _settings;
    private static ManualLogSource? _log;
    private static TimingProfile? _vanillaProfile;

    public static SessionTimingState? CurrentState { get; private set; }

    public static void Initialize(ActiveSettings settings, ManualLogSource log)
    {
        _settings = settings;
        _log = log;
        _vanillaProfile = null;
        CurrentState = null;
    }

    public static bool TryApplyFromResources(string context)
    {
        EnsureInitialized();

        if (!TryGetActiveGameSettings(context, out var gameSettings, includeSceneInstance: false))
        {
            return false;
        }

        return TryApplyToGameSettings(gameSettings!, context);
    }

    public static bool TryApplyToGameSettings(GameSettings gameSettings, string context)
    {
        EnsureInitialized();
        CaptureVanillaProfile(gameSettings);

        if (!_settings!.IsEnabled)
        {
            var baseProfile = GetBaseProfile(gameSettings);
            CurrentState = BuildState(
                baseProfile.Name,
                baseProfile.IsVanillaProfile,
                baseProfile.DayDurationSeconds,
                baseProfile.DaysBeforeQuota,
                baseProfile.StartingQuota,
                baseProfile.StartingMoney,
                baseProfile.CatchUpFactor,
                baseProfile.QuotaScalingMode,
                baseProfile.QuotaMultipliers.Count,
                context);

            return false;
        }

        if (!TryResolveProfile(gameSettings, context, out var resolvedProfile))
        {
            return false;
        }

        var profile = resolvedProfile!;

        ApplyResolvedProfileToGameSettings(gameSettings, profile, context);
        return true;
    }

    public static bool TryApplyInitialQuotaToSaveData(SaveData saveData, string context)
    {
        EnsureInitialized();

        if (saveData == null || !_settings!.IsEnabled)
        {
            return false;
        }

        if (!TryGetActiveGameSettings(context, out var gameSettings))
        {
            return false;
        }

        if (!TryResolveProfile(gameSettings!, context, out var resolvedProfile))
        {
            return false;
        }

        ApplyResolvedProfileToSaveData(saveData, resolvedProfile!, context, resetQuotaState: true);

        return true;
    }

    public static bool TryApplyToActiveSaveData(string context)
    {
        EnsureInitialized();

        if (!_settings!.IsEnabled)
        {
            return false;
        }

        var saveManager = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        if (saveManager == null)
        {
            return false;
        }

        var saveData = CurrentSaveDataRef(saveManager);
        if (saveData == null)
        {
            return false;
        }

        var gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null && !CanUpdatePreDayRuntimeState(gameManager))
        {
            return false;
        }

        if (!TryGetActiveGameSettings(context, out var gameSettings))
        {
            return false;
        }

        if (!TryResolveProfile(gameSettings!, context, out var resolvedProfile))
        {
            return false;
        }

        ApplyResolvedProfileToSaveData(saveData, resolvedProfile!, context, resetQuotaState: true);
        return true;
    }

    public static bool TryGetResolvedProfile(string context, out TimingProfile? profile)
    {
        EnsureInitialized();

        if (!TryGetActiveGameSettings(context, out var gameSettings))
        {
            profile = null;
            return false;
        }

        if (!_settings!.IsEnabled)
        {
            profile = GetBaseProfile(gameSettings!);

            return true;
        }

        return TryResolveProfile(gameSettings!, context, out profile);
    }

    public static bool TryApplyManualOverrides(
        TimingProfile previousProfile,
        TimingProfile updatedProfile,
        string context,
        out IReadOnlyList<ValidationOutcome> outcomes)
    {
        EnsureInitialized();

        if (!TryGetActiveGameSettings(context, out var gameSettings))
        {
            outcomes = new[]
            {
                ValidationOutcome.Error(nameof(GameSettings), "No active GameSettings were available to apply manual overrides."),
            };

            return false;
        }

        var validation = TimingProfileValidator.Validate(updatedProfile);
        outcomes = validation;
        var errors = validation.Where(outcome => outcome.Status == ValidationStatus.Error).ToArray();
        if (errors.Length > 0)
        {
            foreach (var outcome in errors)
            {
                _log!.LogError($"[{context}] {outcome.TargetField}: {outcome.Message}");
            }

            return false;
        }

        CaptureVanillaProfile(gameSettings!);

        _settings!.SetManualOverrides(updatedProfile);
        if (!_settings.TryCreateResolvedProfile(GetBaseProfile(gameSettings!), out var resolvedProfile, out var resolvedOutcomes))
        {
            outcomes = resolvedOutcomes;
            foreach (var outcome in resolvedOutcomes.Where(outcome => outcome.Status == ValidationStatus.Error))
            {
                _log!.LogError($"[{context}] {outcome.TargetField}: {outcome.Message}");
            }

            return false;
        }

        outcomes = resolvedOutcomes;
        ApplyResolvedProfileToGameSettings(gameSettings!, resolvedProfile, context);

        var gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            ApplyManualProfileToGameManager(gameManager, previousProfile, resolvedProfile, context);
        }

        var saveManager = UnityEngine.Object.FindFirstObjectByType<SaveManager>();
        if (saveManager != null)
        {
            var saveData = CurrentSaveDataRef(saveManager);
            if (saveData != null)
            {
                ApplyManualProfileToSaveData(saveData, previousProfile, resolvedProfile, context);
            }
        }

        return true;
    }

    public static bool TryApplyToGameManagerRuntime(GameManager gameManager, string context)
    {
        EnsureInitialized();

        if (gameManager == null || !_settings!.IsEnabled)
        {
            return false;
        }

        var gameSettings = GameSettingsRef(gameManager);
        if (gameSettings == null)
        {
            return false;
        }

        if (!TryResolveProfile(gameSettings, context, out var resolvedProfile))
        {
            return false;
        }

        ApplyResolvedProfileToGameManager(gameManager, resolvedProfile!, context);
        return true;
    }

    private static bool TryResolveProfile(GameSettings gameSettings, string context, out TimingProfile? profile)
    {
        CaptureVanillaProfile(gameSettings);
        if (!_settings!.TryCreateResolvedProfile(GetBaseProfile(gameSettings), out var resolvedProfile, out var outcomes))
        {
            foreach (var outcome in outcomes.Where(o => o.Status == ValidationStatus.Error))
            {
                _log!.LogError($"[{context}] {outcome.TargetField}: {outcome.Message}");
            }

            profile = null;
            return false;
        }

        profile = resolvedProfile;
        return true;
    }

    private static void CaptureVanillaProfile(GameSettings gameSettings)
    {
        if (_vanillaProfile != null || gameSettings == null)
        {
            return;
        }

        _vanillaProfile = new TimingProfile(
            "Vanilla",
            true,
            gameSettings.dayDuration,
            gameSettings.daysBeforeQuota,
            gameSettings.startingQuota,
            gameSettings.startingMoney,
            gameSettings.catchUpFactor,
            QuotaScalingMode.Vanilla,
            (gameSettings.quotas ?? Array.Empty<float>()).ToArray());
    }

    private static TimingProfile GetBaseProfile(GameSettings gameSettings)
    {
        CaptureVanillaProfile(gameSettings);
        return _vanillaProfile ?? new TimingProfile(
            "Vanilla",
            true,
            gameSettings.dayDuration,
            gameSettings.daysBeforeQuota,
            gameSettings.startingQuota,
            gameSettings.startingMoney,
            gameSettings.catchUpFactor,
            QuotaScalingMode.Vanilla,
            (gameSettings.quotas ?? Array.Empty<float>()).ToArray());
    }

    private static bool TryGetActiveGameSettings(
        string context,
        out GameSettings? gameSettings,
        bool includeSceneInstance = true)
    {
        if (includeSceneInstance)
        {
            var gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                var sceneSettings = GameSettingsRef(gameManager);
                if (sceneSettings != null)
                {
                    gameSettings = sceneSettings;
                    return true;
                }
            }
        }

        gameSettings = Resources.Load<GameSettings>("GameSettings");
        if (gameSettings == null)
        {
            _log!.LogDebug($"No GameSettings resource was available during {context}.");
            return false;
        }

        return true;
    }

    private static void ApplyResolvedProfileToGameSettings(GameSettings gameSettings, TimingProfile profile, string context)
    {
        gameSettings.dayDuration = profile.DayDurationSeconds;
        gameSettings.startingQuota = profile.StartingQuota;
        gameSettings.startingMoney = profile.StartingMoney;
        gameSettings.catchUpFactor = profile.CatchUpFactor;
        gameSettings.quotas = profile.QuotaMultipliers.ToArray();

        CurrentState = BuildState(
            profile.Name,
            profile.IsVanillaProfile,
            profile.DayDurationSeconds,
            profile.DaysBeforeQuota,
            profile.StartingQuota,
            profile.StartingMoney,
            profile.CatchUpFactor,
            profile.QuotaScalingMode,
            profile.QuotaMultipliers.Count,
            context);

        _log!.LogInfo(
            $"[{context}] Applied timing profile '{profile.Name}' " +
            $"(dayDuration={profile.DayDurationSeconds}, daysBeforeQuota={profile.DaysBeforeQuota}, " +
            $"startingQuota={profile.StartingQuota}, startingMoney={profile.StartingMoney}, catchUpFactor={profile.CatchUpFactor}, " +
            $"quotaScalingMode={profile.QuotaScalingMode}, " +
            $"quotaMultipliers={profile.QuotaMultipliers.Count}).");
    }

    private static void ApplyResolvedProfileToGameManager(
        GameManager gameManager,
        TimingProfile profile,
        string context,
        bool resetQuotaState = true)
    {
        if (!CanUpdatePreDayRuntimeState(gameManager))
        {
            return;
        }

        NetworkTimerProperty?.SetValue(gameManager, profile.DayDurationSeconds);

        if (resetQuotaState)
        {
            NetworkCurrentQuotaProperty?.SetValue(gameManager, profile.StartingQuota);
            NetworkRequiredQuotaProperty?.SetValue(gameManager, profile.StartingQuota);
        }

        _log!.LogInfo(
            $"[{context}] Applied pre-day runtime state to GameManager " +
            $"(timer={profile.DayDurationSeconds}, quotaReset={resetQuotaState}, " +
            $"currentQuota={profile.StartingQuota}, requiredQuota={profile.StartingQuota}).");
    }

    private static void ApplyManualProfileToGameManager(
        GameManager gameManager,
        TimingProfile previousProfile,
        TimingProfile updatedProfile,
        string context)
    {
        if (!CanUpdatePreDayRuntimeState(gameManager))
        {
            return;
        }

        NetworkTimerProperty?.SetValue(gameManager, updatedProfile.DayDurationSeconds);

        var daysPassed = ReadIntProperty(NetworkDaysPassedProperty, gameManager);
        var successfulQuota = ReadIntProperty(NetworkSuccessfulQuotaProperty, gameManager);

        var shouldResetInitialQuota = QuotaRuntimeStatePlanner.ShouldResetInitialQuota(
            previousProfile.StartingQuota,
            ReadLongProperty(NetworkCurrentQuotaProperty, gameManager),
            ReadLongProperty(NetworkRequiredQuotaProperty, gameManager),
            daysPassed,
            successfulQuota);

        if (shouldResetInitialQuota)
        {
            NetworkCurrentQuotaProperty?.SetValue(gameManager, updatedProfile.StartingQuota);
            NetworkRequiredQuotaProperty?.SetValue(gameManager, updatedProfile.StartingQuota);
        }

        _log!.LogInfo(
            $"[{context}] Applied manual pre-day runtime state to GameManager " +
            $"(timer={updatedProfile.DayDurationSeconds}, preservedQuota={!shouldResetInitialQuota}).");
    }

    private static void ApplyResolvedProfileToSaveData(
        SaveData saveData,
        TimingProfile profile,
        string context,
        bool resetQuotaState)
    {
        if (resetQuotaState)
        {
            saveData.currentQuota = profile.StartingQuota;
            saveData.requiredQuotaToNextFloor = profile.StartingQuota;
            saveData.money = profile.StartingMoney;
            TrySyncMoneyManagerBalance(profile.StartingMoney);
        }

        _log!.LogInfo(
            $"[{context}] Applied save timing state " +
            $"(quotaReset={resetQuotaState}, currentQuota={saveData.currentQuota}, " +
            $"requiredQuota={saveData.requiredQuotaToNextFloor}, money={saveData.money}).");
    }

    private static void ApplyManualProfileToSaveData(
        SaveData saveData,
        TimingProfile previousProfile,
        TimingProfile updatedProfile,
        string context)
    {
        var shouldResetInitialQuota = QuotaRuntimeStatePlanner.ShouldResetInitialQuota(
            previousProfile.StartingQuota,
            saveData.currentQuota,
            saveData.requiredQuotaToNextFloor,
            saveData.daysPassed,
            saveData.successfulQuota);

        var shouldResetInitialMoney = QuotaRuntimeStatePlanner.ShouldResetInitialMoney(
            previousProfile.StartingMoney,
            saveData.money,
            saveData.daysPassed,
            saveData.successfulQuota);

        if (shouldResetInitialQuota)
        {
            saveData.currentQuota = updatedProfile.StartingQuota;
            saveData.requiredQuotaToNextFloor = updatedProfile.StartingQuota;
        }

        if (shouldResetInitialMoney)
        {
            saveData.money = updatedProfile.StartingMoney;
        }

        if (shouldResetInitialMoney)
        {
            TrySyncMoneyManagerBalance(updatedProfile.StartingMoney);
        }

        _log!.LogInfo(
            $"[{context}] Updated active save state " +
            $"(preservedQuota={!shouldResetInitialQuota}, preservedMoney={!shouldResetInitialMoney}).");
    }

    private static bool CanUpdatePreDayRuntimeState(GameManager gameManager)
    {
        var hasDayStarted = HasDayStartedProperty?.GetValue(gameManager) as bool? ?? false;
        return !hasDayStarted;
    }

    private static void TrySyncMoneyManagerBalance(long balance)
    {
        var moneyManager = UnityEngine.Object.FindFirstObjectByType<MoneyManager>();
        if (moneyManager == null)
        {
            return;
        }

        moneyManager.SetBalance(balance, null, ChangeType.Save);
    }

    private static int ReadIntProperty(System.Reflection.PropertyInfo? property, object instance) =>
        property?.GetValue(instance) switch
        {
            int value => value,
            long value => checked((int)value),
            _ => 0,
        };

    private static long ReadLongProperty(System.Reflection.PropertyInfo? property, object instance) =>
        property?.GetValue(instance) switch
        {
            long value => value,
            int value => value,
            _ => 0L,
        };

    private static SessionTimingState BuildState(
        string profileName,
        bool isVanilla,
        float dayDurationSeconds,
        int daysBeforeQuota,
        long startingQuota,
        long startingMoney,
        float catchUpFactor,
        QuotaScalingMode quotaScalingMode,
        int quotaMultiplierCount,
        string context)
    {
        return new SessionTimingState(
            context,
            profileName,
            isVanilla,
            dayDurationSeconds,
            daysBeforeQuota,
            startingQuota,
            startingMoney,
            catchUpFactor,
            quotaScalingMode,
            quotaMultiplierCount,
            DateTimeOffset.UtcNow);
    }

    private static void EnsureInitialized()
    {
        if (_settings == null || _log == null)
        {
            throw new InvalidOperationException("TimingCoordinator.Initialize must run before timing overrides are applied.");
        }
    }
}
