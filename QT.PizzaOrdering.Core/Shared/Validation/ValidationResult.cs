namespace QT.PizzaOrdering.Core.Shared.Validation;

/// <summary>
/// Contains the result of a validation operation.
/// </summary>
public class ValidationResult
{
    private readonly Dictionary<string, string[]> _errors = [];

    /// <summary>
    /// Indicates whether the validation passed with no errors.
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Dictionary of validation errors keyed by property name.
    /// </summary>
    public Dictionary<string, string[]> Errors => _errors;

    /// <summary>
    /// Adds a validation error for a specific property.
    /// </summary>
    public void AddError(string key, string message)
    {
        _errors[key] = _errors.TryGetValue(key, out string[]? existing)
            ? [.. existing, message]
            : [message];
    }

    /// <summary>
    /// Conditionally adds a validation error based on a condition.
    /// </summary>
    public void AddErrorIf(bool condition, string key, string message)
    {
        if (condition) AddError(key, message);
    }

    /// <summary>
    /// Creates a successful validation result with no errors.
    /// </summary>
    public static ValidationResult Success() => new();
}
