using QT.PizzaOrdering.Core.Features.Menu.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Models;

namespace QT.PizzaOrdering.Core.Features.Orders.Responses;

/// <summary>
/// A pizza resolved against the menu: display names plus its price.
/// </summary>
public sealed record PricedPizza(
    string Name,
    string Size,
    string Crust,
    string Sauce,
    CheeseLevel Cheese,
    IReadOnlyList<string> Toppings,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderQuote(
    IReadOnlyList<PricedPizza> Pizzas,
    decimal Subtotal,
    decimal Tax,
    decimal DeliveryFee,
    decimal Total);

/// <summary>
/// A placed order as shown back to the customer. Leaves the phone number out on purpose: the
/// confirmation page has no use for it.
/// </summary>
public sealed record OrderConfirmation(
    Guid Id,
    string OrderNumber,
    DateTimeOffset PlacedAt,
    string CustomerName,
    FulfillmentType Fulfillment,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<PricedPizza> Pizzas,
    decimal Subtotal,
    decimal Tax,
    decimal DeliveryFee,
    decimal Total)
{
    public static OrderConfirmation From(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.PlacedAt,
        order.CustomerName,
        order.Fulfillment,
        order.DeliveryAddress,
        order.Notes,
        [.. order.Lines.Select(line => line.ToPricedPizza())],
        order.Subtotal,
        order.Tax,
        order.DeliveryFee,
        order.Total);
}
