using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;

namespace QT.PizzaOrdering.Core.Features.Orders;

/// <summary>
/// Prices a whole order: every pizza, then tax and the delivery fee.
/// </summary>
public static class OrderPricer
{
    public const int MaxPizzaLines = 20;

    public static Result<OrderQuote> Quote(
        MenuCatalog menu,
        IReadOnlyList<PizzaSelection> pizzas,
        FulfillmentType fulfillment,
        OrderPricingRules rules)
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

        if (failures.Count > 0)
            return Error.Validation.Of(failures);

        decimal subtotal = priced.Sum(pizza => pizza.LineTotal);
        decimal tax = Math.Round(subtotal * rules.TaxRate, 2, MidpointRounding.AwayFromZero);
        decimal deliveryFee = fulfillment == FulfillmentType.Delivery ? rules.DeliveryFee : 0m;

        return new OrderQuote(priced, subtotal, tax, deliveryFee, subtotal + tax + deliveryFee);
    }
}
