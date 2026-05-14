using System;
using System.Collections.Generic;

namespace MoreSettings.Models;

public sealed class TimingProfile
{
    public TimingProfile(
        string name,
        bool isVanillaProfile,
        float dayDurationSeconds,
        int daysBeforeQuota,
        long startingQuota,
        long startingMoney,
        float catchUpFactor,
        QuotaScalingMode quotaScalingMode,
        IReadOnlyList<float> quotaMultipliers)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        IsVanillaProfile = isVanillaProfile;
        DayDurationSeconds = dayDurationSeconds;
        DaysBeforeQuota = daysBeforeQuota;
        StartingQuota = startingQuota;
        StartingMoney = startingMoney;
        CatchUpFactor = catchUpFactor;
        QuotaScalingMode = quotaScalingMode;
        QuotaMultipliers = quotaMultipliers ?? throw new ArgumentNullException(nameof(quotaMultipliers));
    }

    public string Name { get; }

    public bool IsVanillaProfile { get; }

    public float DayDurationSeconds { get; }

    public int DaysBeforeQuota { get; }

    public long StartingQuota { get; }

    public long StartingMoney { get; }

    public float CatchUpFactor { get; }

    public QuotaScalingMode QuotaScalingMode { get; }

    public IReadOnlyList<float> QuotaMultipliers { get; }
}
