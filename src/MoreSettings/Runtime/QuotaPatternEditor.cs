using System;
using System.Collections.Generic;

namespace MoreSettings.Runtime;

public static class QuotaPatternEditor
{
    public static float[] BuildEditableValues(IReadOnlyList<float> sourceValues, int slotCount)
    {
        if (slotCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slotCount));
        }

        var editableValues = new float[slotCount];
        var fallbackValue = sourceValues is { Count: > 0 }
            ? sourceValues[sourceValues.Count - 1]
            : 1f;

        for (var index = 0; index < slotCount; index++)
        {
            editableValues[index] = index < sourceValues.Count
                ? sourceValues[index]
                : fallbackValue;
        }

        return editableValues;
    }

    public static float[] BuildPattern(IReadOnlyList<float> editableValues, int desiredLength)
    {
        if (editableValues == null)
        {
            throw new ArgumentNullException(nameof(editableValues));
        }

        if (editableValues.Count == 0)
        {
            return Array.Empty<float>();
        }

        var length = Math.Max(1, Math.Min(desiredLength, editableValues.Count));
        var pattern = new float[length];
        for (var index = 0; index < length; index++)
        {
            pattern[index] = editableValues[index];
        }

        return pattern;
    }
}
