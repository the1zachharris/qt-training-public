using QT.PizzaOrdering.Core.Shared;

namespace QT.PizzaOrdering.API.Features.Orders;

public static class ErrorExtensions
{
    /// <summary>
    /// The field-level failures of an <see cref="Error"/>, in the shape a validation problem response takes.
    /// </summary>
    public static Dictionary<string, string[]> ToValidationErrors(this Error error) => error switch
    {
        Error.Validation validation => new Dictionary<string, string[]>(validation.Failures),
        _ => new Dictionary<string, string[]> { [error.Code] = [error.Message] }
    };
}
