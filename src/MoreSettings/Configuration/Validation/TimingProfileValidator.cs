using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MoreSettings.Models;

namespace MoreSettings.Configuration;

public static class TimingProfileValidator
{
    public const float MinDayDurationSeconds = 10f;
    public const float MaxDayDurationSeconds = 86400f;
    public const int MinDaysBeforeQuota = 1;
    public const int MaxDaysBeforeQuota = 30;
    public const long MaxQuotaValue = 1_000_000_000_000_000_000L;
    public const float MinCatchUpFactor = 0f;
    public const float MaxCatchUpFactor = 5f;
    public const float MinQuotaMultiplier = 0.01f;
    public const float MaxQuotaMultiplier = 100f;

    public static IReadOnlyList<ValidationOutcome> Validate(TimingProfile profile)
    {
        var outcomes = new List<ValidationOutcome>();

        if (profile.DayDurationSeconds < MinDayDurationSeconds || profile.DayDurationSeconds > MaxDayDurationSeconds)
        {
            outcomes.Add(ValidationOutcome.Error(
                nameof(profile.DayDurationSeconds),
                $"Day duration must be between {MinDayDurationSeconds} and {MaxDayDurationSeconds} seconds."));
        }

        if (profile.DaysBeforeQuota < MinDaysBeforeQuota || profile.DaysBeforeQuota > MaxDaysBeforeQuota)
        {
            outcomes.Add(ValidationOutcome.Error(
                nameof(profile.DaysBeforeQuota),
                $"Days before quota must be between {MinDaysBeforeQuota} and {MaxDaysBeforeQuota}."));
        }

        if (profile.StartingQuota < 0 || profile.StartingQuota > MaxQuotaValue)
        {
            outcomes.Add(ValidationOutcome.Error(
                nameof(profile.StartingQuota),
                $"Starting quota must be between 0 and {MaxQuotaValue}."));
        }

        if (profile.CatchUpFactor < MinCatchUpFactor || profile.CatchUpFactor > MaxCatchUpFactor)
        {
            outcomes.Add(ValidationOutcome.Error(
                nameof(profile.CatchUpFactor),
                $"Catch-up factor must be between {MinCatchUpFactor} and {MaxCatchUpFactor}."));
        }

        if (profile.QuotaScalingMode == QuotaScalingMode.CustomPattern)
        {
            if (profile.QuotaMultipliers.Count == 0)
            {
                outcomes.Add(ValidationOutcome.Error(
                    nameof(profile.QuotaMultipliers),
                    "At least one quota multiplier is required when custom quota scaling is enabled."));
            }
            else if (profile.QuotaMultipliers.Any(multiplier => multiplier < MinQuotaMultiplier || multiplier > MaxQuotaMultiplier))
            {
                outcomes.Add(ValidationOutcome.Error(
                    nameof(profile.QuotaMultipliers),
                    $"Each quota multiplier must be between {MinQuotaMultiplier} and {MaxQuotaMultiplier}."));
            }
        }

        if (outcomes.Count == 0)
        {
            outcomes.Add(ValidationOutcome.Valid("Profile", "Timing profile values are valid."));
        }

        return outcomes;
    }

    public static bool TryParseQuotaMultipliers(
        string rawValue,
        out float[] parsedValues,
        out ValidationOutcome? error)
    {
        var tokens = rawValue
            .Split(new[] { ',', ';', ' ' }, System.StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim())
            .ToArray();

        if (tokens.Length == 0)
        {
            parsedValues = new float[0];
            error = ValidationOutcome.Error(nameof(rawValue), "Quota multipliers cannot be empty when an override string is provided.");
            return false;
        }

        var values = new List<float>(tokens.Length);
        foreach (var token in tokens)
        {
            if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                parsedValues = new float[0];
                error = ValidationOutcome.Error(nameof(rawValue), $"'{token}' is not a valid floating-point quota multiplier.");
                return false;
            }

            values.Add(value);
        }

        parsedValues = values.ToArray();
        error = null;
        return true;
    }
}
