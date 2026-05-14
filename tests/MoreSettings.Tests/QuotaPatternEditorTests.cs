using MoreSettings.Runtime;

namespace MoreSettings.Tests;

public sealed class QuotaPatternEditorTests
{
    [Fact]
    public void BuildEditableValues_PadsWithLastKnownValue()
    {
        var editableValues = QuotaPatternEditor.BuildEditableValues(new[] { 1.2f, 1.5f }, 4);

        Assert.Equal(new[] { 1.2f, 1.5f, 1.5f, 1.5f }, editableValues);
    }

    [Fact]
    public void BuildPattern_ClampsLengthToAvailableSlots()
    {
        var pattern = QuotaPatternEditor.BuildPattern(new[] { 1.1f, 1.3f, 1.6f }, 5);

        Assert.Equal(new[] { 1.1f, 1.3f, 1.6f }, pattern);
    }
}
