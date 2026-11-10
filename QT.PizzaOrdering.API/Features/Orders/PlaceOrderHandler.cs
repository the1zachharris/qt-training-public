using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

using QT.PizzaOrdering.API.Configs;
using QT.PizzaOrdering.Core.Abstractions;
using QT.PizzaOrdering.Core.Features.Menu.Interfaces;
using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.Core.Shared.Validation;

namespace QT.PizzaOrdering.API.Features.Orders;

/// <summary>
/// Handles placing an order.
/// This is the entry point for the POST /orders endpoint.
/// </summary>
public sealed partial class PlaceOrderHandler
{
    private readonly ILogger<PlaceOrderHandler> _logger;
    private readonly IValidator<PlaceOrderRequest> _validator;
    private readonly IMenuQueries _menuQueries;
    private readonly IOrderRepository _orderRepository;
    private readonly IOptionsMonitor<OrderPricingConfig> _pricingOptions;
    private readonly TimeProvider _timeProvider;

    public PlaceOrderHandler(
        ILogger<PlaceOrderHandler> logger,
        IValidator<PlaceOrderRequest> validator,
        IMenuQueries menuQueries,
        IOrderRepository orderRepository,
        IOptionsMonitor<OrderPricingConfig> pricingOptions,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _validator = validator;
        _menuQueries = menuQueries;
        _orderRepository = orderRepository;
        _pricingOptions = pricingOptions;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Places an order. The cart is priced here from the current menu; any price the caller saw
    /// is never trusted.
    /// </summary>
    /// <returns>
    /// Ok with the order confirmation if successful.
    /// ValidationProblem if the request is malformed or refers to something that is not on the menu.
    /// </returns>
    public async Task<Results<Ok<OrderConfirmation>, ValidationProblem>> PlaceOrderAsync(
        PlaceOrderRequest request,
        CancellationToken ct = default)
    {
        ValidationResult validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.Errors);

        MenuCatalog menu = await _menuQueries.GetMenuAsync(ct);
        Result<OrderQuote> quote = OrderPricer.Quote(menu, request.Pizzas, request.Fulfillment, _pricingOptions.CurrentValue.ToRules());
        if (quote.IsFailure)
        {
            LogCartNotOnMenu(quote.Error.Message);
            return TypedResults.ValidationProblem(quote.Error.ToValidationErrors());
        }

        Order order = Order.Place(_timeProvider, request, quote.Value);
        await _orderRepository.AddAsync(order, ct);
        LogOrderPlaced(order.Id, order.OrderNumber, order.Fulfillment, order.Total);
        return TypedResults.Ok(OrderConfirmation.From(order));
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Order rejected because the cart does not match the menu. {Reason}")]
    private partial void LogCartNotOnMenu(string reason);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Placed order '{OrderId}' ({OrderNumber}). Fulfillment: {Fulfillment}, Total: {Total}.")]
    private partial void LogOrderPlaced(Guid orderId, string orderNumber, FulfillmentType fulfillment, decimal total);
}

/// <summary>
/// Validates PlaceOrderRequest instances.
/// </summary>
public sealed class PlaceOrderRequestValidator : IValidator<PlaceOrderRequest>
{
    public const int MaxCustomerNameLength = 100;
    public const int MaxDeliveryAddressLength = 200;
    public const int MaxNotesLength = 500;
    public const int MinPhoneDigits = 10;
    public const int MaxPhoneDigits = 15;

    public ValueTask<ValidationResult> ValidateAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        ValidationResult result = ValidationResult.Success();

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            result.AddError(nameof(request.CustomerName), "Name is required.");
        else
            result.AddErrorIf(request.CustomerName.Trim().Length > MaxCustomerNameLength, nameof(request.CustomerName), $"Name cannot be longer than {MaxCustomerNameLength} characters.");

        if (string.IsNullOrWhiteSpace(request.Phone))
            result.AddError(nameof(request.Phone), "Phone number is required.");
        else
            result.AddErrorIf(!IsPhoneNumber(request.Phone), nameof(request.Phone), $"Phone number must have {MinPhoneDigits} to {MaxPhoneDigits} digits.");

        OrderRequestRules.ValidateFulfillment(result, request.Fulfillment);
        if (request.Fulfillment == FulfillmentType.Delivery)
        {
            if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                result.AddError(nameof(request.DeliveryAddress), "A delivery address is required for delivery orders.");
            else
                result.AddErrorIf(request.DeliveryAddress.Trim().Length > MaxDeliveryAddressLength, nameof(request.DeliveryAddress), $"Delivery address cannot be longer than {MaxDeliveryAddressLength} characters.");
        }

        result.AddErrorIf(request.Notes?.Trim().Length > MaxNotesLength, nameof(request.Notes), $"Notes cannot be longer than {MaxNotesLength} characters.");

        OrderRequestRules.ValidatePizzas(result, request.Pizzas);
        return ValueTask.FromResult(result);
    }

    private static bool IsPhoneNumber(string phone)
    {
        int digits = phone.Count(char.IsAsciiDigit);
        bool onlyPhoneCharacters = phone.All(c => char.IsAsciiDigit(c) || c is ' ' or '-' or '(' or ')' or '+' or '.');
        return onlyPhoneCharacters && digits is >= MinPhoneDigits and <= MaxPhoneDigits;
    }
}
