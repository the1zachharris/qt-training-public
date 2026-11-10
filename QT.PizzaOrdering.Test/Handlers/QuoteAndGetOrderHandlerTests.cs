using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using QT.PizzaOrdering.API.Configs;
using QT.PizzaOrdering.API.Features.Orders;
using QT.PizzaOrdering.Core.Features.Menu.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Infrastructure.Seeding;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Handlers;

public class QuoteOrderHandlerTests
{
    private readonly QuoteOrderHandler _handler;

    public QuoteOrderHandlerTests()
    {
        IMenuQueries menuQueries = Substitute.For<IMenuQueries>();
        menuQueries.GetMenuAsync(Arg.Any<CancellationToken>()).Returns(MenuSeed.Create());

        IOptionsMonitor<OrderPricingConfig> pricingOptions = Substitute.For<IOptionsMonitor<OrderPricingConfig>>();
        pricingOptions.CurrentValue.Returns(new OrderPricingConfig { TaxRate = 0.085m, DeliveryFee = 3.99m });

        _handler = new QuoteOrderHandler(new QuoteOrderRequestValidator(), menuQueries, pricingOptions);
    }

    [Fact]
    public async Task QuoteOrderAsync_WhenValid_ReturnsQuote()
    {
        QuoteOrderRequest request = new(FulfillmentType.Delivery, [Pizzas.MediumPepperoni(2)]);

        var response = await _handler.QuoteOrderAsync(request, TestContext.Current.CancellationToken);

        OrderQuote quote = Assert.IsType<Ok<OrderQuote>>(response.Result).Value!;
        Assert.Equal(28.98m, quote.Subtotal);
        Assert.Equal(35.43m, quote.Total);
    }

    [Fact]
    public async Task QuoteOrderAsync_WhenCartIsEmpty_ReturnsValidationProblem()
    {
        QuoteOrderRequest request = new(FulfillmentType.Pickup, []);

        var response = await _handler.QuoteOrderAsync(request, TestContext.Current.CancellationToken);

        Assert.IsType<ValidationProblem>(response.Result);
    }

    [Fact]
    public async Task QuoteOrderAsync_WhenPizzaIsNotOnTheMenu_ReturnsValidationProblem()
    {
        QuoteOrderRequest request = new(FulfillmentType.Pickup, [Pizzas.BuildYourOwn(crustId: 99)]);

        var response = await _handler.QuoteOrderAsync(request, TestContext.Current.CancellationToken);

        ValidationProblem problem = Assert.IsType<ValidationProblem>(response.Result);
        Assert.Contains("Pizzas[0].CrustId", problem.ProblemDetails.Errors.Keys);
    }
}

public class GetOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly GetOrderHandler _handler;

    public GetOrderHandlerTests()
    {
        _handler = new GetOrderHandler(NullLogger<GetOrderHandler>.Instance, _orderRepository);
    }

    [Fact]
    public async Task GetOrderAsync_WhenFound_ReturnsConfirmation()
    {
        PlaceOrderRequest request = Pizzas.PickupOrder(Pizzas.MediumPepperoni());
        OrderQuote quote = new([new PricedPizza("Pepperoni", "Medium (12\")", "Hand Tossed", "Classic Marinara", default, ["Pepperoni"], 1, 14.49m, 14.49m)], 14.49m, 1.23m, 0m, 15.72m);
        Order order = Order.Place(TimeProvider.System, request, quote);
        _orderRepository.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var response = await _handler.GetOrderAsync(order.Id, TestContext.Current.CancellationToken);

        OrderConfirmation confirmation = Assert.IsType<Ok<OrderConfirmation>>(response.Result).Value!;
        Assert.Equal(order.OrderNumber, confirmation.OrderNumber);
        Assert.Equal("Pepperoni", Assert.Single(confirmation.Pizzas).Name);
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
