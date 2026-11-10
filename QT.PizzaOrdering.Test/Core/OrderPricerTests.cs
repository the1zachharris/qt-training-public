using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.Infrastructure.Seeding;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Core;

public class OrderPricerTests
{
    private readonly MenuCatalog _menu = MenuSeed.Create();

    [Fact]
    public void Price_TwoDifferentPizzas_PricesEachLine()
    {
        Result<IReadOnlyList<PricedPizza>> result = OrderPricer.Price(
            _menu,
            [Pizzas.Custom(Pizzas.Small, toppingIds: [Pizzas.Pepperoni]), Pizzas.Custom(Pizzas.Large, toppingIds: Pizzas.AllToppings)]);

        Assert.Equal(2, result.Value.Count);
        Assert.Equal(12.00m + 22.00m, result.Value.Sum(pizza => pizza.LineTotal));
    }

    [Fact]
    public void Price_InvalidPizza_ReportsItByPosition()
    {
        Result<IReadOnlyList<PricedPizza>> result = OrderPricer.Price(
            _menu,
            [Pizzas.Custom(), Pizzas.Custom(sizeId: 99)]);

        Error.Validation validation = Assert.IsType<Error.Validation>(result.Error);
        Assert.Equal(["Pizzas[1].SizeId"], validation.Failures.Keys);
    }
}
