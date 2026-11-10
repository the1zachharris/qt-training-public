using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;

namespace QT.PizzaOrdering.Core.Features.Orders.Models;

/// <summary>
/// A placed pickup order. Aggregate root: lines are only ever created through <see cref="Place"/>,
/// and the status only moves through <see cref="MarkReady"/>.
/// </summary>
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }

    /// <summary>Identifies the order in URLs. Not guessable, unlike <see cref="OrderNumber"/>.</summary>
    public Guid Id { get; private set; }

    /// <summary>The short number staff and customers say out loud. 0 until the repository assigns one.</summary>
    public int OrderNumber { get; private set; }

    public string CustomerName { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines;

    /// <summary>
    /// Creates a New order from a validated request and the pizzas priced for it. The priced
    /// pizzas, not the request, are the source of every name and amount on the order. The total
    /// is the sum of the pizzas: the shop charges no tax or fees through the application.
    /// </summary>
    public static Order Place(TimeProvider timeProvider, PlaceOrderRequest request, IReadOnlyList<PricedPizza> pizzas)
    {
        if (pizzas.Count == 0)
            throw new ArgumentException("An order needs at least one pizza.", nameof(pizzas));

        Order order = new()
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName.Trim(),
            Status = OrderStatus.New,
            PlacedAt = timeProvider.GetUtcNow(),
            Total = pizzas.Sum(pizza => pizza.LineTotal)
        };
        order._lines.AddRange(pizzas.Select(OrderLine.From));
        return order;
    }

    /// <summary>
    /// Sets the order number. The repository may call this again with a different number when
    /// another order claimed the first one before this order was saved.
    /// </summary>
    public void AssignOrderNumber(int orderNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(orderNumber, 1);
        OrderNumber = orderNumber;
    }

    /// <summary>
    /// Marks the order prepared. Marking an order that is already Ready changes nothing, so two
    /// kitchen screens acting on the same order do not disturb the time it was first marked.
    /// </summary>
    public void MarkReady(TimeProvider timeProvider)
    {
        if (Status == OrderStatus.Ready)
            return;

        Status = OrderStatus.Ready;
        ReadyAt = timeProvider.GetUtcNow();
    }
}
