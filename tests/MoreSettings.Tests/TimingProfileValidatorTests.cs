using MoreSettings.Configuration;
using MoreSettings.Models;

namespace MoreSettings.Tests;

public sealed class TimingProfileValidatorTests
{
    [Fact]
    public void Validate_ReturnsValidOutcome_ForExpectedVanillaLikeValues()
    {
        var profile = new TimingProfile(
            "Test",
            false,
            300f,
            3,
            100L,
            0.75f,
            QuotaScalingMode.CustomPattern,
            new[] { 1.2f, 1.3f, 1.5f });

        var outcomes = TimingProfileValidator.Validate(profile);

        Assert.Contains(outcomes, outcome => outcome.Status == ValidationStatus.Valid);
        Assert.DoesNotContain(outcomes, outcome => outcome.Status == ValidationStatus.Error);
    }

    [Fact]
    public void Validate_ReturnsError_WhenDayDurationIsOutOfRange()
    {
        var profile = new TimingProfile(
            "BadDay",
            false,
            5f,
            3,
            100L,
            0.75f,
            QuotaScalingMode.CustomPattern,
            new[] { 1.2f, 1.3f });

        var outcomes = TimingProfileValidator.Validate(profile);

        Assert.Contains(
            outcomes,
            outcome => outcome.Status == ValidationStatus.Error &&
                       outcome.TargetField == nameof(profile.DayDurationSeconds));
    }

    [Fact]
    public void TryParseQuotaMultipliers_RejectsInvalidToken()
    {
        var success = TimingProfileValidator.TryParseQuotaMultipliers(
            "1.2,not-a-number,1.4",
            out var values,
            out var error);

        Assert.False(success);
        Assert.Empty(values);
        Assert.NotNull(error);
        Assert.Equal(ValidationStatus.Error, error!.Status);
    }

    [Fact]
    public void Validate_AllowsVanillaScaling_WithoutCustomPattern()
    {
        var profile = new TimingProfile(
            "VanillaScaling",
            false,
            300f,
            3,
            100L,
            0.75f,
            QuotaScalingMode.Vanilla,
            Array.Empty<float>());

        var outcomes = TimingProfileValidator.Validate(profile);

        Assert.Contains(outcomes, outcome => outcome.Status == ValidationStatus.Valid);
        Assert.DoesNotContain(outcomes, outcome => outcome.Status == ValidationStatus.Error);
    }
}
