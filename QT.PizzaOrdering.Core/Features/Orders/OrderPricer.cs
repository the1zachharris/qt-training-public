using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;

namespace QT.PizzaOrdering.Core.Features.Orders;

/// <summary>
/// Prices every pizza on an order. The order total is simply the sum of the lines: there is no
/// tax and there are no fees.
/// </summary>
public static class OrderPricer
{
    public const int MaxPizzaLines = 20;

    public static Result<IReadOnlyList<PricedPizza>> Price(MenuCatalog menu, IReadOnlyList<PizzaSelection> pizzas)
    {
        Dictionary<string, string[]> failures = [];
        List<PricedPizza> priced = new(pizzas.Count);

        for (int index = 0; index < pizzas.Count; index++)
        {
            Result<PricedPizza> result = PizzaPricer.Price(menu, pizzas[index], $"Pizzas[{index}]");
            if (result.IsSuccess)
            {
                priced.Add(result.Value);
            }
            else if (result.Error is Error.Validation validation)
            {
                foreach ((string key, string[] messages) in validation.Failures)
                    failures[key] = messages;
            }
        }

        return failures.Count > 0
            ? Error.Validation.Of(failures)
            : priced;
    }
}
