using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using QT.PizzaOrdering.API.Features.Orders;
using QT.PizzaOrdering.Core.Features.Menu.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Infrastructure.Seeding;
using QT.PizzaOrdering.Test.Fakes;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Handlers;

public class PlaceOrderHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 18, 30, 0, TimeSpan.Zero);

    private readonly IMenuQueries _menuQueries = Substitute.For<IMenuQueries>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly PlaceOrderHandler _handler;

    public PlaceOrderHandlerTests()
    {
        _menuQueries.GetMenuAsync(Arg.Any<CancellationToken>()).Returns(MenuSeed.Create());

        _handler = new PlaceOrderHandler(
            NullLogger<PlaceOrderHandler>.Instance,
            new PlaceOrderRequestValidator(),
            _menuQueries,
            _orderRepository,
            new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenValid_SavesANewOrderPricedFromTheMenu()
    {
        // 2 x (Large 14.00 + Pepperoni 2.00 + Mushrooms 2.00) = 36.00, with no tax or fees on top
        PlaceOrderRequest request = Pizzas.Order(Pizzas.Custom(Pizzas.Large, quantity: 2, toppingIds: [Pizzas.Pepperoni, Pizzas.Mushrooms]));

        var response = await _handler.PlaceOrderAsync(request, TestContext.Current.CancellationToken);

        OrderDetails placed = Assert.IsType<Ok<OrderDetails>>(response.Result).Value!;
        Assert.Equal("Pat Sample", placed.CustomerName);
        Assert.Equal(OrderStatus.New, placed.Status);
        Assert.Equal(36.00m, placed.Total);
        Assert.Equal(Now, placed.PlacedAt);
        await _orderRepository.Received(1).AddAsync(
            Arg.Is<Order>(order => order.Id == placed.Id && order.Total == 36.00m && order.Status == OrderStatus.New),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrderAsync_WithoutAName_ReturnsValidationProblemAndSavesNothing()
    {
        PlaceOrderRequest request = Pizzas.Order(Pizzas.Custom()) with { CustomerName = "" };

        var response = await _handler.PlaceOrderAsync(request, TestContext.Current.CancellationToken);

        ValidationProblem problem = Assert.IsType<ValidationProblem>(response.Result);
        Assert.Contains(nameof(PlaceOrderRequest.CustomerName), problem.ProblemDetails.Errors.Keys);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrderAsync_WithAnEmptyOrder_ReturnsValidationProblemAndSavesNothing()
    {
        var response = await _handler.PlaceOrderAsync(Pizzas.Order(), TestContext.Current.CancellationToken);

        ValidationProblem problem = Assert.IsType<ValidationProblem>(response.Result);
        Assert.Contains(nameof(PlaceOrderRequest.Pizzas), problem.ProblemDetails.Errors.Keys);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenPizzaIsNotOnTheMenu_ReturnsValidationProblemAndSavesNothing()
    {
        PlaceOrderRequest request = Pizzas.Order(Pizzas.Custom(sizeId: 99));

        var response = await _handler.PlaceOrderAsync(request, TestContext.Current.CancellationToken);

        ValidationProblem problem = Assert.IsType<ValidationProblem>(response.Result);
        Assert.Contains("Pizzas[0].SizeId", problem.ProblemDetails.Errors.Keys);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }
}
