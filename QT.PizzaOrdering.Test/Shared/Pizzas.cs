using QT.PizzaOrdering.Core.Features.Menu.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;

namespace QT.PizzaOrdering.Test.Shared;

/// <summary>
/// Selections against the seeded menu (<c>MenuSeed</c>), with the prices they should come to.
/// </summary>
internal static class Pizzas
{
    public const int Medium = 2;
    public const int Large = 3;
    public const int HandTossed = 1;
    public const int DeepDish = 3;
    public const int Marinara = 1;
    public const int Pepperoni = 1;
    public const int PepperoniPreset = 2;

    /// <summary>Medium (12.99) + one topping (1.50) = 14.49 each.</summary>
    public static PizzaSelection MediumPepperoni(int quantity = 1) =>
        new(PepperoniPreset, Medium, HandTossed, Marinara, CheeseLevel.Regular, [Pepperoni], quantity);

    public static PizzaSelection BuildYourOwn(int sizeId = Medium, int crustId = HandTossed, CheeseLevel cheese = CheeseLevel.Regular, int quantity = 1, params int[] toppingIds) =>
        new(null, sizeId, crustId, Marinara, cheese, toppingIds, quantity);

    public static PlaceOrderRequest PickupOrder(params PizzaSelection[] pizzas) =>
        new("Pat Sample", "918-555-0100", FulfillmentType.Pickup, null, null, pizzas);

    public static PlaceOrderRequest DeliveryOrder(params PizzaSelection[] pizzas) =>
        new("Pat Sample", "918-555-0100", FulfillmentType.Delivery, "100 Example St", "Leave at the door", pizzas);
}
