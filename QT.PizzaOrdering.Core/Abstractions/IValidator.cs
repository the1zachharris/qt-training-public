using QT.PizzaOrdering.Core.Shared.Validation;

namespace QT.PizzaOrdering.Core.Abstractions;

/// <summary>
/// Defines validation behavior for a specific request type.
/// </summary>
/// <typeparam name="T">The request type to validate.</typeparam>
public interface IValidator<T>
{
    /// <summary>
    /// Validates the request asynchronously.
    /// </summary>
    ValueTask<ValidationResult> ValidateAsync(T request, CancellationToken ct = default);
}
