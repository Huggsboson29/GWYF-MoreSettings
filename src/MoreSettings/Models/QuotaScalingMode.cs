using System;

namespace MoreSettings.Models;

public enum QuotaScalingMode
{
    Vanilla = 0,
    CustomPattern = 1,
}

public static class QuotaScalingModeParser
{
    public static bool TryParse(string? rawValue, out QuotaScalingMode mode) =>
        Enum.TryParse(rawValue?.Trim(), ignoreCase: true, out mode);

    public static QuotaScalingMode ParseOrDefault(string? rawValue, QuotaScalingMode fallback = QuotaScalingMode.Vanilla) =>
        TryParse(rawValue, out var mode) ? mode : fallback;
}
