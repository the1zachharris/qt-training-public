using System.Globalization;

using QT.PizzaOrdering.Core.Features.Menu.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Responses;

namespace QT.PizzaOrdering.UI.Shared;

/// <summary>
/// How pizzas and money read on screen, so every page words them the same way.
/// </summary>
public static class PizzaText
{
    private static readonly CultureInfo UnitedStates = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Formats as US dollars regardless of the server's culture.</summary>
    public static string ToCurrency(this decimal amount) => amount.ToString("C2", UnitedStates);

    public static string Cheese(CheeseLevel cheese) => cheese switch
    {
        CheeseLevel.None => "No cheese",
        CheeseLevel.Light => "Light cheese",
        CheeseLevel.Regular => "Regular cheese",
        CheeseLevel.Extra => "Extra cheese",
        _ => cheese.ToString()
    };

    /// <summary>Size, crust, sauce, and cheese on one line.</summary>
    public static string Build(PricedPizza pizza) =>
        $"{pizza.Size} · {pizza.Crust} · {pizza.Sauce} · {Cheese(pizza.Cheese)}";

    public static string Toppings(PricedPizza pizza) =>
        pizza.Toppings.Count == 0 ? "No toppings" : string.Join(", ", pizza.Toppings);
}
