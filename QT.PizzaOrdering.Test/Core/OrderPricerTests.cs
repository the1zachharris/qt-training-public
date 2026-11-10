using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.Infrastructure.Seeding;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Core;

public class OrderPricerTests
{
    private static readonly OrderPricingRules Rules = new(TaxRate: 0.085m, DeliveryFee: 3.99m);
    private readonly MenuCatalog _menu = MenuSeed.Create();

    [Fact]
    public void Quote_WhenPickup_TotalsSubtotalAndTaxWithoutDeliveryFee()
    {
        Result<OrderQuote> result = OrderPricer.Quote(_menu, [Pizzas.MediumPepperoni(2)], FulfillmentType.Pickup, Rules);

        Assert.Equal(28.98m, result.Value.Subtotal);
        Assert.Equal(2.46m, result.Value.Tax);
        Assert.Equal(0m, result.Value.DeliveryFee);
        Assert.Equal(31.44m, result.Value.Total);
    }

    [Fact]
    public void Quote_WhenDelivery_AddsUntaxedDeliveryFee()
    {
        Result<OrderQuote> result = OrderPricer.Quote(_menu, [Pizzas.MediumPepperoni(2)], FulfillmentType.Delivery, Rules);

        Assert.Equal(2.46m, result.Value.Tax);
        Assert.Equal(3.99m, result.Value.DeliveryFee);
        Assert.Equal(35.43m, result.Value.Total);
    }

    [Fact]
    public void Quote_WhenSeveralPizzas_SumsEveryLine()
    {
        Result<OrderQuote> result = OrderPricer.Quote(
            _menu,
            [Pizzas.MediumPepperoni(), Pizzas.BuildYourOwn(Pizzas.Large)],
            FulfillmentType.Pickup,
            Rules);

        Assert.Equal(2, result.Value.Pizzas.Count);
        Assert.Equal(14.49m + 15.99m, result.Value.Subtotal);
    }

    [Fact]
    public void Quote_WhenAPizzaIsInvalid_ReportsItByPosition()
    {
        Result<OrderQuote> result = OrderPricer.Quote(
            _menu,
            [Pizzas.MediumPepperoni(), Pizzas.BuildYourOwn(sizeId: 99)],
            FulfillmentType.Pickup,
            Rules);

        Error.Validation validation = Assert.IsType<Error.Validation>(result.Error);
        Assert.Equal(["Pizzas[1].SizeId"], validation.Failures.Keys);
    }
}
