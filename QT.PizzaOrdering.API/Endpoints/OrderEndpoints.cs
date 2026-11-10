using Microsoft.AspNetCore.Mvc;

using QT.PizzaOrdering.API.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;

namespace QT.PizzaOrdering.API.Endpoints;

/// <summary>
/// Configures and maps the order HTTP endpoints.
/// </summary>
public static class OrderEndpoints
{
    /// <remarks>
    /// Registered endpoints:
    /// - POST /api/v1/orders/quote - Prices a cart without placing an order.
    /// - POST /api/v1/orders - Places an order.
    /// - GET /api/v1/orders/{orderId:guid} - Gets a placed order.
    /// </remarks>
    public static void MapOrderEndpoints(this IEndpointRouteBuilder endpointRouteBuilder, string version)
    {
        RouteGroupBuilder routeGroupBuilder = endpointRouteBuilder.MapGroup($"/api/{version}");

        routeGroupBuilder
            .MapPost("/orders/quote", async (
                [FromServices] QuoteOrderHandler handler,
                QuoteOrderRequest request,
                CancellationToken ct) => await handler.QuoteOrderAsync(request, ct))
            .WithName("QuoteOrder")
            .WithSummary("Prices a cart.")
            .WithDescription("Prices the pizzas in a cart and returns the subtotal, tax, delivery fee, and total. Nothing is saved.")
            .WithTags("Orders");

        routeGroupBuilder
            .MapPost("/orders", async (
                [FromServices] PlaceOrderHandler handler,
                PlaceOrderRequest request,
                CancellationToken ct) => await handler.PlaceOrderAsync(request, ct))
            .WithName("PlaceOrder")
            .WithSummary("Places an order.")
            .WithDescription("Validates and prices the cart from the current menu, then saves the order.")
            .WithTags("Orders");

        routeGroupBuilder
            .MapGet("/orders/{orderId:guid}", async (
                [FromServices] GetOrderHandler handler,
                Guid orderId,
                CancellationToken ct) => await handler.GetOrderAsync(orderId, ct))
            .WithName("GetOrder")
            .WithSummary("Gets a placed order.")
            .WithDescription("Gets the confirmation details for the provided orderId.")
            .WithTags("Orders");
    }
}
