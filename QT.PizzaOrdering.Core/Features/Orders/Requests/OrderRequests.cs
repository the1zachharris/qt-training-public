using QT.PizzaOrdering.Core.Features.Menu.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Enums;

namespace QT.PizzaOrdering.Core.Features.Orders.Requests;

/// <summary>
/// One pizza as the customer chose it, by menu id. Carries no names or prices: those are always
/// resolved from the menu by <see cref="PizzaPricer"/>.
/// </summary>
/// <param name="PresetPizzaId">The preset this pizza started from, or null for a build-your-own.</param>
public sealed record PizzaSelection(
    int? PresetPizzaId,
    int SizeId,
    int CrustId,
    int SauceId,
    CheeseLevel Cheese,
    IReadOnlyList<int> ToppingIds,
    int Quantity);

public sealed record QuoteOrderRequest(
    FulfillmentType Fulfillment,
    IReadOnlyList<PizzaSelection> Pizzas);

public sealed record PlaceOrderRequest(
    string CustomerName,
    string Phone,
    FulfillmentType Fulfillment,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<PizzaSelection> Pizzas);
