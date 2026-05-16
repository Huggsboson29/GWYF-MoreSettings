using System;

namespace MoreSettings.Runtime;

public static class QuotaRuntimeStatePlanner
{
    public static int ComputeRemainingDays(int daysBeforeQuota, int daysPassed) =>
        Math.Max(0, daysBeforeQuota - daysPassed);

    public static bool ShouldResetUntouchedPreDayState(
        int currentFloor,
        int daysPassed,
        int successfulQuota) =>
        successfulQuota == 0 &&
        daysPassed == 0 &&
        currentFloor <= 0;

    public static bool ShouldResetInitialQuota(
        long previousStartingQuota,
        long currentQuota,
        int currentFloor,
        int daysPassed,
        int successfulQuota) =>
        ShouldResetUntouchedPreDayState(currentFloor, daysPassed, successfulQuota) &&
        currentQuota == previousStartingQuota;

    public static bool ShouldResetInitialMoney(
        long previousStartingMoney,
        long currentMoney,
        int currentFloor,
        int daysPassed,
        int successfulQuota) =>
        ShouldResetUntouchedPreDayState(currentFloor, daysPassed, successfulQuota) &&
        currentMoney == previousStartingMoney;
}
