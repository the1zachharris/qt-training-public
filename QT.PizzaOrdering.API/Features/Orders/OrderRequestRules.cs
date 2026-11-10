using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.Core.Shared.Validation;

namespace QT.PizzaOrdering.API.Features.Orders;

/// <summary>
/// Request-shape rules shared by the quote and place-order validators. Whether a pizza is valid
/// against the menu is not decided here: <see cref="PizzaPricer"/> owns that.
/// </summary>
internal static class OrderRequestRules
{
    public static void ValidateFulfillment(ValidationResult result, FulfillmentType fulfillment)
    {
        result.AddErrorIf(!Enum.IsDefined(fulfillment), nameof(QuoteOrderRequest.Fulfillment), "Fulfillment must be Pickup or Delivery.");
    }

    public static void ValidatePizzas(ValidationResult result, IReadOnlyList<PizzaSelection>? pizzas)
    {
        const string key = nameof(QuoteOrderRequest.Pizzas);

        if (pizzas is null || pizzas.Count == 0)
        {
            result.AddError(key, "The cart is empty.");
            return;
        }

        result.AddErrorIf(pizzas.Count > OrderPricer.MaxPizzaLines, key, $"An order can have at most {OrderPricer.MaxPizzaLines} different pizzas.");
        result.AddErrorIf(pizzas.Any(pizza => pizza is null), key, "A pizza in the cart is missing.");
    }
}

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
