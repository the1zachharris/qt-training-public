using QT.PizzaOrdering.Core.Features.Orders.Models;

namespace QT.PizzaOrdering.Core.Features.Orders.Interfaces;

public interface IOrderRepository
{
    /// <summary>Assigns the next order number and saves the order.</summary>
    Task AddAsync(Order order, CancellationToken ct = default);

    /// <summary>The order with its lines, tracked so a status change can be saved; null when not found.</summary>
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>Every New order, oldest first, so the kitchen works them in the order they came in.</summary>
    Task<IReadOnlyList<Order>> GetNewAsync(CancellationToken ct = default);

    /// <summary>The most recently placed Ready orders, newest first, up to <paramref name="take"/>.</summary>
    Task<IReadOnlyList<Order>> GetRecentReadyAsync(int take, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
