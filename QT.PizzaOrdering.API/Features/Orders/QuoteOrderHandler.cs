using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

using QT.PizzaOrdering.API.Configs;
using QT.PizzaOrdering.Core.Abstractions;
using QT.PizzaOrdering.Core.Features.Menu.Interfaces;
using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.Core.Shared.Validation;

namespace QT.PizzaOrdering.API.Features.Orders;

/// <summary>
/// Prices a cart without placing an order.
/// This is the entry point for the POST /orders/quote endpoint.
/// </summary>
public sealed class QuoteOrderHandler(
    IValidator<QuoteOrderRequest> validator,
    IMenuQueries menuQueries,
    IOptionsMonitor<OrderPricingConfig> pricingOptions)
{
    /// <returns>
    /// Ok with the quote if successful.
    /// ValidationProblem if the request is malformed or refers to something that is not on the menu.
    /// </returns>
    public async Task<Results<Ok<OrderQuote>, ValidationProblem>> QuoteOrderAsync(
        QuoteOrderRequest request,
        CancellationToken ct = default)
    {
        ValidationResult validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.Errors);

        MenuCatalog menu = await menuQueries.GetMenuAsync(ct);
        Result<OrderQuote> quote = OrderPricer.Quote(menu, request.Pizzas, request.Fulfillment, pricingOptions.CurrentValue.ToRules());

        return quote.IsSuccess
            ? TypedResults.Ok(quote.Value)
            : TypedResults.ValidationProblem(quote.Error.ToValidationErrors());
    }
}

/// <summary>
/// Validates QuoteOrderRequest instances.
/// </summary>
public sealed class QuoteOrderRequestValidator : IValidator<QuoteOrderRequest>
{
    public ValueTask<ValidationResult> ValidateAsync(QuoteOrderRequest request, CancellationToken ct = default)
    {
        ValidationResult result = ValidationResult.Success();
        OrderRequestRules.ValidateFulfillment(result, request.Fulfillment);
        OrderRequestRules.ValidatePizzas(result, request.Pizzas);
        return ValueTask.FromResult(result);
    }
}
