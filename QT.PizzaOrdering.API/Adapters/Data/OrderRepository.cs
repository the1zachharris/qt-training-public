using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Infrastructure;

namespace QT.PizzaOrdering.API.Adapters.Data;

public class OrderRepository(PizzaOrderingDbContext dbContext) : IOrderRepository
{
    /// <summary>The number the shop's first order gets.</summary>
    public const int FirstOrderNumber = 1001;

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        // Reading the highest number and inserting the next one must happen as one step, or two
        // orders placed together would read the same number. A SQLite transaction takes the
        // write lock when it begins, so a second order waits here until the first has committed.
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct);

        int highest = await dbContext.Orders.MaxAsync(existing => (int?)existing.OrderNumber, ct) ?? FirstOrderNumber - 1;
        order.AssignOrderNumber(highest + 1);
        await dbContext.Orders.AddAsync(order, ct);
        await dbContext.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await dbContext.Orders
            .Include(order => order.Lines)
            .SingleOrDefaultAsync(order => order.Id == orderId, ct);
    }

    // Both lists sort by order number, which is also the order the orders were placed in.
    // SQLite cannot sort by DateTimeOffset, so PlacedAt and ReadyAt are not used for ordering.

    public async Task<IReadOnlyList<Order>> GetNewAsync(CancellationToken ct = default)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .Where(order => order.Status == OrderStatus.New)
            .OrderBy(order => order.OrderNumber)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Order>> GetRecentReadyAsync(int take, CancellationToken ct = default)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .Where(order => order.Status == OrderStatus.Ready)
            .OrderByDescending(order => order.OrderNumber)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await dbContext.SaveChangesAsync(ct);
    }
}
