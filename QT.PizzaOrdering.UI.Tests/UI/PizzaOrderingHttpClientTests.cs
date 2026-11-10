using System.Net;
using System.Text;

using Microsoft.Extensions.Logging.Abstractions;

using QT.PizzaOrdering.Core.Features.Menu.Enums;
using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.UI.Shared.Clients;

namespace QT.PizzaOrdering.UI.Tests.UI;

public class PizzaOrderingHttpClientTests
{
    private static PizzaOrderingHttpClient ClientReturning(HttpStatusCode status, string? json, Action<HttpRequestMessage>? inspect = null)
    {
        StubHandler handler = new(request =>
        {
            inspect?.Invoke(request);
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
              "sizes": [ { "id": 2, "name": "Medium", "inches": 12, "basePrice": 12.99, "toppingPrice": 1.50 } ],
              "crusts": [ { "id": 1, "name": "Hand Tossed", "upcharge": 0 } ],
              "sauces": [ { "id": 1, "name": "Classic Marinara" } ],
              "toppings": [ { "id": 1, "name": "Pepperoni", "category": "Meat" } ],
              "presetPizzas": [ { "id": 2, "name": "Pepperoni", "description": "x", "crustId": 1, "sauceId": 1, "cheese": "Regular", "toppingIds": [ 1 ] } ]
            }
            """;
        string? requestedPath = null;
        PizzaOrderingHttpClient client = ClientReturning(HttpStatusCode.OK, json, request => requestedPath = request.RequestUri?.AbsolutePath);

        ClientResult<MenuCatalog> result = await client.GetMenuAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("/api/v1/menu", requestedPath);
        Assert.Equal(ToppingCategory.Meat, Assert.Single(result.Data!.Toppings).Category);
        Assert.Equal(CheeseLevel.Regular, Assert.Single(result.Data.PresetPizzas).Cheese);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenValidationProblem_CarriesTheFieldErrors()
    {
        const string json = """
            {
              "title": "One or more validation errors occurred.",
              "status": 400,
              "errors": { "CustomerName": [ "Name is required." ], "Phone": [ "Phone number is required." ] }
            }
            """;
        PizzaOrderingHttpClient client = ClientReturning(HttpStatusCode.BadRequest, json);
        PlaceOrderRequest request = new("", "", FulfillmentType.Pickup, null, null, []);

        ClientResult<OrderConfirmation> result = await client.PlaceOrderAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(HttpStatusCode.BadRequest, result.Error!.Status);
        Assert.Equal("Name is required. Phone number is required.", result.Error.Message);
        Assert.Equal(["Name is required."], result.Error.Errors!["CustomerName"]);
    }

    [Fact]
    public async Task GetOrderAsync_WhenNotFoundProblem_UsesTheDetailAsTheMessage()
    {
        const string json = """{ "title": "OrderNotFound", "status": 404, "detail": "Order with id 'abc' was not found." }""";
        PizzaOrderingHttpClient client = ClientReturning(HttpStatusCode.NotFound, json);

        ClientResult<OrderConfirmation> result = await client.GetOrderAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, result.Error!.Status);
        Assert.Equal("Order with id 'abc' was not found.", result.Error.Message);
    }

    [Fact]
    public async Task QuoteOrderAsync_WhenFailureHasNoBody_FallsBackToTheStatusLine()
    {
        PizzaOrderingHttpClient client = ClientReturning(HttpStatusCode.ServiceUnavailable, json: null);

        ClientResult<OrderQuote> result = await client.QuoteOrderAsync(new QuoteOrderRequest(FulfillmentType.Pickup, []), TestContext.Current.CancellationToken);

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
