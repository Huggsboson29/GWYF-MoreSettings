using System;

namespace MoreSettings.Models;

public sealed class SessionTimingState
{
    public SessionTimingState(
        string source,
        string profileName,
        bool isVanilla,
        float dayDurationSeconds,
        int daysBeforeQuota,
        long startingQuota,
        float catchUpFactor,
        QuotaScalingMode quotaScalingMode,
        int quotaMultiplierCount,
        DateTimeOffset appliedAtUtc)
    {
        Source = source;
        ProfileName = profileName;
        IsVanilla = isVanilla;
        DayDurationSeconds = dayDurationSeconds;
        DaysBeforeQuota = daysBeforeQuota;
        StartingQuota = startingQuota;
        CatchUpFactor = catchUpFactor;
        QuotaScalingMode = quotaScalingMode;
        QuotaMultiplierCount = quotaMultiplierCount;
        AppliedAtUtc = appliedAtUtc;
    }

    public string Source { get; }

    public string ProfileName { get; }

    public bool IsVanilla { get; }

    public float DayDurationSeconds { get; }

    public int DaysBeforeQuota { get; }

    public long StartingQuota { get; }

    public float CatchUpFactor { get; }

    public QuotaScalingMode QuotaScalingMode { get; }

    public int QuotaMultiplierCount { get; }

    public DateTimeOffset AppliedAtUtc { get; }
}
