using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Infrastructure.Seeding;
using QT.PizzaOrdering.Test.Fakes;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Core;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 18, 30, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _timeProvider = new(Now);

    private static OrderQuote QuoteFor(PlaceOrderRequest request) =>
        OrderPricer.Quote(MenuSeed.Create(), request.Pizzas, request.Fulfillment, new OrderPricingRules(0.085m, 3.99m)).Value;

    [Fact]
    public void Place_WhenValid_CopiesTheQuoteOntoTheOrder()
    {
        PlaceOrderRequest request = Pizzas.DeliveryOrder(Pizzas.MediumPepperoni(2));
        OrderQuote quote = QuoteFor(request);

        Order order = Order.Place(_timeProvider, request, quote);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.StartsWith("PZ-", order.OrderNumber);
        Assert.Equal(Now, order.PlacedAt);
        Assert.Equal(quote.Total, order.Total);
        OrderLine line = Assert.Single(order.Lines);
        Assert.Equal("Pepperoni", line.Name);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(28.98m, line.LineTotal);
    }

    [Fact]
    public void Place_WhenPickup_DropsAnyDeliveryAddress()
    {
        PlaceOrderRequest request = Pizzas.PickupOrder(Pizzas.MediumPepperoni()) with { DeliveryAddress = "100 Example St" };

        Order order = Order.Place(_timeProvider, request, QuoteFor(request));

        Assert.Equal(FulfillmentType.Pickup, order.Fulfillment);
        Assert.Null(order.DeliveryAddress);
    }

    [Fact]
    public void Place_WhenTextHasPadding_TrimsItAndStoresBlankNotesAsNull()
    {
        PlaceOrderRequest request = Pizzas.PickupOrder(Pizzas.MediumPepperoni()) with { CustomerName = "  Pat Sample ", Notes = "   " };

        Order order = Order.Place(_timeProvider, request, QuoteFor(request));

        Assert.Equal("Pat Sample", order.CustomerName);
        Assert.Null(order.Notes);
    }

    [Fact]
    public void Place_WhenQuoteHasNoPizzas_Throws()
    {
        PlaceOrderRequest request = Pizzas.PickupOrder();
        OrderQuote emptyQuote = new([], 0m, 0m, 0m, 0m);

        Assert.Throws<ArgumentException>(() => Order.Place(_timeProvider, request, emptyQuote));
    }
}
