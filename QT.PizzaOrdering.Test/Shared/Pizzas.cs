using QT.PizzaOrdering.Core.Features.Orders.Requests;

namespace QT.PizzaOrdering.Test.Shared;

/// <summary>
/// Selections against the seeded menu (<c>MenuSeed</c>): Small $10.00, Large $14.00, every topping $2.00.
/// </summary>
internal static class Pizzas
{
    public const int Small = 1;
    public const int Large = 2;

    public const int Pepperoni = 1;
    public const int Mushrooms = 2;
    public const int Onions = 3;
    public const int GreenPeppers = 4;

    public const int VeggiePreset = 3;

    public static readonly int[] AllToppings = [Pepperoni, Mushrooms, Onions, GreenPeppers];

    /// <summary>A pizza built from scratch, with no preset behind it.</summary>
    public static PizzaSelection Custom(int sizeId = Small, int quantity = 1, params int[] toppingIds) =>
        new(null, sizeId, toppingIds, quantity);

    /// <summary>The Veggie preset exactly as the menu defines it: mushrooms, onions, green peppers.</summary>
    public static PizzaSelection Veggie(int sizeId = Small) =>
        new(VeggiePreset, sizeId, [Mushrooms, Onions, GreenPeppers], 1);

    public static PlaceOrderRequest Order(params PizzaSelection[] pizzas) => new("Pat Sample", pizzas);
}
