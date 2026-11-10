namespace QT.PizzaOrdering.Core.Features.Orders.Requests;

/// <summary>
/// One pizza as the customer chose it, by menu id. Carries no names or prices: those are always
/// resolved from the menu by <see cref="PizzaPricer"/>.
/// </summary>
/// <param name="PresetPizzaId">The preset this pizza started from, or null when built from scratch.</param>
public sealed record PizzaSelection(
    int? PresetPizzaId,
    int SizeId,
    IReadOnlyList<int> ToppingIds,
    int Quantity);

/// <summary>
/// A pickup order. The customer's name is all the shop asks for; payment happens at the counter.
/// </summary>
public sealed record PlaceOrderRequest(
    string CustomerName,
    IReadOnlyList<PizzaSelection> Pizzas);
