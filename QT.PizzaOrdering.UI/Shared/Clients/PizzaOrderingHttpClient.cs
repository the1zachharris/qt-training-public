using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;

using System.Net;
using System.Text.Json;

namespace QT.PizzaOrdering.UI.Shared.Clients;

public sealed partial class PizzaOrderingHttpClient(ILogger<PizzaOrderingHttpClient> logger, HttpClient http) : IPizzaOrderingHttpClient
{
    private readonly ILogger<PizzaOrderingHttpClient> _logger = logger;

    private readonly HttpClient _http = http;

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug, Message = "Method: {MethodName}, StatusCode: {StatusCode}, ContentLength: {ContentSize}.")]
    private static partial void LogHttpCall(ILogger logger, string methodName, HttpStatusCode statusCode, long? contentSize);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Exception caught while executing method {MethodName}")]
    private static partial void LogHttpException(ILogger logger, Exception exception, string methodName);

    /// <summary>
    /// Centralized route constants for the Pizza Ordering API.
    /// </summary>
    private static class Endpoints
    {
        private const string Base = "api/v1";

        public const string Menu = $"{Base}/menu";
        public const string Orders = $"{Base}/orders";
    }

    public Task<ClientResult<MenuCatalog>> GetMenuAsync(CancellationToken ct = default) =>
        SendAsync<MenuCatalog>(nameof(GetMenuAsync), () => _http.GetAsync(Endpoints.Menu, ct), ct);

    public Task<ClientResult<OrderDetails>> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default) =>
        SendAsync<OrderDetails>(nameof(PlaceOrderAsync), () => _http.PostAsJsonAsync(Endpoints.Orders, request, ct), ct);

    public Task<ClientResult<OrderDetails>> GetOrderAsync(Guid orderId, CancellationToken ct = default) =>
        SendAsync<OrderDetails>(nameof(GetOrderAsync), () => _http.GetAsync($"{Endpoints.Orders}/{orderId}", ct), ct);

    /// <summary>
    /// Sends one request and turns every outcome into a <see cref="ClientResult{T}"/>: the body on
    /// success, the server's own error on a failure status, and the exception when the call never completed.
    /// </summary>
    private async Task<ClientResult<T>> SendAsync<T>(string methodName, Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using HttpResponseMessage response = await send();
            LogHttpCall(_logger, methodName, response.StatusCode, response.Content.Headers.ContentLength);
            if (!response.IsSuccessStatusCode)
                return await ProblemResult.FromResponseAsync<T>(response, ct);

            T data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct)
                ?? throw new JsonException($"Could not deserialize response for object type of {typeof(T)}");
            return ClientResult<T>.Ok(data);
        }
        catch (Exception ex)
        {
            LogHttpException(_logger, ex, methodName);
            return ClientResult<T>.Fail(ex);
        }
    }
}
