using Microsoft.AspNetCore.Http.HttpResults;

using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Responses;

namespace QT.PizzaOrdering.API.Features.Orders;

/// <summary>
/// Handles retrieval of the orders the kitchen is working from.
/// This is the entry point for the GET /kitchen/orders endpoint.
/// </summary>
public sealed class GetKitchenOrdersHandler(IOrderRepository orderRepository)
{
    /// <summary>
    /// Ready orders pile up all day and are only needed until the customer collects them, so the
    /// list keeps the most recent ones. New orders are never cut off.
    /// </summary>
    public const int MaxReadyOrders = 50;

    public async Task<Ok<KitchenOrders>> GetKitchenOrdersAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Order> newOrders = await orderRepository.GetNewAsync(ct);
        IReadOnlyList<Order> readyOrders = await orderRepository.GetRecentReadyAsync(MaxReadyOrders, ct);

        return TypedResults.Ok(new KitchenOrders(
            [.. newOrders.Select(OrderDetails.From)],
            [.. readyOrders.Select(OrderDetails.From)]));
    }
}
