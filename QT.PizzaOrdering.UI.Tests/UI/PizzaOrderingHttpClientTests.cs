using System.Net;
using System.Text;

using Microsoft.Extensions.Logging.Abstractions;

using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.UI.Shared.Clients;

namespace QT.PizzaOrdering.UI.Tests.UI;

public class PizzaOrderingHttpClientTests
{
    private const string OrderJson = """
        {
          "id": "3f2a6c1e-0d4b-4a57-9d1c-6f0e2b7a8c11", "orderNumber": 1001, "customerName": "Pat Sample",
          "placedAt": "2026-11-10T18:30:00+00:00", "status": "New", "readyAt": null,
          "pizzas": [ { "name": "Build Your Own", "size": "Large (14\")", "toppings": [ "Pepperoni" ], "quantity": 2, "unitPrice": 16.00, "lineTotal": 32.00 } ],
          "total": 32.00
        }
        """;

    private HttpRequestMessage? _request;

    private PizzaOrderingHttpClient ClientReturning(HttpStatusCode status, string? json)
    {
        StubHandler handler = new(request =>
        {
            _request = request;
            HttpResponseMessage response = new(status);
            if (json is not null)
                response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        });
        HttpClient http = new(handler) { BaseAddress = new Uri("http://localhost/") };
        return new PizzaOrderingHttpClient(NullLogger<PizzaOrderingHttpClient>.Instance, http);
    }

    [Fact]
    public async Task GetMenuAsync_WhenOk_ReturnsTheMenu()
    {
        const string json = """
            {
              "sizes": [ { "id": 1, "name": "Small", "inches": 10, "price": 10.00 }, { "id": 2, "name": "Large", "inches": 14, "price": 14.00 } ],
              "toppings": [ { "id": 1, "name": "Pepperoni", "price": 2.00 } ],
              "presetPizzas": [ { "id": 2, "name": "Pepperoni", "description": "x", "toppingIds": [ 1 ] } ]
            }
            """;
        PizzaOrderingHttpClient client = ClientReturning(HttpStatusCode.OK, json);

        ClientResult<MenuCatalog> result = await client.GetMenuAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("/api/v1/menu", _request!.RequestUri!.AbsolutePath);
        Assert.Equal([10.00m, 14.00m], result.Data!.Sizes.Select(size => size.Price));
        Assert.Equal(2.00m, Assert.Single(result.Data.Toppings).Price);
        Assert.Equal([1], Assert.Single(result.Data.PresetPizzas).ToppingIds);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenOk_ReturnsTheOrderWithItsNumberAndStatus()
    {
        PizzaOrderingHttpClient client = ClientReturning(HttpStatusCode.OK, OrderJson);

        ClientResult<OrderDetails> result = await client.PlaceOrderAsync(
            new PlaceOrderRequest("Pat Sample", [new PizzaSelection(null, 2, [1], 2)]), TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, _request!.Method);
        Assert.Equal("/api/v1/orders", _request.RequestUri!.AbsolutePath);
        Assert.Equal(1001, result.Data!.OrderNumber);
        Assert.Equal(OrderStatus.New, result.Data.Status);
        Assert.Equal(32.00m, result.Data.Total);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenValidationProblem_CarriesTheFieldErrors()
    {
        const string json = """
            {
              "title": "One or more validation errors occurred.",
              "status": 400,
              "errors": { "CustomerName": [ "A name is required to place an order." ] }
            }
            """;
        PizzaOrderingHttpClient client = ClientReturning(HttpStatusCode.BadRequest, json);

        ClientResult<OrderDetails> result = await client.PlaceOrderAsync(new PlaceOrderRequest("", []), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(HttpStatusCode.BadRequest, result.Error!.Status);
        Assert.Equal("A name is required to place an order.", result.Error.Message);
        Assert.Equal(["A name is required to place an order."], result.Error.Errors!["CustomerName"]);
    }

    [Fact]
    public async Task GetOrderAsync_WhenNotFoundProblem_UsesTheDetailAsTheMessage()
    {
        const string json = """{ "title": "OrderNotFound", "status": 404, "detail": "Order with id 'abc' was not found." }""";
        PizzaOrderingHttpClient client = ClientReturning(HttpStatusCode.NotFound, json);

        ClientResult<OrderDetails> result = await client.GetOrderAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, result.Error!.Status);
        Assert.Equal("Order with id 'abc' was not found.", result.Error.Message);
    }

    [Fact]
    public async Task GetMenuAsync_WhenFailureHasNoBody_FallsBackToTheStatusLine()
    {
        PizzaOrderingHttpClient client = ClientReturning(HttpStatusCode.ServiceUnavailable, json: null);

        ClientResult<MenuCatalog> result = await client.GetMenuAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Contains("503", result.Error!.Message);
    }

    [Fact]
    public async Task GetMenuAsync_WhenTheCallThrows_ReturnsAFailureInsteadOfThrowing()
    {
        StubHandler handler = new(_ => throw new HttpRequestException("Connection refused"));
        PizzaOrderingHttpClient client = new(NullLogger<PizzaOrderingHttpClient>.Instance, new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        ClientResult<MenuCatalog> result = await client.GetMenuAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Connection refused", result.Error!.Message);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
