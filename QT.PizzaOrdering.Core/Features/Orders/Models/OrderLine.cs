using QT.PizzaOrdering.Core.Features.Orders.Responses;

namespace QT.PizzaOrdering.Core.Features.Orders.Models;

/// <summary>
/// One pizza on a placed order. Stores names and prices as they were when the order was placed,
/// so later menu changes never rewrite a past order.
/// </summary>
public sealed class OrderLine
{
    private OrderLine() { }

    public int Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Size { get; private set; } = string.Empty;
    public List<string> Toppings { get; private set; } = [];
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }

    internal static OrderLine From(PricedPizza pizza) => new()
    {
        Name = pizza.Name,
        Size = pizza.Size,
        Toppings = [.. pizza.Toppings],
        Quantity = pizza.Quantity,
        UnitPrice = pizza.UnitPrice,
        LineTotal = pizza.LineTotal
    };

    public PricedPizza ToPricedPizza() =>
        new(Name, Size, [.. Toppings], Quantity, UnitPrice, LineTotal);
}
