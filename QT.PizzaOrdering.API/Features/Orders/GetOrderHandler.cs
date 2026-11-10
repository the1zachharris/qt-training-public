using Microsoft.AspNetCore.Http.HttpResults;

using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Models;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;

namespace QT.PizzaOrdering.API.Features.Orders;

/// <summary>
/// Handles retrieval of a placed order.
/// This is the entry point for the GET /orders/{orderId:guid} endpoint.
/// </summary>
public sealed partial class GetOrderHandler
{
    private readonly ILogger<GetOrderHandler> _logger;
    private readonly IOrderRepository _orderRepository;

    public GetOrderHandler(ILogger<GetOrderHandler> logger, IOrderRepository orderRepository)
    {
        _logger = logger;
        _orderRepository = orderRepository;
    }

    /// <returns>
    /// Ok with the order confirmation when found.
    /// ProblemHttpResult (404 Not Found) when no order has that id.
    /// </returns>
    public async Task<Results<Ok<OrderConfirmation>, ProblemHttpResult>> GetOrderAsync(
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

        return TypedResults.Ok(OrderConfirmation.From(order));
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Order not found. OrderId: {OrderId}")]
    private partial void LogOrderNotFound(Guid orderId);
}
