using Microsoft.AspNetCore.Mvc;

using QT.PizzaOrdering.API.Features.Menu;

namespace QT.PizzaOrdering.API.Endpoints;

/// <summary>
/// Configures and maps the menu HTTP endpoints.
/// </summary>
public static class MenuEndpoints
{
    /// <remarks>
    /// Registered endpoints:
    /// - GET /api/v1/health - Returns 200 OK if the API is reachable.
    /// - GET /api/v1/menu - Gets sizes, crusts, sauces, toppings, and preset pizzas.
    /// </remarks>
    public static void MapMenuEndpoints(this IEndpointRouteBuilder endpointRouteBuilder, string version)
    {
        RouteGroupBuilder routeGroupBuilder = endpointRouteBuilder.MapGroup($"/api/{version}");

        routeGroupBuilder
            .MapGet("/health", () => Results.Ok())
            .WithName("HealthCheck")
            .WithSummary("API health check.")
            .WithDescription("Returns 200 OK if the API is reachable.")
            .WithTags("Health")
            .ExcludeFromDescription();

        routeGroupBuilder
            .MapGet("/menu", async (
                [FromServices] GetMenuHandler handler,
                CancellationToken ct) => await handler.GetMenuAsync(ct))
            .WithName("GetMenu")
            .WithSummary("Gets the menu.")
            .WithDescription("Gets every size, crust, sauce, topping, and preset pizza a customer can order.")
            .WithTags("Menu");
    }
}
