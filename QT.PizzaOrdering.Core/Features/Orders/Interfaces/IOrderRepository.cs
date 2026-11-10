using QT.PizzaOrdering.Core.Features.Orders.Models;

namespace QT.PizzaOrdering.Core.Features.Orders.Interfaces;

public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);
}
