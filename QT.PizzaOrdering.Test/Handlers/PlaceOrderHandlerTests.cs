using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using QT.PizzaOrdering.API.Configs;
using QT.PizzaOrdering.API.Features.Orders;
using QT.PizzaOrdering.Core.Features.Menu.Interfaces;
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

        IOptionsMonitor<OrderPricingConfig> pricingOptions = Substitute.For<IOptionsMonitor<OrderPricingConfig>>();
        pricingOptions.CurrentValue.Returns(new OrderPricingConfig { TaxRate = 0.085m, DeliveryFee = 3.99m });

        _handler = new PlaceOrderHandler(
            NullLogger<PlaceOrderHandler>.Instance,
            new PlaceOrderRequestValidator(),
            _menuQueries,
            _orderRepository,
            pricingOptions,
            new FakeTimeProvider(Now));
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenValid_SavesTheOrderAndReturnsConfirmation()
    {
        PlaceOrderRequest request = Pizzas.DeliveryOrder(Pizzas.MediumPepperoni(2));

        var response = await _handler.PlaceOrderAsync(request, TestContext.Current.CancellationToken);

        OrderConfirmation confirmation = Assert.IsType<Ok<OrderConfirmation>>(response.Result).Value!;
        Assert.Equal(35.43m, confirmation.Total);
        Assert.Equal(Now, confirmation.PlacedAt);
        await _orderRepository.Received(1).AddAsync(
            Arg.Is<Order>(order => order.Id == confirmation.Id && order.Total == 35.43m && order.Lines.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenRequestIsInvalid_ReturnsValidationProblemWithoutReadingTheMenu()
    {
        PlaceOrderRequest request = Pizzas.PickupOrder(Pizzas.MediumPepperoni()) with { CustomerName = "" };

        var response = await _handler.PlaceOrderAsync(request, TestContext.Current.CancellationToken);

        ValidationProblem problem = Assert.IsType<ValidationProblem>(response.Result);
        Assert.Contains(nameof(PlaceOrderRequest.CustomerName), problem.ProblemDetails.Errors.Keys);
        await _menuQueries.DidNotReceive().GetMenuAsync(Arg.Any<CancellationToken>());
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenPizzaIsNotOnTheMenu_ReturnsValidationProblemAndSavesNothing()
    {
        PlaceOrderRequest request = Pizzas.PickupOrder(Pizzas.BuildYourOwn(sizeId: 99));

        var response = await _handler.PlaceOrderAsync(request, TestContext.Current.CancellationToken);

        ValidationProblem problem = Assert.IsType<ValidationProblem>(response.Result);
        Assert.Contains("Pizzas[0].SizeId", problem.ProblemDetails.Errors.Keys);
        await _orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }
}
