using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Models;

namespace QT.PizzaOrdering.Core.Features.Orders.Responses;

/// <summary>
/// A pizza resolved against the menu: display names plus its price.
/// </summary>
public sealed record PricedPizza(
    string Name,
    string Size,
    IReadOnlyList<string> Toppings,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>
/// A placed order, as the customer's confirmation and the kitchen both show it.
/// </summary>
public sealed record OrderDetails(
    Guid Id,
    int OrderNumber,
    string CustomerName,
    DateTimeOffset PlacedAt,
    OrderStatus Status,
    DateTimeOffset? ReadyAt,
    IReadOnlyList<PricedPizza> Pizzas,
    decimal Total)
{
    public static OrderDetails From(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.CustomerName,
        order.PlacedAt,
        order.Status,
        order.ReadyAt,
        [.. order.Lines.Select(line => line.ToPricedPizza())],
        order.Total);
}

/// <summary>
/// What the kitchen screen shows: orders still to prepare, and orders waiting to be picked up.
/// </summary>
public sealed record KitchenOrders(
    IReadOnlyList<OrderDetails> New,
    IReadOnlyList<OrderDetails> Ready);
