using Microsoft.AspNetCore.Mvc;

using QT.PizzaOrdering.API.Features.Orders;

namespace QT.PizzaOrdering.API.Endpoints;

/// <summary>
/// Configures and maps the HTTP endpoints the kitchen screen uses.
/// </summary>
public static class KitchenEndpoints
{
    /// <remarks>
    /// Registered endpoints:
    /// - GET /api/v1/kitchen/orders - Gets New orders (oldest first) and recent Ready orders (newest first).
    /// - PATCH /api/v1/kitchen/orders/{orderId:guid}/ready - Marks an order Ready.
    /// </remarks>
    public static void MapKitchenEndpoints(this IEndpointRouteBuilder endpointRouteBuilder, string version)
    {
        RouteGroupBuilder routeGroupBuilder = endpointRouteBuilder.MapGroup($"/api/{version}/kitchen");

        routeGroupBuilder
            .MapGet("/orders", async (
                [FromServices] GetKitchenOrdersHandler handler,
                CancellationToken ct) => await handler.GetKitchenOrdersAsync(ct))
            .WithName("GetKitchenOrders")
            .WithSummary("Gets the kitchen's orders.")
            .WithDescription("Gets every New order, oldest first, and the most recent Ready orders, newest first.")
            .WithTags("Kitchen");

        routeGroupBuilder
            .MapPatch("/orders/{orderId:guid}/ready", async (
                [FromServices] MarkOrderReadyHandler handler,
                Guid orderId,
                CancellationToken ct) => await handler.MarkReadyAsync(orderId, ct))
            .WithName("MarkOrderReady")
            .WithSummary("Marks an order Ready.")
            .WithDescription("Changes a New order to Ready. Marking an order that is already Ready succeeds without changing it.")
            .WithTags("Kitchen");
    }
}
