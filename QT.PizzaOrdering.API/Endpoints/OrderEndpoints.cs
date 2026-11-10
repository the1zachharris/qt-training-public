using Microsoft.AspNetCore.Mvc;

using QT.PizzaOrdering.API.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;

namespace QT.PizzaOrdering.API.Endpoints;

/// <summary>
/// Configures and maps the customer-facing order HTTP endpoints.
/// </summary>
public static class OrderEndpoints
{
    /// <remarks>
    /// Registered endpoints:
    /// - POST /api/v1/orders - Places a pickup order.
    /// - GET /api/v1/orders/{orderId:guid} - Gets a placed order.
    /// </remarks>
    public static void MapOrderEndpoints(this IEndpointRouteBuilder endpointRouteBuilder, string version)
    {
        RouteGroupBuilder routeGroupBuilder = endpointRouteBuilder.MapGroup($"/api/{version}");

        routeGroupBuilder
            .MapPost("/orders", async (
                [FromServices] PlaceOrderHandler handler,
                PlaceOrderRequest request,
                CancellationToken ct) => await handler.PlaceOrderAsync(request, ct))
            .WithName("PlaceOrder")
            .WithSummary("Places a pickup order.")
            .WithDescription("Validates and prices the cart from the current menu, then saves the order as New for the kitchen.")
            .WithTags("Orders");

        routeGroupBuilder
            .MapGet("/orders/{orderId:guid}", async (
                [FromServices] GetOrderHandler handler,
                Guid orderId,
                CancellationToken ct) => await handler.GetOrderAsync(orderId, ct))
            .WithName("GetOrder")
            .WithSummary("Gets a placed order.")
            .WithDescription("Gets the order number, customer name, pizzas, total, and status for the provided orderId.")
            .WithTags("Orders");
    }
}
