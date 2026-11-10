using Microsoft.AspNetCore.Http.HttpResults;

using QT.PizzaOrdering.Core.Abstractions;
using QT.PizzaOrdering.Core.Features.Menu.Interfaces;
using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.Core.Shared.Validation;

namespace QT.PizzaOrdering.API.Features.Orders;

/// <summary>
/// Handles placing a pickup order.
/// This is the entry point for the POST /orders endpoint.
/// </summary>
public sealed partial class PlaceOrderHandler
{
    private readonly ILogger<PlaceOrderHandler> _logger;
    private readonly IValidator<PlaceOrderRequest> _validator;
    private readonly IMenuQueries _menuQueries;
    private readonly IOrderRepository _orderRepository;
    private readonly TimeProvider _timeProvider;

    public PlaceOrderHandler(
        ILogger<PlaceOrderHandler> logger,
        IValidator<PlaceOrderRequest> validator,
        IMenuQueries menuQueries,
        IOrderRepository orderRepository,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _validator = validator;
        _menuQueries = menuQueries;
        _orderRepository = orderRepository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Places an order. The cart is priced here from the current menu; any price the caller saw
    /// is never trusted. The saved order is New, which is what puts it in front of the kitchen.
    /// </summary>
    /// <returns>
    /// Ok with the placed order if successful.
    /// ValidationProblem if the request is malformed or refers to something that is not on the menu.
    /// </returns>
    public async Task<Results<Ok<OrderDetails>, ValidationProblem>> PlaceOrderAsync(
        PlaceOrderRequest request,
        CancellationToken ct = default)
    {
        ValidationResult validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.Errors);

        MenuCatalog menu = await _menuQueries.GetMenuAsync(ct);
        Result<IReadOnlyList<PricedPizza>> pizzas = OrderPricer.Price(menu, request.Pizzas);
        if (pizzas.IsFailure)
        {
            LogCartNotOnMenu(pizzas.Error.Message);
            return TypedResults.ValidationProblem(pizzas.Error.ToValidationErrors());
        }

        Order order = Order.Place(_timeProvider, request, pizzas.Value);
        await _orderRepository.AddAsync(order, ct);
        LogOrderPlaced(order.Id, order.OrderNumber, order.Total);
        return TypedResults.Ok(OrderDetails.From(order));
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Order rejected because the cart does not match the menu. {Reason}")]
    private partial void LogCartNotOnMenu(string reason);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Placed order '{OrderId}' (#{OrderNumber}). Total: {Total}.")]
    private partial void LogOrderPlaced(Guid orderId, int orderNumber, decimal total);
}

/// <summary>
/// Validates PlaceOrderRequest instances. Whether each pizza is valid against the menu is not
/// decided here: <see cref="PizzaPricer"/> owns that.
/// </summary>
public sealed class PlaceOrderRequestValidator : IValidator<PlaceOrderRequest>
{
    public const int MaxCustomerNameLength = 100;

    public ValueTask<ValidationResult> ValidateAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        ValidationResult result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            result.AddError(nameof(request.CustomerName), "A name is required to place an order.");
        else
            result.AddErrorIf(request.CustomerName.Trim().Length > MaxCustomerNameLength, nameof(request.CustomerName), $"Name cannot be longer than {MaxCustomerNameLength} characters.");

        if (request.Pizzas is null || request.Pizzas.Count == 0)
        {
            result.AddError(nameof(request.Pizzas), "The order is empty. Add at least one pizza.");
        }
        else
        {
            result.AddErrorIf(request.Pizzas.Count > OrderPricer.MaxPizzaLines, nameof(request.Pizzas), $"An order can have at most {OrderPricer.MaxPizzaLines} different pizzas.");
            result.AddErrorIf(request.Pizzas.Any(pizza => pizza is null), nameof(request.Pizzas), "A pizza in the order is missing.");
        }

        return ValueTask.FromResult(result);
    }
}
