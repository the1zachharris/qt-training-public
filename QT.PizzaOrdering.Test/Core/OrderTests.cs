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
    private static readonly DateTimeOffset PlacedTime = new(2026, 11, 10, 18, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ReadyTime = PlacedTime.AddMinutes(20);

    private static IReadOnlyList<PricedPizza> PriceOf(PlaceOrderRequest request) =>
        OrderPricer.Price(MenuSeed.Create(), request.Pizzas).Value;

    private static Order PlaceOrder(PlaceOrderRequest request) =>
        Order.Place(new FakeTimeProvider(PlacedTime), request, PriceOf(request));

    [Fact]
    public void Place_StartsAsNew_WithTheSumOfThePizzasAsTheTotal()
    {
        // Small + 1 topping (12.00) and Large + 4 toppings (22.00)
        PlaceOrderRequest request = Pizzas.Order(
            Pizzas.Custom(Pizzas.Small, toppingIds: [Pizzas.Pepperoni]),
            Pizzas.Custom(Pizzas.Large, toppingIds: Pizzas.AllToppings));

        Order order = PlaceOrder(request);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(OrderStatus.New, order.Status);
        Assert.Null(order.ReadyAt);
        Assert.Equal(PlacedTime, order.PlacedAt);
        Assert.Equal(34.00m, order.Total);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(["Pepperoni"], order.Lines[0].Toppings);
    }

    [Fact]
    public void Place_TrimsTheCustomerName()
    {
        Order order = PlaceOrder(Pizzas.Order(Pizzas.Custom()) with { CustomerName = "  Pat Sample " });

        Assert.Equal("Pat Sample", order.CustomerName);
    }

    [Fact]
    public void Place_WithNoPizzas_Throws()
    {
        Assert.Throws<ArgumentException>(() => Order.Place(new FakeTimeProvider(PlacedTime), Pizzas.Order(), []));
    }

    [Fact]
    public void MarkReady_SetsReadyAndTheTime()
    {
        Order order = PlaceOrder(Pizzas.Order(Pizzas.Custom()));

        order.MarkReady(new FakeTimeProvider(ReadyTime));

        Assert.Equal(OrderStatus.Ready, order.Status);
        Assert.Equal(ReadyTime, order.ReadyAt);
    }

    [Fact]
    public void MarkReady_WhenAlreadyReady_KeepsTheFirstTime()
    {
        Order order = PlaceOrder(Pizzas.Order(Pizzas.Custom()));
        order.MarkReady(new FakeTimeProvider(ReadyTime));

        order.MarkReady(new FakeTimeProvider(ReadyTime.AddMinutes(5)));

        Assert.Equal(OrderStatus.Ready, order.Status);
        Assert.Equal(ReadyTime, order.ReadyAt);
    }

    [Fact]
    public void AssignOrderNumber_SetsTheNumber()
    {
        Order order = PlaceOrder(Pizzas.Order(Pizzas.Custom()));

        order.AssignOrderNumber(1001);

        Assert.Equal(1001, order.OrderNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AssignOrderNumber_BelowOne_Throws(int orderNumber)
    {
        Order order = PlaceOrder(Pizzas.Order(Pizzas.Custom()));

        Assert.Throws<ArgumentOutOfRangeException>(() => order.AssignOrderNumber(orderNumber));
    }
}
