using MoreSettings.Runtime;

namespace MoreSettings.Tests;

public sealed class QuotaRuntimeStatePlannerTests
{
    [Fact]
    public void ComputeRemainingDays_SubtractsElapsedDays()
    {
        var remainingDays = QuotaRuntimeStatePlanner.ComputeRemainingDays(5, 2);

        Assert.Equal(3, remainingDays);
    }

    [Fact]
    public void ShouldResetInitialQuota_ReturnsTrue_WhenStillOnInitialQuota()
    {
        var shouldReset = QuotaRuntimeStatePlanner.ShouldResetInitialQuota(
            previousStartingQuota: 120L,
            currentQuota: 120L,
            requiredQuota: 120L,
            daysPassed: 0,
            successfulQuota: 0);

        Assert.True(shouldReset);
    }

    [Fact]
    public void ShouldResetInitialQuota_ReturnsFalse_AfterProgressBegins()
    {
        var shouldReset = QuotaRuntimeStatePlanner.ShouldResetInitialQuota(
            previousStartingQuota: 120L,
            currentQuota: 120L,
            requiredQuota: 120L,
            daysPassed: 1,
            successfulQuota: 0);

        Assert.False(shouldReset);
    }
}