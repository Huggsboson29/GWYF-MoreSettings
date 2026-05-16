using System;

namespace MoreSettings.Runtime;

public static class QuotaRuntimeStatePlanner
{
    public static int ComputeRemainingDays(int daysBeforeQuota, int daysPassed) =>
        Math.Max(0, daysBeforeQuota - daysPassed);

    public static bool ShouldResetUntouchedPreDayState(
        long currentQuota,
        long requiredQuota,
        int currentFloor,
        int daysPassed,
        int successfulQuota) =>
        successfulQuota == 0 &&
        daysPassed == 0 &&
        currentFloor <= 1 &&
        currentQuota == requiredQuota;

    public static bool ShouldResetInitialQuota(
        long previousStartingQuota,
        long currentQuota,
        long requiredQuota,
        int daysPassed,
        int successfulQuota) =>
        successfulQuota == 0 &&
        daysPassed == 0 &&
        currentQuota == previousStartingQuota &&
        requiredQuota == previousStartingQuota;

    public static bool ShouldResetInitialMoney(
        long previousStartingMoney,
        long currentMoney,
        int daysPassed,
        int successfulQuota) =>
        successfulQuota == 0 &&
        daysPassed == 0 &&
        currentMoney == previousStartingMoney;
}
