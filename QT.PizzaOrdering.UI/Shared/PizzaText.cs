using System.Globalization;

using QT.PizzaOrdering.Core.Features.Orders.Responses;

namespace QT.PizzaOrdering.UI.Shared;

/// <summary>
/// How pizzas, order numbers, and money read on screen, so every page words them the same way.
/// </summary>
public static class PizzaText
{
    private static readonly CultureInfo UnitedStates = CultureInfo.GetCultureInfo("en-US");

    /// <summary>Formats as US dollars regardless of the server's culture.</summary>
    public static string ToCurrency(this decimal amount) => amount.ToString("C2", UnitedStates);

    /// <summary>The order number the way staff and customers say it: <c>#1001</c>.</summary>
    public static string OrderNumber(int orderNumber) => $"#{orderNumber}";

    public static string Toppings(PricedPizza pizza) =>
        pizza.Toppings.Count == 0 ? "Plain cheese" : string.Join(", ", pizza.Toppings);

    /// <summary>Size and toppings on one line: <c>Large (14") · Pepperoni, Mushrooms</c>.</summary>
    public static string Details(PricedPizza pizza) => $"{pizza.Size} · {Toppings(pizza)}";
}
