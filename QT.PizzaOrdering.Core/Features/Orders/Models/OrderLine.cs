using QT.PizzaOrdering.Core.Features.Menu.Enums;
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
    public string Crust { get; private set; } = string.Empty;
    public string Sauce { get; private set; } = string.Empty;
    public CheeseLevel Cheese { get; private set; }
    public List<string> Toppings { get; private set; } = [];
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }

    internal static OrderLine From(PricedPizza pizza) => new()
    {
        Name = pizza.Name,
        Size = pizza.Size,
        Crust = pizza.Crust,
        Sauce = pizza.Sauce,
        Cheese = pizza.Cheese,
        Toppings = [.. pizza.Toppings],
        Quantity = pizza.Quantity,
        UnitPrice = pizza.UnitPrice,
        LineTotal = pizza.LineTotal
    };

    public PricedPizza ToPricedPizza() =>
        new(Name, Size, Crust, Sauce, Cheese, [.. Toppings], Quantity, UnitPrice, LineTotal);
}
