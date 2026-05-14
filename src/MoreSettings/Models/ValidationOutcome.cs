namespace MoreSettings.Models;

public enum ValidationStatus
{
    Valid,
    Warning,
    Error
}

public sealed class ValidationOutcome
{
    public ValidationOutcome(string targetField, ValidationStatus status, string message)
    {
        TargetField = targetField;
        Status = status;
        Message = message;
    }

    public string TargetField { get; }

    public ValidationStatus Status { get; }

    public string Message { get; }

    public static ValidationOutcome Valid(string targetField, string message)
    {
        return new ValidationOutcome(targetField, ValidationStatus.Valid, message);
    }

    public static ValidationOutcome Warning(string targetField, string message)
    {
        return new ValidationOutcome(targetField, ValidationStatus.Warning, message);
    }

    public static ValidationOutcome Error(string targetField, string message)
    {
        return new ValidationOutcome(targetField, ValidationStatus.Error, message);
    }
}
