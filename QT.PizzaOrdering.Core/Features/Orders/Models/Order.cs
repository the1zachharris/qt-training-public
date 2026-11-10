using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;

namespace QT.PizzaOrdering.Core.Features.Orders.Models;

/// <summary>
/// A placed order. Aggregate root: lines are only ever created through <see cref="Place"/>.
/// </summary>
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order() { }

    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public FulfillmentType Fulfillment { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Tax { get; private set; }
    public decimal DeliveryFee { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines;

    /// <summary>
    /// Creates an order from a validated request and the quote priced for it. The quote, not the
    /// request, is the source of every name and amount on the order.
    /// </summary>
    public static Order Place(TimeProvider timeProvider, PlaceOrderRequest request, OrderQuote quote)
    {
        if (quote.Pizzas.Count == 0)
            throw new ArgumentException("An order needs at least one pizza.", nameof(quote));

        Guid id = Guid.NewGuid();
        Order order = new()
        {
            Id = id,
            OrderNumber = $"PZ-{id.ToString("N")[..6].ToUpperInvariant()}",
            CustomerName = request.CustomerName.Trim(),
            Phone = request.Phone.Trim(),
            Fulfillment = request.Fulfillment,
            // An address typed before switching to pickup is not part of the order.
            DeliveryAddress = request.Fulfillment == FulfillmentType.Delivery ? NullIfBlank(request.DeliveryAddress) : null,
            Notes = NullIfBlank(request.Notes),
            PlacedAt = timeProvider.GetUtcNow(),
            Subtotal = quote.Subtotal,
            Tax = quote.Tax,
            DeliveryFee = quote.DeliveryFee,
            Total = quote.Total
        };
        order._lines.AddRange(quote.Pizzas.Select(OrderLine.From));
        return order;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
