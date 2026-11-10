using Microsoft.AspNetCore.Http.HttpResults;

using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;

namespace QT.PizzaOrdering.API.Features.Orders;

/// <summary>
/// Handles the kitchen marking an order as prepared.
/// This is the entry point for the PATCH /kitchen/orders/{orderId:guid}/ready endpoint.
/// </summary>
public sealed partial class MarkOrderReadyHandler
{
    private readonly ILogger<MarkOrderReadyHandler> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly TimeProvider _timeProvider;

    public MarkOrderReadyHandler(
        ILogger<MarkOrderReadyHandler> logger,
        IOrderRepository orderRepository,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _timeProvider = timeProvider;
    }

    /// <returns>
    /// Ok with the order, now Ready. An order that was already Ready is returned unchanged, so a
    /// second kitchen screen acting on the same order is not an error.
    /// ProblemHttpResult (404 Not Found) when no order has that id.
    /// </returns>
    public async Task<Results<Ok<OrderDetails>, ProblemHttpResult>> MarkReadyAsync(
        Guid orderId,
        CancellationToken ct = default)
    {
        Order? order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            LogOrderNotFound(orderId);
            Error.NotFound notFound = Error.NotFound.For(nameof(Order), orderId);
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: notFound.Code,
                detail: notFound.Message);
        }

        if (order.Status != OrderStatus.Ready)
        {
            order.MarkReady(_timeProvider);
            await _orderRepository.SaveChangesAsync(ct);
            LogOrderReady(order.Id, order.OrderNumber);
        }

        return TypedResults.Ok(OrderDetails.From(order));
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Cannot mark order ready because it was not found. OrderId: {OrderId}")]
    private partial void LogOrderNotFound(Guid orderId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Order '{OrderId}' (#{OrderNumber}) marked Ready.")]
    private partial void LogOrderReady(Guid orderId, int orderNumber);
}
