using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using QT.PizzaOrdering.API.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Infrastructure.Seeding;
using QT.PizzaOrdering.Test.Fakes;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Handlers;

public class MarkOrderReadyHandlerTests
{
    private static readonly DateTimeOffset PlacedTime = new(2026, 11, 10, 18, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ReadyTime = PlacedTime.AddMinutes(20);

    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly MarkOrderReadyHandler _handler;

    public MarkOrderReadyHandlerTests()
    {
        _handler = new MarkOrderReadyHandler(NullLogger<MarkOrderReadyHandler>.Instance, _orderRepository, new FakeTimeProvider(ReadyTime));
    }

    private Order ExistingOrder()
    {
        PlaceOrderRequest request = Pizzas.Order(Pizzas.Custom());
        Order order = Order.Place(new FakeTimeProvider(PlacedTime), request, OrderPricer.Price(MenuSeed.Create(), request.Pizzas).Value);
        order.AssignOrderNumber(1001);
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        return order;
    }

    [Fact]
    public async Task MarkReadyAsync_NewOrder_BecomesReadyAndIsSaved()
    {
        Order order = ExistingOrder();

        var response = await _handler.MarkReadyAsync(order.Id, TestContext.Current.CancellationToken);

        OrderDetails details = Assert.IsType<Ok<OrderDetails>>(response.Result).Value!;
        Assert.Equal(OrderStatus.Ready, details.Status);
        Assert.Equal(ReadyTime, details.ReadyAt);
        await _orderRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkReadyAsync_AlreadyReady_SucceedsAndKeepsTheFirstReadyTime()
    {
        Order order = ExistingOrder();
        DateTimeOffset firstReadyTime = PlacedTime.AddMinutes(10);
        order.MarkReady(new FakeTimeProvider(firstReadyTime));

        var response = await _handler.MarkReadyAsync(order.Id, TestContext.Current.CancellationToken);

        OrderDetails details = Assert.IsType<Ok<OrderDetails>>(response.Result).Value!;
        Assert.Equal(OrderStatus.Ready, details.Status);
        Assert.Equal(firstReadyTime, details.ReadyAt);
    }

    [Fact]
    public async Task MarkReadyAsync_UnknownOrder_ReturnsNotFoundAndSavesNothing()
    {
        Guid orderId = Guid.NewGuid();
        _orderRepository.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var response = await _handler.MarkReadyAsync(orderId, TestContext.Current.CancellationToken);

        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(response.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("OrderNotFound", problem.ProblemDetails.Title);
        await _orderRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
