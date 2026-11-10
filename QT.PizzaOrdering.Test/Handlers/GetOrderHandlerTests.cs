using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using QT.PizzaOrdering.API.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Infrastructure.Seeding;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Handlers;

public class GetOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly GetOrderHandler _handler;

    public GetOrderHandlerTests()
    {
        _handler = new GetOrderHandler(NullLogger<GetOrderHandler>.Instance, _orderRepository);
    }

    [Fact]
    public async Task GetOrderAsync_WhenFound_ReturnsTheOrder()
    {
        PlaceOrderRequest request = Pizzas.Order(Pizzas.Custom(Pizzas.Small, toppingIds: [Pizzas.Onions]));
        Order order = Order.Place(TimeProvider.System, request, OrderPricer.Price(MenuSeed.Create(), request.Pizzas).Value);
        order.AssignOrderNumber(1001);
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var response = await _handler.GetOrderAsync(order.Id, TestContext.Current.CancellationToken);

        OrderDetails details = Assert.IsType<Ok<OrderDetails>>(response.Result).Value!;
        Assert.Equal(1001, details.OrderNumber);
        Assert.Equal("Pat Sample", details.CustomerName);
        Assert.Equal(12.00m, details.Total);
        Assert.Equal(["Onions"], Assert.Single(details.Pizzas).Toppings);
    }

    [Fact]
    public async Task GetOrderAsync_WhenMissing_ReturnsNotFoundProblem()
    {
        Guid orderId = Guid.NewGuid();
        _orderRepository.GetByIdAsync(orderId, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var response = await _handler.GetOrderAsync(orderId, TestContext.Current.CancellationToken);

        ProblemHttpResult problem = Assert.IsType<ProblemHttpResult>(response.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("OrderNotFound", problem.ProblemDetails.Title);
    }
}
