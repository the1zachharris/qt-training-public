using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;

namespace QT.PizzaOrdering.Core.Abstractions;

/// <summary>
/// The UI's view of the Pizza Ordering API. Every call returns a <see cref="ClientResult{T}"/>
/// so pages can show the server's own error instead of handling exceptions.
/// </summary>
public interface IPizzaOrderingHttpClient
{
    Task<ClientResult<MenuCatalog>> GetMenuAsync(CancellationToken ct = default);

    Task<ClientResult<OrderDetails>> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default);

    Task<ClientResult<OrderDetails>> GetOrderAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>The kitchen's orders: New (oldest first) and recent Ready (newest first).</summary>
    Task<ClientResult<KitchenOrders>> GetKitchenOrdersAsync(CancellationToken ct = default);

    /// <summary>Marks an order Ready. Succeeds without change when the order is already Ready.</summary>
    Task<ClientResult<OrderDetails>> MarkOrderReadyAsync(Guid orderId, CancellationToken ct = default);
}
